using NSubstitute;

namespace ClaudeShell.Tests;

public class MainViewModelTests
{
    static readonly DateTime Now = new(2026, 11, 9, 12, 0, 0);

    readonly IProjectScanner _scanner = Substitute.For<IProjectScanner>();
    readonly IProjectLauncher _launcher = Substitute.For<IProjectLauncher>();

    MainViewModel Create(params string[] paths)
    {
        _scanner.Scan().Returns(paths
            .Select((p, i) => new ClaudeProject(p, Now.AddDays(-i)))
            .ToList());

        var viewModel = new MainViewModel(_scanner, _launcher, () => Now);
        viewModel.Refresh();

        return viewModel;
    }

    MainViewModel CreateWith(params ClaudeProject[] projects)
    {
        _scanner.Scan().Returns(projects.ToList());

        var viewModel = new MainViewModel(_scanner, _launcher, () => Now);
        viewModel.Refresh();

        return viewModel;
    }

    [Fact]
    public void Hide_removes_the_item_and_selects_its_neighbour()
    {
        var viewModel = Create(@"C:\a\one", @"C:\a\two", @"C:\a\three");

        viewModel.Hide(viewModel.Projects[1]);

        Assert.Equal([@"C:\a\one", @"C:\a\three"], viewModel.Projects.Select(p => p.Path));
        Assert.Equal(@"C:\a\three", viewModel.Selected?.Path);
    }

    [Fact]
    public void Hiding_the_last_item_selects_the_one_before()
    {
        var viewModel = Create(@"C:\a\one", @"C:\a\two");

        viewModel.Hide(viewModel.Projects[1]);

        Assert.Equal(@"C:\a\one", viewModel.Selected?.Path);
    }

    [Fact]
    public void Hiding_the_only_item_leaves_nothing_selected()
    {
        var viewModel = Create(@"C:\a\one");

        viewModel.Hide(viewModel.Projects[0]);

        Assert.Empty(viewModel.Projects);
        Assert.Null(viewModel.Selected);
    }

    [Fact]
    public void A_hidden_item_stays_hidden_after_a_refresh()
    {
        var viewModel = Create(@"C:\a\one", @"C:\a\two");

        viewModel.Hide(viewModel.Projects[0]);
        viewModel.Refresh();

        Assert.Equal([@"C:\a\two"], viewModel.Projects.Select(p => p.Path));
    }

    [Fact]
    public void A_hidden_item_comes_back_when_the_folder_is_used_again()
    {
        var viewModel = CreateWith(new ClaudeProject(@"C:\a\one", Now.AddDays(-1)));

        viewModel.Hide(viewModel.Projects[0]);
        Assert.Empty(viewModel.Projects);

        _scanner.Scan().Returns(new List<ClaudeProject> { new(@"C:\a\one", Now.AddHours(1)) });
        viewModel.Refresh();

        Assert.Equal([@"C:\a\one"], viewModel.Projects.Select(p => p.Path));
    }

    [Fact]
    public void Sorts_by_most_recently_used_by_default()
    {
        var viewModel = CreateWith(
            new ClaudeProject(@"C:\a\old", Now.AddDays(-9), 50),
            new ClaudeProject(@"C:\a\new", Now.AddDays(-1), 2));

        Assert.Equal([@"C:\a\new", @"C:\a\old"], viewModel.Projects.Select(p => p.Path));
    }

    [Fact]
    public void Sorts_by_most_sessions()
    {
        var viewModel = CreateWith(
            new ClaudeProject(@"C:\a\few", Now.AddDays(-1), 2),
            new ClaudeProject(@"C:\a\many", Now.AddDays(-9), 50),
            new ClaudeProject(@"C:\a\tie", Now.AddDays(-3), 2));

        viewModel.SortOrder = SortOrder.MostSessions;

        Assert.Equal([@"C:\a\many", @"C:\a\few", @"C:\a\tie"], viewModel.Projects.Select(p => p.Path));
    }

    [Fact]
    public void Sorts_by_name_ignoring_case()
    {
        var viewModel = CreateWith(
            new ClaudeProject(@"C:\x\banana", Now),
            new ClaudeProject(@"C:\y\Apple", Now),
            new ClaudeProject(@"C:\z\cherry", Now));

        viewModel.SortOrder = SortOrder.Name;

        Assert.Equal(["Apple", "banana", "cherry"], viewModel.Projects.Select(p => p.Name));
    }

