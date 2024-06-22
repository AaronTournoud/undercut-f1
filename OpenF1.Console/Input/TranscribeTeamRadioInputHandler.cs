using Instances.Exceptions;
using OpenF1.Data;

namespace OpenF1.Console;

public sealed class TranscribeTeamRadioInputHandler(
    State state,
    TeamRadioProcessor teamRadio,
    ILogger<TranscribeTeamRadioInputHandler> logger
) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.TeamRadio];

    public ConsoleKey[] Keys => [ConsoleKey.T];

    public string Description => _task switch
    {
        null or { IsCompletedSuccessfully: true } => "Transcribe",
        { IsCompleted: false } => "Transcribing...",
        _ => "Transcribe (Errored)"
    };

    public int Sort => 41;

    private Task? _task;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        switch (_task)
        {
            case { IsCompleted: false }:
                logger.LogInformation("Asked to start transcribing, but already working");
                break;
            default:
                _task = Task.Run(() => TranscribeAsync(state.CursorOffset));
                break;
        }
        return Task.CompletedTask;
    }

    private async Task TranscribeAsync(int offset)
    {
        var radio = teamRadio.Ordered.ElementAtOrDefault(offset);
        try
        {
            await teamRadio.TranscribeAsync(radio.Key);
        }
        catch (InstanceFileNotFoundException ex)
        {
            var text = """
            Failed to transcribe, likely because ffmpeg could not be found installed on your computer. 
            We use FFMpegCore to convert audio files from mp3 to wav, and it requires ffmpeg. 
            Visit https://github.com/rosenbjerg/FFMpegCore?tab=readme-ov-file#binaries to learn how to install.
            """;

            logger.LogError(ex, text);
            radio.Value.Transcription = text;
        }
        catch (Exception ex)
        {
            var text = $"""
            Failed to transcribe, due to an unknown error.
            Team Radio File Path: {radio.Value.DownloadedFilePath}
            """;
            logger.LogError(ex, text);
        }
    }
}
