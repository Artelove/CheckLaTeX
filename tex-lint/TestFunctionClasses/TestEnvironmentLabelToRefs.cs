using System.Text.Json;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

/// <summary>
/// Тестовая функция для проверки соответствия меток и ссылок.
/// Проверяет:
/// 1. Дублирующиеся метки
/// 2. Ссылки на несуществующие метки  
/// 3. Неиспользуемые метки
/// 4. Окружения, которые должны иметь метки
/// 5. Метки в ненумерованных окружениях (например, equation*)
/// 
/// Поддерживаемые команды ссылок:
/// - Основные: \ref, \eqref, \pageref
/// - Из пакета hyperref: \autoref, \nameref, \hyperref
/// - Из пакета cleveref: \cref, \Cref, \cpageref, \Cpageref, \crefrange, \Crefrange, \cpagerefrange, \Cpagerefrange, \labelcref
/// - Из пакета varioref: \vref, \vpageref
/// </summary>

public class TestEnvironmentLabelToRefs : TestFunction
{
    private readonly LabelsReferencesRule _labelsReferencesConfig;

    public TestEnvironmentLabelToRefs(ILatexConfigurationService configurationService, string requestId)
        : base(configurationService, requestId)
    {
        // Загружаем конфигурацию из lint-rules.json
        var lintRulesJson = File.ReadAllText(TestUtilities.FindConfigFile("lint-rules.json"));
        var lintRules = JsonSerializer.Deserialize<LintRules>(lintRulesJson);
        _labelsReferencesConfig = lintRules?.LabelsReferences ?? new LabelsReferencesRule();
        
        RunCheck();
    }
    
