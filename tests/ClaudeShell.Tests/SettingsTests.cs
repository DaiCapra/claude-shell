using System.Windows.Input;

namespace ClaudeShell.Tests;

public sealed class SettingsTests : IDisposable
{
    readonly string _temp = Path.Combine(Path.GetTempPath(), "claude-shell-settings-" + Guid.NewGuid());

    string SettingsPath => Path.Combine(_temp, "sub", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_temp))
        {
            Directory.Delete(_temp, true);
        }
    }

    [Fact]
    public void Default_hotkey_is_ctrl_shift_space()
    {
        Assert.Equal("Ctrl+Shift+Space", HotKeySetting.Default.Display);
    }

    [Fact]
    public void Display_lists_modifiers_in_a_fixed_order()
    {
        var hotKey = new HotKeySetting(ModifierKeys.Windows | ModifierKeys.Alt | ModifierKeys.Control, Key.C);

        Assert.Equal("Ctrl+Alt+Win+C", hotKey.Display);
    }

    [Fact]
    public void Load_returns_defaults_when_the_file_is_missing()
    {
        var settings = new SettingsStore(SettingsPath).Load();

        Assert.Equal(HotKeySetting.Default, settings.HotKey);
    }

    [Fact]
    public void Save_then_load_round_trips()
    {
        var store = new SettingsStore(SettingsPath);
        var hotKey = new HotKeySetting(ModifierKeys.Control | ModifierKeys.Alt, Key.J);

        store.Save(new AppSettings { HotKey = hotKey });

        Assert.Equal(hotKey, store.Load().HotKey);
    }

    [Fact]
    public void Sort_order_defaults_to_recently_used_and_round_trips()
    {
        var store = new SettingsStore(SettingsPath);

        Assert.Equal(SortOrder.RecentlyUsed, store.Load().SortOrder);

        store.Save(new AppSettings { SortOrder = SortOrder.MostSessions });

        Assert.Equal(SortOrder.MostSessions, store.Load().SortOrder);
    }

    [Fact]
    public void Hidden_folders_round_trip()
    {
        var store = new SettingsStore(SettingsPath);
        var when = new DateTime(2026, 11, 9, 12, 0, 0);

        Assert.Empty(store.Load().Hidden);

        store.Save(new AppSettings { Hidden = new Dictionary<string, DateTime> { [@"C:\a\one"] = when } });

        Assert.Equal(when, store.Load().Hidden[@"C:\a\one"]);
    }

    [Fact]
    public void Load_returns_defaults_when_the_file_is_corrupt()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{ not json");

        Assert.Equal(HotKeySetting.Default, new SettingsStore(SettingsPath).Load().HotKey);
    }
}
