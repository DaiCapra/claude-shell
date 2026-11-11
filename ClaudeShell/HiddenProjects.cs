namespace ClaudeShell;

/// <summary>
/// Folders the user removed from the list. Each one is remembered with the time it
/// was removed, and stays hidden until Claude is used in it again (a newer session).
/// </summary>
public class HiddenProjects
{
    readonly Dictionary<string, DateTime> _hiddenAt = new(StringComparer.OrdinalIgnoreCase);

    public HiddenProjects(IDictionary<string, DateTime>? initial = null)
    {
        if (initial is null)
        {
            return;
        }

        foreach (var (path, hiddenAt) in initial)
        {
            _hiddenAt[path] = hiddenAt;
        }
    }

    public event Action? Changed;

    public Dictionary<string, DateTime> Snapshot => new(_hiddenAt, StringComparer.OrdinalIgnoreCase);

    public void Hide(string path, DateTime now)
    {
        _hiddenAt[path] = now;
        Changed?.Invoke();
    }

    public bool IsHidden(ClaudeProject project) =>
        _hiddenAt.TryGetValue(project.Path, out var hiddenAt) && project.LastUsed <= hiddenAt;

    /// <summary>Forgets folders that have been used again since they were hidden.</summary>
    public void Prune(IEnumerable<ClaudeProject> projects)
    {
        var removed = false;

        foreach (var project in projects)
        {
            if (_hiddenAt.TryGetValue(project.Path, out var hiddenAt) && project.LastUsed > hiddenAt)
            {
                _hiddenAt.Remove(project.Path);
                removed = true;
            }
        }

        if (removed)
        {
            Changed?.Invoke();
        }
    }
}
