using System.Reflection;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace UndercutF1.Console;

public class MainDisplay(IHttpClientFactory httpClientFactory, ILogger<MainDisplay> logger)
    : IDisplay
{
    public Screen Screen => Screen.Main;

    private Task<string?>? _fetchVersionTask = null;
    private string _currentVersion = $"v{ThisAssembly.NuGetPackageVersion}";

    public Task<IRenderable> GetContentAsync()
    {
        var title = new Text(
            """
               __  __  _   __  ____    ______  ____    ______  __  __  ______        ______  ___
              / / / / / | / / / __ \  / ____/ / __ \  / ____/ / / / / /_  __/       / ____/ <  /
             / / / / /  |/ / / / / / / __/   / /_/ / / /     / / / /   / /         / /_     / / 
            / /_/ / / /|  / / /_/ / / /___  / _, _/ / /___  / /_/ /   / /         / __/    / /  
            \____/ /_/ |_/ /_____/ /_____/ /_/ |_|  \____/  \____/   /_/         /_/      /_/   
            """
        ).Centered();

        var content = new Markup(
            """
            Welcome to [bold italic]Undercut F1[/].

            To start a live timing session, press [bold]S[/] then [bold]L[/].
            To start a replay a previously recorded/imported session, press [bold]S[/] then [bold]F[/].

            Once a session is started, navigate to the Timing Tower using [bold]T[/]
            Then use the Arrow Keys [bold]◄[/]/[bold]►[/] to switch between timing pages.
            Use [bold]N[/]/[bold]M[/]/[bold],[/]/[bold].[/] to adjust the stream delay, and [bold]▲[/]/[bold]▼[/] keys to use the cursor.
            Press Shift with these keys to adjust by a higher amount.

            You can download old session data from Formula 1 by running:
            [italic]> undercutf1 import[/]
            """
        );

        var latestVersion = GetLatestVersion();

        var newVersionText =
            latestVersion == _currentVersion
                ? string.Empty
                : $"[green italic]A newer version is available: {latestVersion}[/]";

        var footer = new Markup(
            $"""
            GitHub: https://github.com/JustAman62/undercut-f1
            Version: {ThisAssembly.AssemblyInformationalVersion} {newVersionText}
            """
        );

        var layout = new Layout("Content").SplitRows(
            new Layout("Title", title).Size(8),
            new Layout("Content", content),
            new Layout("Footer", footer).Size(2)
        );
        var panel = new Panel(layout).Expand().RoundedBorder();

        return Task.FromResult<IRenderable>(panel);
    }

    private string GetLatestVersion()
    {
        // We don't want to block anything when fetching the latest version from GitHub
        // So we trigger a background task to fetch, and return the current version until we have a result available.
        switch (_fetchVersionTask)
        {
            case null:
                _fetchVersionTask = Task.Run(GetLatestVersionAsync);
                return _currentVersion;
            case { IsCompletedSuccessfully: true }:
                return _fetchVersionTask.Result ?? _currentVersion;
            default:
                return _currentVersion;
        }
    }

    private async Task<string?> GetLatestVersionAsync()
    {
        var httpClient = httpClientFactory.CreateClient("Default");
        var res = await httpClient.GetFromJsonAsync<GitHubTagEntry[]>(
            "https://api.github.com/repos/justaman62/undercut-f1/tags"
        );
        var tag = res?.FirstOrDefault()?.Name;
        logger.LogInformation("Latest tag from GitHub: {Tag}", tag);
        return tag;
    }

    private record GitHubTagEntry(string Name);
}
