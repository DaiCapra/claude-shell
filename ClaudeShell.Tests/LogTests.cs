namespace ClaudeShell.Tests;

public sealed class LogTests : IDisposable
{
    readonly string _temp = Path.Combine(Path.GetTempPath(), "claude-shell-log-" + Guid.NewGuid());

    public LogTests()
    {
        Log.Path = Path.Combine(_temp, "log.txt");
    }

    public void Dispose()
    {
        if (Directory.Exists(_temp))
        {
            Directory.Delete(_temp, true);
        }
    }

    [Fact]
    public void Writes_lines_with_level()
    {
        Log.Info("hello");
        Log.Warn("careful");

        var text = File.ReadAllText(Log.Path);

        Assert.Contains("[INFO] hello", text);
        Assert.Contains("[WARN] careful", text);
    }

    [Fact]
    public void Rolls_over_past_one_megabyte_and_keeps_a_single_old_file()
    {
        var line = new string('x', 1000);

        for (var i = 0; i < 2500; i++)
        {
            Log.Info(line);
        }

        var current = new FileInfo(Log.Path);
        var old = new FileInfo(Path.Combine(_temp, "log.old.txt"));

        Assert.True(old.Exists);
        Assert.True(current.Length <= 1024 * 1024 + 2000);
        Assert.True(old.Length <= 1024 * 1024 + 2000);
        Assert.Equal(2, Directory.GetFiles(_temp).Length);
    }
}
