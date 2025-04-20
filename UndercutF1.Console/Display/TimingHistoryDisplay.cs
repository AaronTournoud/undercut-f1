using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.VisualElements;
using Microsoft.Extensions.Options;
using UndercutF1.Data;
using SkiaSharp;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace UndercutF1.Console;

public class TimingHistoryDisplay(
    State state,
    TimingDataProcessor timingData,
    DriverListProcessor driverList,
    LapCountProcessor lapCount,
    SessionInfoProcessor sessionInfo,
    TerminalInfoProvider terminalInfo,
    IOptions<LiveTimingOptions> options
) : IDisplay
{
    public Screen Screen => Screen.TimingHistory;

    private const int LEFT_OFFSET = 68; // The normal width of the timing table
    private const int BOTTOM_OFFSET = 2;
    private const int LAPS_IN_CHART = 15;

    private readonly Style _personalBest = new(
        foreground: Color.White,
        background: new Color(0, 118, 0)
    );
    private readonly Style _overallBest = new(
        foreground: Color.White,
        background: new Color(118, 0, 118)
    );
    private readonly Style _normal = new(foreground: Color.White);
    private static readonly SKPaint _errorPaint = new()
    {
        Color = SKColor.Parse("FF0000"),
        IsStroke = true,
        Typeface = _boldTypeface,
    };
    private static readonly SKTypeface _boldTypeface = SKTypeface.FromFamilyName(
        "Consolas",
        weight: SKFontStyleWeight.ExtraBold,
        width: SKFontStyleWidth.Normal,
        slant: SKFontStyleSlant.Upright
    );

    private string _chartPanelControlSequence = string.Empty;

    public Task<IRenderable> GetContentAsync()
    {
        var timingTower = GetTimingTower();

        _chartPanelControlSequence = GetChartPanel();

        var layout = new Layout("Root").SplitRows(new Layout("Timing Tower", timingTower));

        return Task.FromResult<IRenderable>(layout);
    }

    /// <inheritdoc />
    public async Task PostContentDrawAsync()
    {
        await Terminal.OutAsync(ControlSequences.MoveCursorTo(0, LEFT_OFFSET));
        await Terminal.OutAsync(_chartPanelControlSequence);
    }

    private IRenderable GetTimingTower()
    {
        var selectedLapNumber = state.CursorOffset + 1;
        var selectedLapDrivers = timingData.DriversByLap.GetValueOrDefault(selectedLapNumber);
        var previousLapDrivers = timingData.DriversByLap.GetValueOrDefault(selectedLapNumber - 1);

        if (selectedLapDrivers is null)
            return new Text($"No Data for Lap {selectedLapNumber}");

        var table = new Table();
        table
            .AddColumns(
                $"LAP {selectedLapNumber, 2}/{lapCount.Latest?.TotalLaps}",
                "Gap",
                "Interval",
                "Last Lap",
                "S1",
                "S2",
                "S3",
                " "
            )
            .NoBorder();

        foreach (var (driverNumber, line) in selectedLapDrivers.OrderBy(x => x.Value.Line))
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var previousLap = previousLapDrivers?.GetValueOrDefault(driverNumber) ?? new();
            var teamColour = driver.TeamColour ?? "000000";

            var driverTagDecoration = state.SelectedDrivers.Contains(driverNumber)
                ? Decoration.None
                : Decoration.Dim;

            table.AddRow(
                DisplayUtils.DriverTag(
                    driver,
                    line,
                    positionChange: line.Line - previousLap.Line,
                    decoration: driverTagDecoration
                ),
                new Markup(
                    $"{line.GapToLeader}{GetMarkedUp(line.GapToLeaderSeconds() - previousLap.GapToLeaderSeconds())}"
                        ?? "",
                    _normal
                ),
                new Markup(
                    $"{line.IntervalToPositionAhead?.Value}{GetMarkedUp(line.IntervalToPositionAhead?.IntervalSeconds() - previousLap.IntervalToPositionAhead?.IntervalSeconds())}"
                        ?? "",
                    _normal
                ),
                new Text(line.LastLapTime?.Value ?? "NULL", GetStyle(line.LastLapTime)),
                new Text(
                    line.Sectors.GetValueOrDefault("0")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("0"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("1")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("1"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("2")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("2"))
                )
            );
        }

        return table;
    }

    private string GetMarkedUp(decimal? time) =>
        time switch
        {
            < 0 => $"[green dim italic]{time}[/]",
            < 0.5m => $"[grey62 dim italic]+{time}[/]",
            null => "",
            _ => $"[yellow dim italic]+{time}[/]",
        };

    private string GetPositionChangeMarkup(int? change) =>
        change switch
        {
            < 0 => "[green]▲[/]",
            > 0 => "[yellow]▼[/]",
            _ => "",
        };

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    private Style GetStyle(TimingDataPoint.Driver.LapSectorTime? time)
    {
        if (time is null)
            return _normal;
        if (time.OverallFastest ?? false)
            return _overallBest;
        if (time.PersonalFastest ?? false)
            return _personalBest;
        return _normal;
    }

    private string GetChartPanel()
    {
        if (
            !terminalInfo.IsITerm2ProtocolSupported.Value
            && !terminalInfo.IsKittyProtocolSupported.Value
        )
        {
            return string.Empty;
        }

        if (!sessionInfo.Latest.IsRace())
        {
            return string.Empty;
        }

        var widthCells = Terminal.Size.Width - LEFT_OFFSET;
        var heightCells = Terminal.Size.Height - BOTTOM_OFFSET;
        var ratio = widthCells * 1d / heightCells;

        var heightPixels = 1000;
        var widthPixels = (int)Math.Ceiling(heightPixels * ratio);

        var surface = SKSurface.Create(new SKImageInfo(widthPixels, heightPixels));
        var canvas = surface.Canvas;

        var gapSeriesData = driverList
            .Latest.Where(x => x.Key != "_kf") // Data quirk, dictionaries include _kf which obviously isn't a driver
            .ToDictionary(x => x.Key, _ => new List<double?>());
        var lapSeriesData = driverList
            .Latest.Where(x => x.Key != "_kf") // Data quirk, dictionaries include _kf which obviously isn't a driver
            .ToDictionary(x => x.Key, _ => new List<double?>());

        var fastestLap = default(TimeSpan);

        // Only use data from the last LAPS_IN_CHART laps
        foreach (
            var (lap, lines) in timingData
                .DriversByLap.OrderBy(x => x.Key)
                .Skip(state.CursorOffset - LAPS_IN_CHART + 1)
                .Take(LAPS_IN_CHART)
        )
        {
            // Discard laps slower than 105% of the fastest car on that lap
            // This should discard laps where cars pit, as those laps aren't very useful
            fastestLap =
                lines.Min(x => x.Value.LastLapTime?.ToTimeSpan()) ?? TimeSpan.FromMinutes(2);
            var threshold = fastestLap * 1.05;
            foreach (var (driver, timingData) in lines)
            {
                // Lapped cars don't have a gap to leader, so null them out
                // We can't just null non-numbers though, because P1 should have a gap of 0
                if (!timingData.GapToLeader?.Contains(" L") ?? true)
                {
                    gapSeriesData[driver].Add((double)(timingData.GapToLeaderSeconds() ?? 0));
                }
                else
                {
                    gapSeriesData[driver].Add(null);
                }

                var lapTime = timingData.LastLapTime?.ToTimeSpan();
                if (lapTime > threshold)
                {
                    lapSeriesData[driver].Add(null);
                }
                else
                {
                    lapSeriesData[driver].Add(lapTime?.TotalMilliseconds);
                }
            }
        }

        var gapSeries = gapSeriesData
            .Select(x =>
            {
                var driver = driverList.Latest.GetValueOrDefault(x.Key) ?? new();
                var colour = driver.TeamColour ?? "FFFFFF";
                return new LineSeries<double?>(x.Value)
                {
                    Name = x.Key,
                    Fill = new SolidColorPaint(SKColors.Transparent),
                    GeometryStroke = null,
                    GeometryFill = null,
                    Stroke = new SolidColorPaint(SKColor.Parse(driver.TeamColour))
                    {
                        StrokeThickness = 2,
                    },
                    IsVisible = state.SelectedDrivers.Contains(x.Key),
                    LineSmoothness = 0,
                    DataLabelsFormatter = p =>
                        p.Index == x.Value.Count - 1 ? driver.Tla! : string.Empty,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Right,
                    DataLabelsSize = 16,
                    DataLabelsPaint = new SolidColorPaint(SKColor.Parse(driver.TeamColour)),
                    DataPadding = new LiveChartsCore.Drawing.LvcPoint(1, 0),
                };
            })
            .ToArray();

        var lapSeries = lapSeriesData
            .Select(x =>
            {
                var driver = driverList.Latest.GetValueOrDefault(x.Key) ?? new();
                var colour = driver.TeamColour ?? "FFFFFF";
                return new LineSeries<double?>(x.Value)
                {
                    Name = x.Key,
                    Fill = new SolidColorPaint(SKColors.Transparent),
                    GeometryStroke = null,
                    GeometryFill = null,
                    Stroke = new SolidColorPaint(SKColor.Parse(driver.TeamColour))
                    {
                        StrokeThickness = 2,
                    },
                    IsVisible = state.SelectedDrivers.Contains(x.Key),
                    LineSmoothness = 0,
                    DataLabelsFormatter = p =>
                        p.Index == x.Value.Count - 1 ? driver.Tla! : string.Empty,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Right,
                    DataLabelsSize = 16,
                    DataLabelsPaint = new SolidColorPaint(SKColor.Parse(driver.TeamColour)),
                    DataPadding = new LiveChartsCore.Drawing.LvcPoint(1, 0),
                };
            })
            .ToArray();

        var gapChart = CreateChart(
            gapSeries,
            "Gap to Leader (s)",
            heightPixels / 2,
            widthPixels,
            labeler: Labelers.Default,
            axisMin: 0
        );
        gapChart.DrawOnCanvas(canvas);

        var lapChart = CreateChart(
            lapSeries,
            "Lap Time",
            heightPixels / 2,
            widthPixels,
            labeler: v => TimeSpan.FromMilliseconds(v).ToString("mm':'ss"),
            axisMax: fastestLap.TotalMilliseconds * 1.05,
            yMinStep: 1000
        );
        var lapChartImage = lapChart.GetImage();
        canvas.DrawImage(lapChartImage, new SKPoint(0, 500));

        if (options.Value.Verbose)
        {
            // Add some debug information when verbose mode is on
            canvas.DrawRect(0, 0, widthPixels - 1, heightPixels - 1, _errorPaint);
            canvas.DrawText($"Ratio: {ratio}", 5, 20, _errorPaint);
        }

        var imageData = surface.Snapshot().Encode();
        var base64 = Convert.ToBase64String(imageData.AsSpan());

        if (terminalInfo.IsKittyProtocolSupported.Value)
        {
            return TerminalGraphics.KittyGraphicsSequenceDelete()
                + TerminalGraphics.KittyGraphicsSequence(heightCells, widthCells, base64);
        }
        else if (terminalInfo.IsITerm2ProtocolSupported.Value)
        {
            return TerminalGraphics.ITerm2GraphicsSequence(heightCells, widthCells, base64);
        }

        return "Unexpected error, shouldn't have got here. Please report!";
    }

    private SKCartesianChart CreateChart(
        LineSeries<double?>[] series,
        string title,
        int height,
        int width,
        Func<double, string> labeler,
        double? axisMin = null,
        double? axisMax = null,
        double yMinStep = 0
    )
    {
        var axisStartLap = state.CursorOffset - LAPS_IN_CHART + 1;
        return new SKCartesianChart
        {
            Series = series,
            Height = height,
            Width = width,
            Background = SKColors.Transparent,
            Title = new LabelVisual
            {
                Text = title,
                Paint = new SolidColorPaint(SKColors.White),
                TextSize = 20,
            },
            XAxes =
            [
                new Axis
                {
                    MinStep = 1,
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    Labeler = v =>
                        axisStartLap > 0 ? (v + axisStartLap + 1).ToString() : (v + 1).ToString(),
                },
            ],
            YAxes =
            [
                new Axis
                {
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray),
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    MinLimit = axisMin,
                    MaxLimit = axisMax,
                    Labeler = labeler,
                    MinStep = yMinStep,
                },
            ],
        };
    }
}
