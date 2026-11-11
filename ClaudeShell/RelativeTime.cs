namespace ClaudeShell;

public static class RelativeTime
{
    public static string Format(DateTime then, DateTime now)
    {
        var span = now - then;

        if (span < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (span < TimeSpan.FromHours(1))
        {
            return $"{(int)span.TotalMinutes} min ago";
        }

        if (span < TimeSpan.FromDays(1))
        {
            return Plural((int)span.TotalHours, "hour");
        }

        if (span < TimeSpan.FromDays(30))
        {
            return Plural((int)span.TotalDays, "day");
        }

        if (span < TimeSpan.FromDays(365))
        {
            return Plural((int)(span.TotalDays / 30), "month");
        }

        return Plural((int)(span.TotalDays / 365), "year");
    }

    static string Plural(int count, string unit) =>
        count == 1 ? $"1 {unit} ago" : $"{count} {unit}s ago";
}
