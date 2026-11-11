using System.ComponentModel;
using System.Diagnostics;

namespace ClaudeShell;

public class ProcessProjectLauncher : IProjectLauncher
{
    public void OpenInExplorer(string path) =>
        Start("explorer.exe", [path]);

    public void OpenClaude(string path)
    {
        if (!TryStart("wt.exe", ["-d", path, "cmd.exe", "/k", "claude"]))
        {
            StartInFolder("cmd.exe", path, ["/k", "claude"]);
        }
    }

    static void Start(string file, string[] arguments)
    {
        var info = new ProcessStartInfo(file);

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        Process.Start(info);
    }

    static bool TryStart(string file, string[] arguments)
    {
        try
        {
            Start(file, arguments);

            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    static void StartInFolder(string file, string folder, string[] arguments)
    {
        var info = new ProcessStartInfo(file) { WorkingDirectory = folder, UseShellExecute = true };

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        Process.Start(info);
    }
}