    private void RunCheck()
    {
        // Собираем все команды ссылок - основные и специализированные
        var referenceCommands = new List<Command>();
        
        // Основные команды ссылок
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "ref"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "eqref"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "pageref"));
        
        // Команды из пакета hyperref
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "autoref"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "nameref"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "hyperref"));
        
        // Команды из пакета cleveref
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "cref"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "Cref"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "cpageref"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "Cpageref"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "crefrange"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "Crefrange"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "cpagerefrange"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "Cpagerefrange"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "labelcref"));
        
        // Команды из пакета varioref
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "vref"));
        referenceCommands.AddRange(_testUtilities.GetAllCommandsByName(_requestId, "vpageref"));
        
        var labels = _testUtilities.GetAllCommandsByName(_requestId, "label");
        
        var refsArguments = new Dictionary<string, Command>();
        var labelsArguments = new Dictionary<string, Command>();

        // Собираем все ссылки из всех команд
        foreach (var refCommand in referenceCommands)
        {
            foreach (var arg in refCommand.Arguments)
            {
                if (!string.IsNullOrEmpty(arg.Value))
                {
                    refsArguments.TryAdd(arg.Value, refCommand);
                }
            }
        }

        // Собираем все метки и проверяем дубликаты
        if (_labelsReferencesConfig.CheckDuplicateLabels)
        {
            foreach (var label in labels)
            {
                foreach (var arg in label.Arguments)
                {
                    if (!string.IsNullOrEmpty(arg.Value))
                    {
                        if (!labelsArguments.TryAdd(arg.Value, label))
                        {
                            // Найден дубликат метки
                            Errors.Add(TestError.CreateWithDiagnostics(
                                ErrorType.Warning,
                                $"Название метки \\label{{{arg.Value}}} уже использовалось ранее",
                                label.FileOwner ?? "unknown.tex",
                                label.StringNumber,
                                label.SourceStartColumn,
                                label.ToString(),
                                suggestedFix: $"Используйте уникальное имя для метки вместо '{arg.Value}'",
                                errorCommand: label
                            ));
                        }
                    }
                }
            }
        }

        // Проверяем, что каждая ссылка ведет к существующей метке
        if (_labelsReferencesConfig.CheckMissingReferences)
        {
            foreach (var refPair in refsArguments)
            {
                var referenceKey = refPair.Key;
                var refCommand = refPair.Value;
                var match = labelsArguments.ContainsKey(referenceKey);

                if (!match)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Error,
                        $"Указанная ссылка \\{refCommand.Name}{{{referenceKey}}} не ведет ни к одной метке",
                        refCommand.FileOwner ?? "unknown.tex",
                        refCommand.StringNumber,
                        refCommand.SourceStartColumn,
                        refCommand.ToString(),
                        suggestedFix: $"Добавьте метку \\label{{{referenceKey}}} или исправьте ссылку",
                        errorCommand: refCommand
                    ));
                }
            }
        }
        
        // Проверяем, что каждая метка используется
        if (_labelsReferencesConfig.CheckUnusedLabels)
        {
            foreach (var labelPair in labelsArguments)
            {
                var labelKey = labelPair.Key;
                var labelCommand = labelPair.Value;
                var match = refsArguments.ContainsKey(labelKey);

                if (!match)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        $"Метка \\label{{{labelKey}}} не используется в документе",
                        labelCommand.FileOwner ?? "unknown.tex",
                        labelCommand.StringNumber,
                        labelCommand.SourceStartColumn,
                        labelCommand.ToString(),
                        suggestedFix: $"Удалите неиспользуемую метку или добавьте \\ref{{{labelKey}}} в текст",
                        errorCommand: labelCommand
                    ));
                }
            }
        }
        
        // Дополнительная проверка: окружения, которые должны содержать метки
        CheckEnvironmentsShouldHaveLabels();
        
        // Проверка меток в ненумерованных (звездочных) окружениях
        CheckLabelsInUnnumberedEnvironments();
    }
    
    /// <summary>
    /// Проверяет, что окружения, которые должны содержать метки, действительно их содержат
    /// </summary>
    private void CheckEnvironmentsShouldHaveLabels()
    {
        var environments = _testUtilities.GetAllEnvironment(_requestId);
        var existingLabels = _testUtilities.GetAllCommandsByName(_requestId, "label");
        
        foreach (var environment in environments)
        {
            if (IsEnvironmentShouldHaveLabel(environment.EnvironmentName))
            {
                // Проверяем, есть ли метка внутри этого окружения
                bool hasLabel = existingLabels.Any(label => 
                    environment.GetDeepInnerCommands().Any(inner => inner.GlobalIndex == label.GlobalIndex));
                
                if (!hasLabel)
                {
                    // Создаем ошибку для окружения без метки
                    var beginCommand = environment.GetDeepInnerCommands().FirstOrDefault();
                    if (beginCommand != null)
                    {
                        Errors.Add(TestError.CreateWithDiagnostics(
                            ErrorType.Warning,
                            $"Окружение '{environment.EnvironmentName}' должно содержать метку \\label{{}}",
                            beginCommand.FileOwner ?? "unknown.tex",
                            beginCommand.StringNumber,
                            beginCommand.SourceStartColumn,
                            $"\\begin{{{environment.EnvironmentName}}}",
                            suggestedFix: $"Добавьте \\label{{название_метки}} внутрь окружения {environment.EnvironmentName}"
                        ));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Проверяет, должно ли указанное окружение содержать метку
    /// </summary>
    /// <param name="environmentName">Имя окружения</param>
    /// <returns>True, если окружение должно содержать метку</returns>
    private bool IsEnvironmentShouldHaveLabel(string environmentName)
    {
        // Окружения со звездочкой не нумеруются и не должны содержать метки  
        if (environmentName.EndsWith("*"))
        {
            return false;
        }
        
        return _labelsReferencesConfig.RequiredLabelEnvironments.Contains(environmentName);
    }

    /// <summary>
    /// Находит окружение, содержащее указанную команду
    /// </summary>
    /// <param name="command">Команда для поиска</param>
    /// <returns>Имя окружения или null, если не найдено</returns>
    private string? FindEnvironmentForCommand(Command command)
    {
        var environments = _testUtilities.GetAllEnvironment(_requestId);
        
        foreach (var environment in environments)
        {
            foreach (var innerCommand in environment.GetDeepInnerCommands())
            {
                if (innerCommand.GlobalIndex == command.GlobalIndex)
                {
                    return environment.EnvironmentName;
                }
            }
        }

        return null;
    }
    
    /// <summary>
    /// Проверяет метки в ненумерованных (звездочных) окружениях
    /// </summary>
    private void CheckLabelsInUnnumberedEnvironments()
    {
        var environments = _testUtilities.GetAllEnvironment(_requestId);
        var existingLabels = _testUtilities.GetAllCommandsByName(_requestId, "label");
        
        foreach (var environment in environments)
        {
            // Проверяем только звездочные окружения
            if (environment.EnvironmentName.EndsWith("*"))
            {
                // Ищем метки внутри этого окружения
                var labelsInEnvironment = existingLabels.Where(label => 
                    environment.GetDeepInnerCommands().Any(inner => inner.GlobalIndex == label.GlobalIndex));
                
                foreach (var label in labelsInEnvironment)
                {
                    // Получаем название метки из аргументов
                    var labelName = label.Arguments.FirstOrDefault()?.Value ?? "неизвестно";
                    
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        $"Метка \\label{{{labelName}}} находится в ненумерованном окружении '{environment.EnvironmentName}'. Такие метки бесполезны, так как на них нельзя ссылаться",
                        label.FileOwner ?? "unknown.tex",
                        label.StringNumber,
                        label.SourceStartColumn,
                        label.ToString(),
                        suggestedFix: $"Удалите метку или используйте нумерованное окружение '{environment.EnvironmentName.TrimEnd('*')}'",
                        errorCommand: label
                    ));
                }
            }
        }
    }
}