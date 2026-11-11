namespace ClaudeShell.Tests;

public class HiddenProjectsTests
{
    static readonly DateTime Now = new(2026, 11, 9, 12, 0, 0);

    [Fact]
    public void A_hidden_folder_stays_hidden_until_it_is_used_again()
    {
        var hidden = new HiddenProjects();
        hidden.Hide(@"C:\a\one", Now);

        Assert.True(hidden.IsHidden(new ClaudeProject(@"C:\a\one", Now.AddDays(-1))));
        Assert.True(hidden.IsHidden(new ClaudeProject(@"C:\a\one", Now)));
        Assert.False(hidden.IsHidden(new ClaudeProject(@"C:\a\one", Now.AddMinutes(1))));
    }

    [Fact]
    public void Other_folders_are_not_hidden()
    {
        var hidden = new HiddenProjects();
        hidden.Hide(@"C:\a\one", Now);

        Assert.False(hidden.IsHidden(new ClaudeProject(@"C:\a\two", Now.AddDays(-1))));
    }

    [Fact]
    public void Paths_match_ignoring_case()
    {
        var hidden = new HiddenProjects();
        hidden.Hide(@"C:\Data\Programming", Now);

        Assert.True(hidden.IsHidden(new ClaudeProject(@"c:\data\programming", Now.AddDays(-1))));
    }

    [Fact]
    public void Prune_forgets_folders_used_again_and_reports_the_change()
    {
        var hidden = new HiddenProjects();
        var changes = 0;
        hidden.Changed += () => changes++;
        hidden.Hide(@"C:\a\one", Now);

        hidden.Prune([new ClaudeProject(@"C:\a\one", Now.AddHours(1))]);

        Assert.Empty(hidden.Snapshot);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void Prune_keeps_folders_that_are_still_hidden()
    {
        var hidden = new HiddenProjects();
        var changes = 0;
        hidden.Hide(@"C:\a\one", Now);
        hidden.Changed += () => changes++;

        hidden.Prune([new ClaudeProject(@"C:\a\one", Now.AddHours(-1))]);

        Assert.Single(hidden.Snapshot);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Starts_from_saved_entries()
    {
        var hidden = new HiddenProjects(new Dictionary<string, DateTime> { [@"C:\a\one"] = Now });

        Assert.True(hidden.IsHidden(new ClaudeProject(@"C:\a\one", Now.AddDays(-1))));
    }
}
