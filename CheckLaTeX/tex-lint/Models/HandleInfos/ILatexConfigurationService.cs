namespace TexLint.Models.HandleInfos;

/// <summary>
/// Интерфейс для работы с конфигурацией LaTeX команд и окружений
/// </summary>
public interface ILatexConfigurationService
{
    /// <summary>
    /// Все команды из конфигурации
    /// </summary>
    IReadOnlyList<ParseInfo> Commands { get; }
    
    /// <summary>
    /// Все окружения из конфигурации
    /// </summary>
    IReadOnlyList<ParseInfo> Environments { get; }
    
    /// <summary>
    /// Получает конфигурацию для указанной команды
    /// </summary>
    /// <param name="commandName">Имя команды (без \\)</param>
    /// <returns>Конфигурация команды или null если не найдена</returns>
    ParseInfo? GetCommandConfiguration(string commandName);
    
    /// <summary>
    /// Получает конфигурацию для указанного окружения
    /// </summary>
    /// <param name="environmentName">Имя окружения</param>
    /// <returns>Конфигурация окружения или null если не найдена</returns>
    ParseInfo? GetEnvironmentConfiguration(string environmentName);

    /// <summary>
    /// Получает правила для проверки переносов строк
    /// </summary>
    /// <returns>Правила переносов строк</returns>
    LineBreakRule GetLineBreakConfig();
} 