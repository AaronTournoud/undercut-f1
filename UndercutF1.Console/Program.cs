﻿using System.CommandLine;
using UndercutF1.Console;
using UndercutF1.Data;

var rootCommand = new RootCommand("undercutf1");

var isVerboseOption = new Option<bool>(
    ["--verbose", "-v"],
    () => false,
    "Whether verbose logging should be enabled"
);
var isApiEnabledOption = new Option<bool>(
    "--with-api",
    () => false,
    "Whether the API endpoint should be exposed at http://localhost:61937"
);
var dataDirectoryOption = new Option<DirectoryInfo?>(
    "--data-directory",
    "The directory to which timing data will be read from and written to"
);
var logDirectoryOption = new Option<DirectoryInfo?>(
    "--log-directory",
    "The directory to which logs will be written to"
);
var notifyOption = new Option<bool?>(
    "--notify",
    "Whether audible BELs are sent to your terminal when new race control messages are received"
);

rootCommand.AddGlobalOption(isVerboseOption);
rootCommand.AddGlobalOption(isApiEnabledOption);
rootCommand.AddGlobalOption(dataDirectoryOption);
rootCommand.AddGlobalOption(logDirectoryOption);
rootCommand.AddGlobalOption(notifyOption);

rootCommand.SetHandler(
    CommandHandler.Root,
    isApiEnabledOption,
    dataDirectoryOption,
    logDirectoryOption,
    isVerboseOption,
    notifyOption
);

var importCommand = new Command(
    "import",
    """
    Imports data from the F1 Live Timing API, if you have missed recording a session live. 
    The data is imported in a format that can be replayed in real-time using undercutf1.
    """
);

var yearArgument = new Argument<int>("year", "The year the meeting took place.");

var meetingKeyOption = new Option<int?>(
    ["--meeting-key", "--meeting", "-m"],
    "The Meeting Key of the session to import. If not provided, all meetings in the year will be listed."
);

var sessionKeyOption = new Option<int?>(
    ["--session-key", "--session", "-s"],
    "The Session Key of the session inside the selected meeting to import. If not provided, all sessions in the provided meeting will be listed."
);
importCommand.AddArgument(yearArgument);
importCommand.AddOption(meetingKeyOption);
importCommand.AddOption(sessionKeyOption);
importCommand.SetHandler(
    CommandHandler.ImportSession,
    yearArgument,
    meetingKeyOption,
    sessionKeyOption,
    dataDirectoryOption,
    logDirectoryOption,
    isVerboseOption
);

rootCommand.AddCommand(importCommand);

// Provided for backwards compatibility, but the normal import command now lists
var importListCommand = new Command(
    "list",
    """
    [DEPRECATED - Use the import command directly without all options to list meetings and sessions]
    Lists the available Meetings and Sessions that can be imported.
    """
);
importListCommand.AddArgument(yearArgument);
importListCommand.AddOption(meetingKeyOption);
importListCommand.SetHandler(
    CommandHandler.ListMeetings,
    yearArgument,
    meetingKeyOption,
    logDirectoryOption,
    isVerboseOption
);

importCommand.AddCommand(importListCommand);

await rootCommand.InvokeAsync(args);
