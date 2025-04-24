using UndercutF1.Data;

namespace UndercutF1.Console;

public class PauseClockInputHandler(IDateTimeProvider dateTimeProvider) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens =>
        [
            Screen.ManageSession,
            Screen.RaceControl,
            Screen.DriverTracker,
            Screen.TimingTower,
            Screen.TimingHistory,
            Screen.TyreStints,
        ];

    public ConsoleKey[] Keys => [ConsoleKey.P];

    public string Description =>
        dateTimeProvider.IsPaused ? "[olive]Resume Clock[/]" : "Pause Clock";

    public int Sort => 23;

    public Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        dateTimeProvider.TogglePause();

        return Task.CompletedTask;
    }
}
