using System.Text.Json;
using TexLint.TestFunctionClasses;

namespace TexLint.Models.HandleInfos;

/// <summary>
/// Реализация сервиса для работы с конфигурацией LaTeX команд и окружений
/// </summary>
public class LatexConfigurationService : ILatexConfigurationService
{
    private readonly List<ParseInfo> _commands;
    private readonly List<ParseInfo> _environments;
    private readonly Dictionary<string, ParseInfo> _commandsCache;
    private readonly Dictionary<string, ParseInfo> _environmentsCache;

    public IReadOnlyList<ParseInfo> Commands => _commands.AsReadOnly();
    public IReadOnlyList<ParseInfo> Environments => _environments.AsReadOnly();

    public LatexConfigurationService()
    {
        try
        {
            // Загружаем конфигурации команд
            var commandsJson = File.ReadAllText(TestUtilities.FindConfigFile("commands.json"));
            var commandsModel = JsonSerializer.Deserialize<CommandsJsonModel>(commandsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _commands = commandsModel?.commands ?? new List<ParseInfo>();

            // Загружаем конфигурации окружений
            var environmentsJson = File.ReadAllText(TestUtilities.FindConfigFile("environments.json"));
            var environmentsModel = JsonSerializer.Deserialize<EnvironmentsJsonModel>(environmentsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _environments = environmentsModel?.environments ?? new List<ParseInfo>();

            // Создаем кэши для быстрого поиска по именам
            _commandsCache = _commands
                .Where(c => !string.IsNullOrEmpty(c.Name))
                .ToDictionary(c => c.Name, c => c);

            _environmentsCache = _environments
                .Where(e => !string.IsNullOrEmpty(e.Name))
                .ToDictionary(e => e.Name, e => e);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка при загрузке конфигурационных файлов LaTeX: {ex.Message}", ex);
        }
    }

    public ParseInfo? GetCommandConfiguration(string commandName)
    {
        if (string.IsNullOrEmpty(commandName))
            return null;

        return _commandsCache.TryGetValue(commandName, out var config) ? config : null;
    }

    public ParseInfo? GetEnvironmentConfiguration(string environmentName)
    {
        if (string.IsNullOrEmpty(environmentName))
            return null;

        return _environmentsCache.TryGetValue(environmentName, out var config) ? config : null;
    }
}

/// <summary>
/// Промежуточные модели для десериализации JSON файлов
/// </summary>
public class CommandsJsonModel
{
    public List<ParseInfo> commands { get; set; } = new List<ParseInfo>();
}

public class EnvironmentsJsonModel
{
    public List<ParseInfo> environments { get; set; } = new List<ParseInfo>();
} 