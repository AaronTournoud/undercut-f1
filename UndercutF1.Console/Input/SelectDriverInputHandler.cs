using UndercutF1.Data;

namespace UndercutF1.Console;

public sealed class SelectDriverInputHandler(State state, TimingDataProcessor timingData)
    : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.DriverTracker];

    public ConsoleKey[] Keys => [ConsoleKey.Enter];

    public string Description => "Toggle Select";

    public int Sort => 40;

    public Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        var selectedDriverNumber = timingData
            .Latest.Lines.FirstOrDefault(x => x.Value.Line == state.CursorOffset)
            .Key;

        if (!state.SelectedDrivers.Remove(selectedDriverNumber))
        {
            state.SelectedDrivers.Add(selectedDriverNumber);
        }

        return Task.CompletedTask;
    }
}
