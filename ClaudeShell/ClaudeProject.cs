namespace ClaudeShell;

public record ClaudeProject(string Path, DateTime LastUsed, int SessionCount = 0);
