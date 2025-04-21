using AutoMapper;

namespace UndercutF1.Data;

public class TeamRadioProcessor(
    SessionInfoProcessor sessionInfoProcessor,
    ITranscriptionProvider transcriptionProvider,
    IHttpClientFactory httpClientFactory,
    IMapper mapper
) : ProcessorBase<TeamRadioDataPoint>(mapper)
{
    public Dictionary<string, TeamRadioDataPoint.Capture> Ordered =>
        Latest.Captures.Reverse().ToDictionary(x => x.Key, x => x.Value);

    /// <summary>
    /// If the selected team radio hasn't already been downloaded,
    /// downloads it to a temp directory and returns the path to the audio file.
    /// </summary>
    public async Task<string> DownloadTeamRadioToFileAsync(
        string key,
        CancellationToken cancellationToken = default
    )
    {
        var radio = Latest.Captures[key];

        if (
            !string.IsNullOrEmpty(radio.DownloadedFilePath) && File.Exists(radio.DownloadedFilePath)
        )
        {
            return radio.DownloadedFilePath;
        }

        var downloadUri =
            $"https://livetiming.formula1.com/static/{sessionInfoProcessor.Latest.Path}{radio.Path}";
        var destFilePath = $"{Path.GetTempFileName()}.mp3";

        var httpClient = httpClientFactory.CreateClient("Default");
        var downloadStream = await httpClient
            .GetStreamAsync(downloadUri, cancellationToken)
            .ConfigureAwait(false);
        using var fsStream = new FileStream(destFilePath, FileMode.OpenOrCreate);
        await downloadStream.CopyToAsync(fsStream, cancellationToken);

        radio.DownloadedFilePath = destFilePath;
        return destFilePath;
    }

    public async Task TranscribeAsync(string key, CancellationToken cancellationToken = default)
    {
        var radio = Latest.Captures[key];
        var filePath = await DownloadTeamRadioToFileAsync(key, cancellationToken);
        radio.Transcription = await transcriptionProvider.TranscribeFromFileAsync(filePath);
    }
}
