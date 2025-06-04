using System.Collections.Generic; 
using System.Text; 

namespace TexLint.Models;

public class Command
{
    public string Name { get; set; } = string.Empty; 
    public string FileOwner { get; set; } = string.Empty;
    public int StringNumber { get; set; }
    public int StartSymbolNumber { get; set; }
    public int EndSymbolNumber { get; set; }
    public int FileIndex { get; set; } 
    public int GlobalIndex { get; set; }

    public List<Parameter> Parameters { get; set; } = new List<Parameter>(); 
    public List<Parameter> Arguments { get; set; } = new List<Parameter>(); 

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append('\\'); // Backslash character
        sb.Append(Name);
        sb.Append(ParamsToString(Parameters, '[', ']', "=", ','));
        sb.Append(ParamsToString(Arguments, '{', '}', "=", ',')); 
        return sb.ToString();
    }

    private string ParamsToString(List<Parameter> list, char open, char close, string valueSeparator, char itemSeparator)
    {
        if (list == null || list.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append(open);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) continue;
            sb.Append(list[i].Text);
            if (list[i].Value is not null)
            {
                sb.Append(valueSeparator);
                sb.Append(list[i].Value);
            }
            if (i < list.Count - 1)
                sb.Append(itemSeparator);
        }
        sb.Append(close);
        return sb.ToString();
    }
}
