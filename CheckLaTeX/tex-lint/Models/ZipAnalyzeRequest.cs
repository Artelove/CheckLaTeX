using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TexLint.Models;

/// <summary>
/// Модель запроса для анализа ZIP архива с LaTeX документами
/// </summary>
public class ZipAnalyzeRequest
{
    /// <summary>
    /// ZIP архив с LaTeX документами
    /// </summary>
    [Required]
    public IFormFile ZipFile { get; set; } = null!;

    /// <summary>
    /// Главный файл для начала анализа (опционально). 
    /// Если не указан, система автоматически найдет файл с \documentclass
    /// </summary>
    public string? StartFile { get; set; }
} 