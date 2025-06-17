namespace TexLint.Models;

public class LintRules
{
    public QuotationMarkRule QuotationMarks { get; set; } = new();
    public HyphenRule Hyphen { get; set; } = new();
    public BibliographyRule Bibliography { get; set; } = new();
    public LabelsReferencesRule LabelsReferences { get; set; } = new();
    public MarginsAndSpacingRule MarginsAndSpacing { get; set; } = new();
    public LineBreakRule LineBreak { get; set; } = new();
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

public class BibliographyRule
{
    public bool CheckUnusedSources { get; set; } = true;
    public bool CheckMissingCitations { get; set; } = true;
    public string BibFilePath { get; set; } = "Bib.bib";
}

public class LabelsReferencesRule
{
    public bool CheckDuplicateLabels { get; set; } = true;
    public bool CheckUnusedLabels { get; set; } = true;
    public bool CheckMissingReferences { get; set; } = true;
    public string[] RequiredLabelEnvironments { get; set; } = { "figure", "table", "equation", "lstlisting" };
}

public class MarginsAndSpacingRule
{
    public bool CheckMargins { get; set; } = true;
    public bool CheckLineSpacing { get; set; } = true;
    public bool CheckFonts { get; set; } = true;
    public bool PreferGeometryPackage { get; set; } = true;
    public bool PreferSetspacePackage { get; set; } = true;
    public bool CheckSingleDefinition { get; set; } = true;
    
    // Разрешенные значения маргинов для А4 (стандартные)
    public string[] AllowedMargins { get; set; } = { "2.5cm", "25mm", "1in" };
    
    // Требуемый межстрочный интервал
    public double RequiredLineSpacing { get; set; } = 1.5;
    
    // Запрещенные команды (устаревшие или нежелательные)
    public string[] ForbiddenMarginCommands { get; set; } = { "a4wide", "fullpage" };
    
    // Предупреждения о прямом изменении параметров
    public bool WarnAboutDirectParameterModification { get; set; } = true;
    
    // === Настройки шрифтов ===
    
    // Разрешенные пакеты шрифтов
    public string[] AllowedFontPackages { get; set; } = { "times", "mathptmx", "newtxtext", "newtxmath" };
    
    // Рекомендуемый основной шрифт
    public string PreferredFontPackage { get; set; } = "times";
    
    // Требуемый размер шрифта
    public string RequiredFontSize { get; set; } = "12pt";
    
    // Разрешенные размеры шрифта
    public string[] AllowedFontSizes { get; set; } = { "10pt", "11pt", "12pt" };
    
    // Запрещенные пакеты шрифтов
    public string[] ForbiddenFontPackages { get; set; } = { "comic", "arev" };
    
    // Проверка согласованности шрифтов (основной + математический)
    public bool CheckFontConsistency { get; set; } = true;
    
    // Предупреждение об использовании прямых команд изменения шрифта
    public bool WarnAboutDirectFontCommands { get; set; } = true;
}

public class LineBreakRule
{
    // Основные настройки правил переноса строк
    public bool CheckSingleNewlines { get; set; } = true;
    
    // Предпочтительная команда переноса строк
    public string PreferredLineBreak { get; set; } = "\\\\";
    
    // Сообщение для пользователей
    public string SingleNewlineMessage { get; set; } = "Одиночный \\n не создает перенос строки в LaTeX. Используйте \\\\\\\\ или \\\\newline для принудительного переноса";
}
