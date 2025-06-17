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
    private readonly LintRules _lintRules;

    private ParseInfo GetForAnyCommand(string commandName)
    {
        return new ParseInfo()
        {
            Name = commandName,
            IsCommandExist = true,
            Param = new TypeCount() { Type = TypeCount.PHRASE_TYPE, Count = "?" },
            Arg = new TypeCount() { Type = TypeCount.PHRASE_TYPE, Count = "?" },
            Order = new List<string> { "any" }
        };
    }

    /// <summary>
    /// Создает автоматическую конфигурацию для неизвестных команд на основе общих паттернов LaTeX
    /// </summary>
    /// <param name="commandName">Имя команды</param>
    /// <returns>Автоматически созданная конфигурация</returns>
    private ParseInfo CreateFallbackCommandConfiguration(string commandName)
    {
        // Команды ссылок - обычно имеют один обязательный аргумент
        var referenceCommands = new HashSet<string>
        {
            "ref", "eqref", "pageref", "autoref", "nameref", "hyperref",
            "cref", "Cref", "cpageref", "Cpageref", "crefrange", "Crefrange", 
            "cpagerefrange", "Cpagerefrange", "labelcref", "vref", "vpageref"
        };

        // Команды меток - обычно имеют один обязательный аргумент
        var labelCommands = new HashSet<string>
        {
            "label"
        };

        // Команды цитирования - обычно имеют один обязательный аргумент, иногда опциональный параметр
        var citationCommands = new HashSet<string>
        {
            "cite", "citep", "citet", "citealp", "citealt", "citeauthor", "citeyear", "citenum"
        };

        // Команды включения файлов - обычно имеют один обязательный аргумент
        var includeCommands = new HashSet<string>
        {
            "input", "include", "includeonly", "includegraphics"
        };

        // Команды форматирования текста - обычно имеют один обязательный аргумент
        var textFormattingCommands = new HashSet<string>
        {
            "textbf", "textit", "texttt", "textsc", "textsf", "textsl", "textup", "textrm",
            "emph", "underline", "textcolor", "colorbox", "fbox"
        };

        // Математические команды - могут иметь различные паттерны
        var mathCommands = new HashSet<string>
        {
            "frac", "sqrt", "sum", "int", "lim", "sin", "cos", "tan", "log", "ln", "exp"
        };

        if (referenceCommands.Contains(commandName) || labelCommands.Contains(commandName))
        {
            // Команды ссылок и меток: один обязательный аргумент в фигурных скобках
            return new ParseInfo
            {
                Name = commandName,
                IsCommandExist = true,
                Param = new TypeCount { Type = TypeCount.VALUE_TYPE, Count = "0" },
                Arg = new TypeCount { Type = TypeCount.PHRASE_TYPE, Count = "1" },
                Order = new List<string> { "a" }
            };
        }

        if (citationCommands.Contains(commandName))
        {
            // Команды цитирования: опциональный параметр + обязательный аргумент
            return new ParseInfo
            {
                Name = commandName,
                IsCommandExist = true,
                Param = new TypeCount { Type = TypeCount.VALUE_TYPE, Count = "?" },
                Arg = new TypeCount { Type = TypeCount.PHRASE_TYPE, Count = "1" },
                Order = new List<string> { "p", "a" }
            };
        }

        if (includeCommands.Contains(commandName) || textFormattingCommands.Contains(commandName))
        {
            // Команды включения и форматирования: один обязательный аргумент
            return new ParseInfo
            {
                Name = commandName,
                IsCommandExist = true,
                Param = new TypeCount { Type = TypeCount.VALUE_TYPE, Count = "0" },
                Arg = new TypeCount { Type = TypeCount.PHRASE_TYPE, Count = "1" },
                Order = new List<string> { "a" }
            };
        }

        if (mathCommands.Contains(commandName))
        {
            // Математические команды: могут иметь несколько аргументов
            if (commandName == "frac")
            {
                // \frac{числитель}{знаменатель}
                return new ParseInfo
                {
                    Name = commandName,
                    IsCommandExist = true,
                    Param = new TypeCount { Type = TypeCount.VALUE_TYPE, Count = "0" },
                    Arg = new TypeCount { Type = TypeCount.PHRASE_TYPE, Count = "2" },
                    Order = new List<string> { "a", "a" }
                };
            }
            else if (commandName == "sqrt")
            {
                // \sqrt[степень]{выражение} - опциональный параметр + аргумент
                return new ParseInfo
                {
                    Name = commandName,
                    IsCommandExist = true,
                    Param = new TypeCount { Type = TypeCount.VALUE_TYPE, Count = "?" },
                    Arg = new TypeCount { Type = TypeCount.PHRASE_TYPE, Count = "1" },
                    Order = new List<string> { "p", "a" }
                };
            }
            else
            {
                // Остальные математические команды: один аргумент
                return new ParseInfo
                {
                    Name = commandName,
                    IsCommandExist = true,
                    Param = new TypeCount { Type = TypeCount.VALUE_TYPE, Count = "0" },
                    Arg = new TypeCount { Type = TypeCount.PHRASE_TYPE, Count = "1" },
                    Order = new List<string> { "a" }
                };
            }
        }

        // Универсальный fallback: попробуем определить аргументы автоматически
        // Большинство LaTeX команд имеют один обязательный аргумент
        return new ParseInfo
        {
            Name = commandName,
            IsCommandExist = true,
            Param = new TypeCount { Type = TypeCount.VALUE_TYPE, Count = "?" },
            Arg = new TypeCount { Type = TypeCount.PHRASE_TYPE, Count = "?" },
            Order = new List<string> { "a" }
        };
    }

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

            // Загружаем правила линтинга
            var lintRulesJson = File.ReadAllText(TestUtilities.FindConfigFile("lint-rules.json"));
            _lintRules = JsonSerializer.Deserialize<LintRules>(lintRulesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new LintRules();

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

        // Сначала проверяем кэш конфигурации
        if (_commandsCache.TryGetValue(commandName, out var config))
            return config;

        // FALLBACK: Создаем автоматическую конфигурацию для неизвестных команд
        var fallbackConfig = CreateFallbackCommandConfiguration(commandName);
        Console.WriteLine($"[FALLBACK] Создана автоматическая конфигурация для команды \\{commandName}: {string.Join(",", fallbackConfig.Order ?? new List<string>())}");
        return fallbackConfig;
    }

    public ParseInfo? GetEnvironmentConfiguration(string environmentName)
    {
        if (string.IsNullOrEmpty(environmentName))
            return null;

        return _environmentsCache.TryGetValue(environmentName, out var config) ? config : null;
    }

    public LineBreakRule GetLineBreakConfig()
    {
        return _lintRules.LineBreak;
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