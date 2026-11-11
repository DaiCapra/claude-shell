namespace ClaudeShell;

public enum SortOrder
{
    RecentlyUsed,
    MostSessions,
    Name
}

public record SortOption(SortOrder Value, string Label);
