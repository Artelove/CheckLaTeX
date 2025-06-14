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
    /// Путь к файлу на устройстве пользователя (для корректного отображения диагностики в VS Code)
    /// </summary>
    public string? FilePath { get; set; }
    
    /// <summary>
    /// Дополнительные параметры проверки (опционально)
    /// </summary>
    public Dictionary<string, string>? Options { get; set; }
} 