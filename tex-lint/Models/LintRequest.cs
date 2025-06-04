namespace TexLint.Models
{
    public class LintRequest
    {
        public string DocumentContent { get; set; }
        public string? FilePath { get; set; }
    }
}
