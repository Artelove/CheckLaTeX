namespace TexLint.Models;

/// <summary>
/// Исключение, возникающее при ошибках парсинга LaTeX документов
/// </summary>
public class LatexParsingException : Exception
{
    public string? FileName { get; }
    public int? LineNumber { get; }
    public int? CharacterPosition { get; }
    public string? LatexContext { get; }
    
    public LatexParsingException(string message) : base(message)
    {
    }
    
    public LatexParsingException(string message, Exception innerException) : base(message, innerException)
    {
    }
    
    public LatexParsingException(
        string message, 
        string? fileName = null, 
        int? lineNumber = null, 
        int? characterPosition = null, 
        string? latexContext = null) : base(message)
    {
        FileName = fileName;
        LineNumber = lineNumber;
        CharacterPosition = characterPosition;
        LatexContext = latexContext;
    }
    
    public override string ToString()
    {
        var details = new List<string> { base.ToString() };
        
        if (!string.IsNullOrEmpty(FileName))
            details.Add($"Файл: {FileName}");
            
        if (LineNumber.HasValue)
            details.Add($"Строка: {LineNumber}");
            
        if (CharacterPosition.HasValue)
            details.Add($"Позиция: {CharacterPosition}");
            
        if (!string.IsNullOrEmpty(LatexContext))
            details.Add($"LaTeX контекст: {LatexContext}");
            
        return string.Join(Environment.NewLine, details);
    }
} 