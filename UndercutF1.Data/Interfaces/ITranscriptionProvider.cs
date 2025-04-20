namespace UndercutF1.Data;

public interface ITranscriptionProvider
{
    string ModelPath { get; }
    bool IsModelDownloaded { get; }

    Task<string> TranscribeFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default
    );

    Task EnsureModelDownloaded();
}
