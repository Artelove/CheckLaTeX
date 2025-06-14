namespace TexLint.Models;

/// <summary>
/// Модель запроса для проверки LaTeX документа
/// </summary>
public class LatexCheckRequest
{
    /// <summary>
    /// Содержимое LaTeX документа для проверки
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Дополнительные параметры проверки (опционально)
    /// </summary>
    public Dictionary<string, string>? Options { get; set; }
} 