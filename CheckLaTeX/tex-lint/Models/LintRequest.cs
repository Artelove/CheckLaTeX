namespace TexLint.Models;

public record LintRequest(string StartFile, Dictionary<string, string> Files);
