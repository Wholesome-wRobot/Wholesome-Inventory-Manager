using robotManager.Helpful;
using System.Drawing;

public class Logger
{
    private static bool _logPerf = false;

    public static void LogFight(string message)
    {
        Logging.Write($"[{Main.PluginName}]: { message}", Logging.LogType.Fight, Color.ForestGreen);
    }

    public static void LogError(string message)
    {
        Logging.Write($"[{Main.PluginName}]: {message}", Logging.LogType.Error, Color.DarkRed);
    }

    public static void Log(string message)
    {
        Logging.Write($"[{Main.PluginName}]: {message}", Logging.LogType.Normal, Color.DarkOrchid);
    }

    public static void Log(string message, Color c)
    {
        Logging.Write($"[{Main.PluginName}]: {message}", Logging.LogType.Normal, c);
    }

    public static void LogDebug(string message)
    {
        Logging.WriteDebug($"[{Main.PluginName}]: {message}");
    }

    public static void LogPerformance(string message)
    {
        if (_logPerf)
            Logging.Write($"[{Main.PluginName}]: {message}", Logging.LogType.Normal, Color.DarkMagenta);
    }

    public static void CombatDebug(string message)
    {
        Logging.Write($"[{Main.PluginName}]: {message}", Logging.LogType.Normal, Color.Plum);
    }
}