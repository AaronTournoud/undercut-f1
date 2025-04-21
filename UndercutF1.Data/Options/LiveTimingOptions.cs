namespace UndercutF1.Data;

/// <summary>
/// Options to configure the behaviour of live timing.
/// </summary>
public record LiveTimingOptions
{
    public static string BaseDirectory =>
        Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "undercut-f1");

    public static string ConfigFilePath => Path.Join(BaseDirectory, "config.json");

    /// <summary>
    /// The directory to read and store live timing data for simulations.
    /// When live sessions are being listened to, all data received will be recorded in this directory.
    /// This is also the directory that imported data is saved to.
    /// Defaults to <c>~/undercut-f1/data</c>.
    /// </summary>
    public string DataDirectory { get; set; } = Path.Join(BaseDirectory, "data");

    /// <summary>
    /// Whether the app should expose an API at http://localhost:61937.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool ApiEnabled { get; set; } = false;

    /// <summary>
    /// Whether verbose logging should be enabled.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Whether notifications should be sent to the user when new Race Control messages are received.
    /// Blue flag related messages do not result in notifications, but all other messages do.
    /// UndercutF1.Console implements these notifications as <c>BEL</c>s sent to your terminal, resulting in an audible beep.
    /// </summary>
    public bool Notify { get; set; } = true;
}
