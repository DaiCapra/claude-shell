using System.IO;

namespace ClaudeShell;

// Small append-only log next to settings.json. Rolls over to log.old.txt
// once it passes MaxBytes, so it never grows without bound.
public static class Log
{
    const long MaxBytes = 1024 * 1024;

    static readonly object Gate = new();

    public static string Path { get; set; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClaudeShell",
        "log.txt");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? e = null) =>
        Write("ERROR", e is null ? message : $"{message}{Environment.NewLine}{e}");

    static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";

        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                RollIfNeeded();
                File.AppendAllText(Path, line);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Logging must never take the app down.
            }
        }
    }

    static void RollIfNeeded()
    {
        var file = new FileInfo(Path);

        if (!file.Exists || file.Length < MaxBytes)
        {
            return;
        }

        var old = System.IO.Path.ChangeExtension(Path, ".old.txt");
        File.Move(Path, old, overwrite: true);
    }
}
