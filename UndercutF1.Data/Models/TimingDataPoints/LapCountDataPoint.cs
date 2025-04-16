namespace UndercutF1.Data;

/// <summary>
/// Sample: { "CurrentLap": 3, "TotalLaps": 71, "_kf": true }
/// </summary>
public sealed class LapCountDataPoint : ILiveTimingDataPoint
{
    /// <inheritdoc />
    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.LapCount;

    public int? CurrentLap { get; set; }
    public int? TotalLaps { get; set; }
}
