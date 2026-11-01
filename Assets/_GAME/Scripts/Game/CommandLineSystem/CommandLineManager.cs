using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandLineManager : GameSystemComponent
{
    public static CommandLineManager Instance;

    private static Dictionary<string, Command> commands = new Dictionary<string, Command>();

    private const string UNKNOWN_COMMAND_RESULT = "unknown command";

    // Recognized action identifiers. Stored in Command/Subcommand.action,
    // with any required argument stored in actionInfo.
    public const string ACTION_OPEN_BROWSER = "openBrowser";

    public static Dictionary<string, Command> Commands => commands;


    public void PrepareCommands()
    {
        commands = new Dictionary<string, Command>();
        List<Command> loadedCommands = ContentLoader.ReturnListOfType<Command>(
            ContentPaths.ActiveUnitFolder(ContentConstValues.FOLDER_COMMANDS),
            ContentConstValues.EXTENSION_COMMAND);

        for (int i = 0; i < loadedCommands.Count; i++)
        {
            commands.Add(loadedCommands[i].command, loadedCommands[i]);
        }
    }

    public static (string, Command) GetCommandResult(string commandText, Command baseCommand)
    {
        if (baseCommand == null)
            return GetEmptyComamandResult(commandText);

        if(baseCommand.isTool == false)
            return GetEmptyComamandResult(commandText);

        return GetToolCommandResult(commandText, baseCommand);
    }

    private static (string, Command) GetEmptyComamandResult(string commandText) {
        if (GlobeStyleManager.Instance != null)
        {
            switch (commandText)
            {
                //case "show tutorial":
                //case "show next tutorial":
                //case "show sample tutorial":
                //    IndicatorManager.Instance.ShowNextTutorial();
                //    return ("showing next tutorial", null);

                default:
                    break;
            }
        }

        string[] commandElements = commandText.Split(' ',System.StringSplitOptions.RemoveEmptyEntries);
        
        if(commandElements.Length == 0)
            return (UNKNOWN_COMMAND_RESULT, null);

        string coreCommand = commandElements[0];

        if (commands.ContainsKey(coreCommand) == false)
            return ($"{commandText} {UNKNOWN_COMMAND_RESULT}", null);

        Command command = commands[coreCommand];

        if (TryGetLockedMessage(command, out string lockedMessage))
            return (lockedMessage, null);

        if (commandElements.Length == 1)
        {
            ExecuteAction(command.action, command.actionInfo);
            return (command.output, null);
        }

        for (int i = 2; i < commandElements.Length; i++)
        {
            commandElements[1] += " " + commandElements[i];
        }

        for (int i = 0; i < command.subcommands.Count; i++)
        {
            if (commandElements[1].Trim() == command.subcommands[i].subcommand.Trim())
            {
                ExecuteAction(command.subcommands[i].action, command.subcommands[i].actionInfo);
                return (command.subcommands[i].output, null);
            }
        }

        return (command.errorSub, null);
    }

    private static (string, Command) GetToolCommandResult(string commandText, Command baseCommand)
    {
        if (commandText == "exit" || commandText == "quit")
            return ("", null);

        if (TryGetLockedMessage(baseCommand, out string lockedMessage))
            return (lockedMessage, baseCommand);

        for (int i = 0; i < baseCommand.subcommands.Count; i++)
        {
            if (commandText.Trim() == baseCommand.subcommands[i].subcommand.Trim())
            {
                ExecuteAction(baseCommand.subcommands[i].action, baseCommand.subcommands[i].actionInfo);
                return (baseCommand.subcommands[i].output, baseCommand);
            }
        }

        return (baseCommand.errorSub, null);
    }

    /// <summary>
    /// Returns true when the command declares a Lab skill that the player has not unlocked yet.
    /// </summary>
    private static bool TryGetLockedMessage(Command command, out string lockedMessage)
    {
        lockedMessage = null;

        if (command == null || string.IsNullOrWhiteSpace(command.requiredSkillID))
            return false;

        if (SkillsManager.IsUnlocked(command.requiredSkillID))
            return false;

        SkillData skill = SkillsManager.GetSkillByID(command.requiredSkillID);
        string skillLabel = skill != null && string.IsNullOrWhiteSpace(skill.skillName) == false
            ? skill.skillName
            : command.requiredSkillID;

        lockedMessage = $"Command locked. Unlock {skillLabel} in the Lab first.";
        return true;
    }

    /// <summary>
    /// Runs an in-game action declared on a command or subcommand.
    /// <paramref name="action"/> is one of the ACTION_* identifiers,
    /// <paramref name="actionInfo"/> carries the action argument (e.g. a URL).
    /// </summary>
    private static void ExecuteAction(string action, string actionInfo)
    {
        if (string.IsNullOrWhiteSpace(action))
            return;

        switch (action.Trim())
        {
            case ACTION_OPEN_BROWSER:
                OpenBrowserAction(actionInfo);
                break;

            default:
                Debug.LogWarning($"CommandLineManager: unknown command action '{action}'.");
                break;
        }
    }

    private static void OpenBrowserAction(string url)
    {
        if (GameUI.Instance == null)
            return;

        GameUI.Instance.ShowWebBrowser();

        if (string.IsNullOrWhiteSpace(url) == false)
            GameUI.Instance.WebBrowserWindow.OpenWebsite(url);
    }

    private void Awake()
    {
        Instance = this;
        PrepareCommands();
    }
}
