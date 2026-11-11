namespace ClaudeShell;

public record AppSettings
{
    public HotKeySetting HotKey { get; init; } = HotKeySetting.Default;

    public SortOrder SortOrder { get; init; } = SortOrder.RecentlyUsed;

    public Dictionary<string, DateTime> Hidden { get; init; } = [];
}
