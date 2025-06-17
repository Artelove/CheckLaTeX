using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

/// <summary>
/// Базовый класс для всех тестовых функций
/// Поддерживает DI и изоляцию данных по запросам
/// </summary>
public abstract class TestFunction
{
    public List<TestError> Errors = new();
    
    protected readonly ILatexConfigurationService _configurationService;
    protected readonly string _requestId;
    protected readonly TestUtilities _testUtilities;

    protected TestFunction(ILatexConfigurationService configurationService, string requestId)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _requestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        _testUtilities = TestUtilities.Instance;
    }
    
    /// <summary>
    /// Вспомогательный метод для добавления ошибок с диагностикой
    /// </summary>
    protected void AddError(ErrorType type, string message, Command? command = null, 
                           string? suggestedFix = null)
    {
        if (command != null)
        {
            Errors.Add(TestError.CreateWithDiagnostics(
                type, message, 
                command.FileOwner ?? "unknown.tex",
                command.StringNumber,
                command.SourceStartColumn,
                command.ToString(),
                suggestedFix: suggestedFix,
                errorCommand: command
            ));
        }
        else
        {
            Errors.Add(new TestError 
            { 
                ErrorType = type, 
                ErrorInfo = message,
                SuggestedFix = suggestedFix
            });
        }
    }
    
    // ========== Методы-обертки для TestUtilities ==========
    // Автоматически подставляют _requestId для упрощения миграции кода
    
    /// <summary>
    /// Получает все найденные команды для текущего запроса
    /// </summary>
    protected List<Command> FoundsCommands => _testUtilities.GetFoundsCommands(_requestId);
    
    /// <summary>
    /// Получает все найденные команды с lstlisting для текущего запроса
    /// </summary>
    protected List<Command> FoundsCommandsWithLstlisting => _testUtilities.GetFoundsCommandsWithLstlisting(_requestId);
    
    /// <summary>
    /// Получает стартовую директорию для текущего запроса
    /// </summary>
    protected string StartDirectory => _testUtilities.GetStartDirectory(_requestId);
    
    /// <summary>
    /// Получает все команды по имени для текущего запроса
    /// </summary>
    protected List<Command> GetAllCommandsByName(string name) => 
        _testUtilities.GetAllCommandsByName(_requestId, name);
    
    /// <summary>
    /// Получает все команды по имени из указанного списка
    /// </summary>
    protected List<Command> GetAllCommandsByNameFromList(string name, IEnumerable<Command> list) => 
        _testUtilities.GetAllCommandsByNameFromList(name, list);
    
    /// <summary>
    /// Получает все окружения для текущего запроса
    /// </summary>
    protected List<EnvironmentCommand> GetAllEnvironment() => 
        _testUtilities.GetAllEnvironment(_requestId);
    
    /// <summary>
    /// Получает все команды как параметры из lstlisting для текущего запроса
    /// </summary>
    protected List<Command> GetAllCommandsLikeParametersFromLstlisting() => 
        _testUtilities.GetAllCommandsLikeParametersFromLstlisting(_requestId);
    
    /// <summary>
    /// Получает все команды как параметры из окружения
    /// </summary>
    protected List<Command> GetAllCommandsLikeParametersFromEnvironment(EnvironmentCommand environmentCommand) => 
        _testUtilities.GetAllCommandsLikeParametersFromEnvironment(environmentCommand);
    
    /// <summary>
    /// Получает команду по индексу в коллекции
    /// </summary>
    protected Command GetCommandByIndexInCollection(int index, IEnumerable<Command> collection) => 
        _testUtilities.GetCommandByIndexInCollection(index, collection);
    
    /// <summary>
    /// Получает область содержимого от найденного символа для текущего запроса
    /// </summary>
    protected string GetContentAreaFromFindSymbol(TextCommand textCommand, int centerAreaSymbolNumber, int areaLength = 50) => 
        _testUtilities.GetContentAreaFromFindSymbol(_requestId, textCommand, centerAreaSymbolNumber, areaLength);
    
    // ========== Статические свойства для конфигурационных файлов ==========
    // Эти свойства общие для всех запросов и не требуют requestId
    
    /// <summary>
    /// Путь к файлу commands.json
    /// </summary>
    protected static string PathToCommandsJson => TestUtilities.PathToCommandsJson;
    
    /// <summary>
    /// Путь к файлу environments.json
    /// </summary>
    protected static string PathToEnvironmentJson => TestUtilities.PathToEnvironmentJson;
    
    /// <summary>
    /// Путь к файлу lint-rules.json
    /// </summary>
    protected static string PathToLintRulesJson => TestUtilities.PathToLintRulesJson;
}