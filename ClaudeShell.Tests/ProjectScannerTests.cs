using System.Text.Json;

namespace ClaudeShell.Tests;

public sealed class ProjectScannerTests : IDisposable
{
    readonly string _temp = Path.Combine(Path.GetTempPath(), "claude-shell-tests-" + Guid.NewGuid());
    readonly string _root;

    public ProjectScannerTests()
    {
        _root = Path.Combine(_temp, "projects");
        Directory.CreateDirectory(_root);
    }

    public void Dispose() => Directory.Delete(_temp, true);

    string MakeWorkDir(string name)
    {
        var dir = Path.Combine(_temp, "work", name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    void AddSession(string folderName, string? cwd, DateTime lastWrite, string file = "session.jsonl")
    {
        var folder = Path.Combine(_root, folderName);
        Directory.CreateDirectory(folder);

        var lines = new List<string> { """{"type":"summary"}""" };

        if (cwd is not null)
        {
            lines.Add(JsonSerializer.Serialize(new { type = "user", cwd }));
        }

        var path = Path.Combine(folder, file);
        File.WriteAllLines(path, lines);
        File.SetLastWriteTime(path, lastWrite);
    }

    [Fact]
    public void Returns_nothing_when_root_is_missing()
    {
        var scanner = new ProjectScanner(Path.Combine(_temp, "nope"));

        Assert.Empty(scanner.Scan());
    }

    [Fact]
    public void Reads_cwd_from_the_session_file()
    {
        var dir = MakeWorkDir("a");
        AddSession("a", dir, new DateTime(2026, 1, 1));

        var result = new ProjectScanner(_root).Scan();

        var project = Assert.Single(result);
        Assert.Equal(dir, project.Path);
        Assert.Equal(new DateTime(2026, 1, 1), project.LastUsed);
    }

    [Fact]
    public void Sorts_newest_first()
    {
        var old = MakeWorkDir("old");
        var recent = MakeWorkDir("recent");
        AddSession("old", old, new DateTime(2026, 1, 1));
        AddSession("recent", recent, new DateTime(2026, 6, 1));

        var result = new ProjectScanner(_root).Scan();

        Assert.Equal([recent, old], result.Select(p => p.Path));
    }

    [Fact]
    public void Uses_the_newest_session_file_in_a_folder()
    {
        var dir = MakeWorkDir("a");
        AddSession("a", dir, new DateTime(2026, 1, 1), "one.jsonl");
        AddSession("a", dir, new DateTime(2026, 3, 1), "two.jsonl");

        var project = Assert.Single(new ProjectScanner(_root).Scan());

        Assert.Equal(new DateTime(2026, 3, 1), project.LastUsed);
    }

    [Fact]
    public void Skips_folders_that_no_longer_exist()
    {
        AddSession("gone", Path.Combine(_temp, "work", "deleted"), new DateTime(2026, 1, 1));

        Assert.Empty(new ProjectScanner(_root).Scan());
    }

    [Fact]
    public void Skips_folders_without_a_cwd_or_session_files()
    {
        AddSession("nocwd", null, new DateTime(2026, 1, 1));
        Directory.CreateDirectory(Path.Combine(_root, "empty"));

        Assert.Empty(new ProjectScanner(_root).Scan());
    }

    [Fact]
    public void Merges_entries_with_the_same_cwd_and_keeps_the_newest()
    {
        var dir = MakeWorkDir("a");
        AddSession("a-1", dir, new DateTime(2026, 1, 1));
        AddSession("a-2", dir, new DateTime(2026, 5, 1));

        var project = Assert.Single(new ProjectScanner(_root).Scan());

        Assert.Equal(new DateTime(2026, 5, 1), project.LastUsed);
    }

    [Fact]
    public void Counts_the_session_files_in_a_folder()
    {
        var dir = MakeWorkDir("a");
        AddSession("a", dir, new DateTime(2026, 1, 1), "one.jsonl");
        AddSession("a", dir, new DateTime(2026, 2, 1), "two.jsonl");
        AddSession("a", dir, new DateTime(2026, 3, 1), "three.jsonl");

        var project = Assert.Single(new ProjectScanner(_root).Scan());

        Assert.Equal(3, project.SessionCount);
    }

    [Fact]
    public void Adds_up_session_counts_when_merging_the_same_cwd()
    {
        var dir = MakeWorkDir("a");
        AddSession("a-1", dir, new DateTime(2026, 1, 1), "one.jsonl");
        AddSession("a-1", dir, new DateTime(2026, 1, 2), "two.jsonl");
        AddSession("a-2", dir, new DateTime(2026, 5, 1), "three.jsonl");

        var project = Assert.Single(new ProjectScanner(_root).Scan());

        Assert.Equal(3, project.SessionCount);
    }
}
