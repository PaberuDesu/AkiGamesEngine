public static class Logger
{
    private static readonly string _logPath = "error_log.txt";
    public static void Log(string message)
    {
        File.AppendAllText(_logPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
    }

    public static void Log(Exception ex)
    {
        File.AppendAllText(_logPath, $"{DateTime.Now}: {ex}{Environment.NewLine}");
    }
}