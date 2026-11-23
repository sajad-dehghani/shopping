namespace Azma.Web.Infrastructure;

/// <summary>
/// Writes fatal startup errors to logs/startup-error.txt next to the app. On shared IIS hosting the only sign of a
/// failed start is "HTTP Error 500.30"; this file (readable from the host's file manager) shows the real cause.
/// The logs folder is outside wwwroot and is never served to visitors.
/// </summary>
public static class StartupLog
{
    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "logs", "startup-error.txt");

    public static void Write(string stage, object error)
    {
        var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Startup failed at: {stage}{Environment.NewLine}{Hint(error)}{error}{Environment.NewLine}{Environment.NewLine}";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.AppendAllText(FilePath, text);
        }
        catch
        {
            // The app folder itself is not writable: nothing more we can do here; stdout logging in web.config still works.
        }
        Console.Error.WriteLine(text);
    }

    /// <summary>A plain-language hint for the most common hosting mistakes.</summary>
    private static string Hint(object error)
    {
        var all = error.ToString() ?? "";
        if (all.Contains("SqlException") || all.Contains("network-related") || all.Contains("Login failed"))
            return "HINT: The database could not be reached or the login was refused. Check ConnectionStrings__DefaultConnection (server, database name, user, password) and that the user is db_owner (needed to create tables)." + Environment.NewLine;
        if (all.Contains("UnauthorizedAccessException") || all.Contains("Access to the path"))
            return "HINT: The site has no write permission on a folder. Give the site user Write permission on uploads, private-uploads, keys and logs." + Environment.NewLine;
        return "";
    }
}
