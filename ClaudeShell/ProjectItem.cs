namespace ClaudeShell;

public record ProjectItem(ClaudeProject Project, string LastUsedText)
{
    public string Path => Project.Path;

    public string SessionsText =>
        Project.SessionCount == 1 ? "1 session" : $"{Project.SessionCount} sessions";

    public string Name
    {
        get
        {
            var name = System.IO.Path.GetFileName(Project.Path.TrimEnd('\\', '/'));

            return name.Length > 0 ? name : Project.Path;
        }
    }
}
