namespace ClaudeShell;

public interface IProjectLauncher
{
    void OpenInExplorer(string path);

    void OpenInTerminal(string path);

    void ContinueClaude(string path);
}
