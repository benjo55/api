public static class DebugLogger
{
    public static void Log(string message)
    {
        try
        {
            string path = @"C:\inetpub\wwwroot\api\logs\debug-api.log";
            System.IO.File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            // ESSAIE AUSSI d'écrire dans C:\Temp au cas où
            try
            {
                System.IO.File.AppendAllText(@"C:\Temp\debug-api.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message} (ERROR: {ex.Message}){Environment.NewLine}");
            }
            catch { }
        }
    }
}
