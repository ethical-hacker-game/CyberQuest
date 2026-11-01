using System;

/// <summary>
/// Resolves the player display name for story text and UI.
/// Authors use the <c>{player}</c> token in content; runtime replaces it with
/// <see cref="PlayerInfo.userName"/>, taken from the unit default name.
/// </summary>
public static class PlayerText
{
    public const string PlayerToken = "{player}";
    public const string DefaultPlayerName = "Alex";

    public static string CurrentName
    {
        get
        {
            string name = GameController.CurrentPlayerInfo?.userName;
            return string.IsNullOrEmpty(name) ? DefaultPlayerName : name;
        }
    }

    public static string Resolve(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf(PlayerToken, StringComparison.Ordinal) < 0)
            return text;

        return text.Replace(PlayerToken, CurrentName);
    }

    public static string GetUnitDefaultName(Unit unit)
    {
        if (unit != null && string.IsNullOrEmpty(unit.defaultPlayerName) == false)
            return unit.defaultPlayerName;

        return DefaultPlayerName;
    }

    public static void ApplyForNewGame(PlayerInfo playerInfo, Unit unit)
    {
        if (playerInfo == null)
            return;

        playerInfo.userName = GetUnitDefaultName(unit);
    }
}
