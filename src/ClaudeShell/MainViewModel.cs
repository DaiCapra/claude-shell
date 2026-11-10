using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClaudeShell;

public class MainViewModel : INotifyPropertyChanged
{
    readonly IProjectScanner _scanner;
    readonly IProjectLauncher _launcher;
    readonly Func<DateTime> _now;
    readonly HiddenProjects _hidden;

    List<ClaudeProject> _all = [];
    string _filter = "";
    SortOrder _sortOrder = SortOrder.RecentlyUsed;
    ProjectItem? _selected;

    public MainViewModel(
        IProjectScanner scanner,
        IProjectLauncher launcher,
        Func<DateTime>? now = null,
        HiddenProjects? hidden = null)
    {
        _scanner = scanner;
        _launcher = launcher;
        _now = now ?? (() => DateTime.Now);
        _hidden = hidden ?? new HiddenProjects();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ProjectItem> Projects { get; } = [];

    public IReadOnlyList<SortOption> SortOptions { get; } =
    [
        new(SortOrder.RecentlyUsed, "Recently used"),
        new(SortOrder.MostSessions, "Most sessions"),
        new(SortOrder.Name, "Name A-Z")
    ];

    public SortOrder SortOrder
    {
        get => _sortOrder;
        set
        {
            if (_sortOrder == value)
            {
                return;
            }

            _sortOrder = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public string Filter
    {
        get => _filter;
        set
        {
            if (_filter == value)
            {
                return;
            }

            _filter = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public ProjectItem? Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = value;
            OnPropertyChanged();
        }
    }

    public void Refresh()
    {
        _all = _scanner.Scan().ToList();
        _hidden.Prune(_all);
        ApplyFilter();
    }

    public void Hide(ProjectItem item)
    {
        var index = Projects.IndexOf(item);

        _hidden.Hide(item.Path, _now());
        ApplyFilter();

        if (Projects.Count > 0)
        {
            Selected = Projects[Math.Clamp(index, 0, Projects.Count - 1)];
        }
    }

    public void MoveSelection(int delta)
    {
        if (Projects.Count == 0)
        {
            return;
        }

        var index = Selected is null ? -1 : Projects.IndexOf(Selected);
        var next = Math.Clamp(index + delta, 0, Projects.Count - 1);

        Selected = Projects[next];
    }

    public void OpenInExplorer() => Launch(_launcher.OpenInExplorer);

    public void OpenClaude() => Launch(_launcher.OpenClaude);

    void Launch(Action<string> action)
    {
        if (Selected is not null)
        {
            action(Selected.Path);
        }
    }

    void ApplyFilter()
    {
        var now = _now();
        var text = _filter.Trim();

        var items = _all
            .Where(p => !_hidden.IsHidden(p))
            .Where(p => text.Length == 0 || p.Path.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Select(p => new ProjectItem(p, RelativeTime.Format(p.LastUsed, now)));

        var matches = _sortOrder switch
        {
            SortOrder.MostSessions => items
                .OrderByDescending(i => i.Project.SessionCount)
                .ThenByDescending(i => i.Project.LastUsed),
            SortOrder.Name => items
                .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.Path, StringComparer.OrdinalIgnoreCase),
            _ => items.OrderByDescending(i => i.Project.LastUsed)
        };

        Projects.Clear();

        foreach (var item in matches)
        {
            Projects.Add(item);
        }

        Selected = Projects.FirstOrDefault();
    }

    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
