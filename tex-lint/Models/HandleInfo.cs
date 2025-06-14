using System.Text.Json;
using TexLint.Models.HandleInfos;
using TexLint.TestFunctionClasses;

namespace TexLint.Models;

/// <summary>
/// Класс для работы с информацией о командах и окружениях LaTeX
/// Теперь использует DI для получения конфигурации
/// </summary>
public class HandleInfo
{
    private readonly ILatexConfigurationService _configurationService;

    public enum ParamsOrder
    {
        Optional,
        Param
    }

    public HandleInfo(ILatexConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
    }

    /// <summary>
    /// Получить информацию о парсинге для команды
    /// </summary>
    /// <param name="command">Команда LaTeX</param>
    /// <returns>Информация о парсинге или объект с IsCommandExist = false</returns>
    public ParseInfo GetParseInfoByCommand(Command command)
    {
        var config = _configurationService.GetCommandConfiguration(command.Name);
        return config ?? new ParseInfo { IsCommandExist = false };
    }

    /// <summary>
    /// Получить информацию о парсинге для окружения
    /// </summary>
    /// <param name="command">Команда begin с именем окружения в аргументе</param>
    /// <returns>Информация о парсинге или объект с IsCommandExist = false</returns>
    public ParseInfo GetParseInfoByEnvironments(Command command)
    {
        if (command.Arguments.Count == 0)
            return new ParseInfo { IsCommandExist = false };

        var environmentName = command.Arguments[0].Value;
        var config = _configurationService.GetEnvironmentConfiguration(environmentName);
        return config ?? new ParseInfo { IsCommandExist = false };
    }
}