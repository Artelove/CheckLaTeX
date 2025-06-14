namespace TexLint.Models.HandleInfos;

/// <summary>
/// Сервис для работы с конфигурацией LaTeX команд и окружений
/// </summary>
public interface ILatexConfigurationService
{
    /// <summary>
    /// Список конфигураций команд LaTeX
    /// </summary>
    IReadOnlyList<ParseInfo> Commands { get; }
    
    /// <summary>
    /// Список конфигураций окружений LaTeX
    /// </summary>
    IReadOnlyList<ParseInfo> Environments { get; }
    
    /// <summary>
    /// Получить конфигурацию для указанной команды
    /// </summary>
    /// <param name="commandName">Имя команды</param>
    /// <returns>Конфигурация команды или null если не найдена</returns>
    ParseInfo? GetCommandConfiguration(string commandName);
    
    /// <summary>
    /// Получить конфигурацию для указанного окружения
    /// </summary>
    /// <param name="environmentName">Имя окружения</param>
    /// <returns>Конфигурация окружения или null если не найдена</returns>
    ParseInfo? GetEnvironmentConfiguration(string environmentName);
} 