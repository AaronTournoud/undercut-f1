using UndercutF1.Data;

namespace UndercutF1.Console;

public class NotifyHandler : INotifyHandler
{
    public async Task OnNotificationAsync() =>
        // When notifications are received, send a ASCII BEL to the console to make a noise to alert the user.
        await Terminal.OutAsync(ControlConstants.BEL);
}
