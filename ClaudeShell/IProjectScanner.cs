namespace ClaudeShell;

public interface IProjectScanner
{
    IReadOnlyList<ClaudeProject> Scan();
}
