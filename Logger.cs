using robotManager.Helpful;
using System.Drawing;

public class Logger
{
    private static bool _debug = false;
    private static string PluginName = "Wholesome Auto Equip";

    public static void LogFight(string message)
    {
        Logging.Write($"[{PluginName}]: { message}", Logging.LogType.Fight, Color.ForestGreen);
    }

    public static void LogError(string message)
    {
        Logging.Write($"[{PluginName}]: {message}", Logging.LogType.Error, Color.DarkRed);
    }

    public static void Log(string message)
    {
        Logging.Write($"[{PluginName}]: {message}", Logging.LogType.Normal, Color.DarkMagenta);
    }

    public static void Log(string message, Color c)
    {
        Logging.Write($"[{PluginName}]: {message}", Logging.LogType.Normal, c);
    }

    public static void LogDebug(string message)
    {
        if (_debug)
            Logging.WriteDebug($"[{PluginName}]: {message}");
    }

    public static void CombatDebug(string message)
    {
        Logging.Write($"[{PluginName}]: {message}", Logging.LogType.Normal, Color.Plum);
    }
}
