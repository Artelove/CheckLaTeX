namespace TexLint.Models;

public class LintRules
{
    public QuotationMarkRule QuotationMarks { get; set; } = new();
    public HyphenRule Hyphen { get; set; } = new();
}

public class QuotationMarkRule
{
    public string PreferredOpen { get; set; } = "«";
    public string PreferredClose { get; set; } = "»";
    public string[] Forbidden { get; set; } = System.Array.Empty<string>();
}

public class HyphenRule
{
    public string WrongSymbol { get; set; } = "-";
    public string Replacement { get; set; } = "—";
}