    [Fact]
    public void Changing_the_sort_selects_the_first_item()
    {
        var viewModel = CreateWith(
            new ClaudeProject(@"C:\a\few", Now.AddDays(-1), 2),
            new ClaudeProject(@"C:\a\many", Now.AddDays(-9), 50));

        viewModel.SortOrder = SortOrder.MostSessions;

        Assert.Equal(@"C:\a\many", viewModel.Selected?.Path);
    }

    [Fact]
    public void Sorting_keeps_the_filter()
    {
        var viewModel = CreateWith(
            new ClaudeProject(@"C:\a\one", Now, 1),
            new ClaudeProject(@"C:\b\two", Now, 9));

        viewModel.Filter = @"C:\a";
        viewModel.SortOrder = SortOrder.MostSessions;

        Assert.Equal([@"C:\a\one"], viewModel.Projects.Select(p => p.Path));
    }

    [Theory]
    [InlineData(1, "1 session")]
    [InlineData(0, "0 sessions")]
    [InlineData(12, "12 sessions")]
    public void Items_show_the_session_count(int count, string expected)
    {
        var viewModel = CreateWith(new ClaudeProject(@"C:\a\one", Now, count));

        Assert.Equal(expected, viewModel.Projects[0].SessionsText);
    }

    [Fact]
    public void Refresh_lists_projects_and_selects_the_first()
    {
        var viewModel = Create(@"C:\a\one", @"C:\a\two");

        Assert.Equal(2, viewModel.Projects.Count);
        Assert.Equal(@"C:\a\one", viewModel.Selected?.Path);
    }

    [Fact]
    public void Filter_matches_the_path_ignoring_case()
    {
        var viewModel = Create(@"C:\Data\Godot\Trains", @"C:\Data\Unity\sparrow");

        viewModel.Filter = "TRAINS";

        Assert.Equal([@"C:\Data\Godot\Trains"], viewModel.Projects.Select(p => p.Path));
    }

    [Fact]
    public void Filter_selects_the_first_match()
    {
        var viewModel = Create(@"C:\a\one", @"C:\b\two", @"C:\b\three");

        viewModel.Filter = @"C:\b";

        Assert.Equal(@"C:\b\two", viewModel.Selected?.Path);
    }

    [Fact]
    public void Filter_with_no_match_clears_the_selection()
    {
        var viewModel = Create(@"C:\a\one");

        viewModel.Filter = "zzz";

        Assert.Empty(viewModel.Projects);
        Assert.Null(viewModel.Selected);
    }

    [Fact]
    public void MoveSelection_stays_within_the_list()
    {
        var viewModel = Create(@"C:\a\one", @"C:\a\two");

        viewModel.MoveSelection(-1);
        Assert.Equal(@"C:\a\one", viewModel.Selected?.Path);

        viewModel.MoveSelection(1);
        viewModel.MoveSelection(1);
        Assert.Equal(@"C:\a\two", viewModel.Selected?.Path);
    }

    [Fact]
    public void Items_show_name_and_relative_time()
    {
        var viewModel = Create(@"C:\a\one", @"C:\a\two");

        Assert.Equal("one", viewModel.Projects[0].Name);
        Assert.Equal("just now", viewModel.Projects[0].LastUsedText);
        Assert.Equal("1 day ago", viewModel.Projects[1].LastUsedText);
    }

    [Fact]
    public void Actions_call_the_launcher_with_the_selected_path()
    {
        var viewModel = Create(@"C:\a\one");

        viewModel.OpenInExplorer();
        viewModel.OpenInTerminal();
        viewModel.ContinueClaude();

        _launcher.Received(1).OpenInExplorer(@"C:\a\one");
        _launcher.Received(1).OpenInTerminal(@"C:\a\one");
        _launcher.Received(1).ContinueClaude(@"C:\a\one");
    }

    [Fact]
    public void Actions_do_nothing_without_a_selection()
    {
        var viewModel = Create();

        viewModel.OpenInExplorer();
        viewModel.OpenInTerminal();
        viewModel.ContinueClaude();

        _launcher.DidNotReceiveWithAnyArgs().OpenInExplorer(default!);
        _launcher.DidNotReceiveWithAnyArgs().OpenInTerminal(default!);
        _launcher.DidNotReceiveWithAnyArgs().ContinueClaude(default!);
    }
}
