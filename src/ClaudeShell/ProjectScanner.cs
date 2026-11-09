using System.IO;
using System.Text.Json;

namespace ClaudeShell;

public class ProjectScanner : IProjectScanner
{
    readonly string _root;

    public ProjectScanner(string? root = null)
    {
        _root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude",
            "projects");
    }

    public IReadOnlyList<ClaudeProject> Scan()
    {
        if (!Directory.Exists(_root))
        {
            return [];
        }

        var found = new Dictionary<string, ClaudeProject>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in Directory.EnumerateDirectories(_root))
        {
            var project = ReadProject(folder);

            if (project is null || !Directory.Exists(project.Path))
            {
                continue;
            }

            if (found.TryGetValue(project.Path, out var existing))
            {
                project = project with
                {
                    LastUsed = project.LastUsed > existing.LastUsed ? project.LastUsed : existing.LastUsed,
                    SessionCount = project.SessionCount + existing.SessionCount
                };
            }

            found[project.Path] = project;
        }

        return found.Values.OrderByDescending(p => p.LastUsed).ToList();
    }

    static ClaudeProject? ReadProject(string folder)
    {
        var sessions = new DirectoryInfo(folder)
            .EnumerateFiles("*.jsonl")
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();

        var newest = sessions.FirstOrDefault();

        if (newest is null)
        {
            return null;
        }

        var cwd = ReadCwd(newest.FullName);

        return cwd is null
            ? null
            : new ClaudeProject(Path.GetFullPath(cwd), newest.LastWriteTime, sessions.Count);
    }

    static string? ReadCwd(string file)
    {
        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            while (reader.ReadLine() is { } line)
            {
                if (!line.Contains("\"cwd\""))
                {
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse(line);

                    if (doc.RootElement.TryGetProperty("cwd", out var cwd) &&
                        cwd.ValueKind == JsonValueKind.String)
                    {
                        return cwd.GetString();
                    }
                }
                catch (JsonException)
                {
                }
            }
        }
        catch (IOException)
        {
        }

        return null;
    }
}
