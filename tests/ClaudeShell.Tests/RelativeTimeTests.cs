namespace ClaudeShell.Tests;

public class RelativeTimeTests
{
    static readonly DateTime Now = new(2026, 11, 9, 12, 0, 0);

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(5 * 60, "5 min ago")]
    [InlineData(60 * 60, "1 hour ago")]
    [InlineData(3 * 60 * 60, "3 hours ago")]
    [InlineData(24 * 60 * 60, "1 day ago")]
    [InlineData(5 * 24 * 60 * 60, "5 days ago")]
    [InlineData(60 * 24 * 60 * 60, "2 months ago")]
    [InlineData(400 * 24 * 60 * 60, "1 year ago")]
    public void Formats_the_gap(int secondsAgo, string expected)
    {
        Assert.Equal(expected, RelativeTime.Format(Now.AddSeconds(-secondsAgo), Now));
    }
}
