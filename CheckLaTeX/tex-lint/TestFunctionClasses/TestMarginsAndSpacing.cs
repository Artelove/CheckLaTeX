using System.Text.Json;
using System.Text.RegularExpressions;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

/// <summary>
/// Тестовая функция для проверки маргинов, межстрочных интервалов и шрифтов в LaTeX документах.
/// Проверяет:
/// 1. Единое определение маргинов в преамбуле (для А4)
/// 2. Единое определение межстрочного интервала 1.5
/// 3. Правильность используемых шрифтов согласно требованиям
/// 4. Отсутствие изменений этих параметров по ходу документа
/// 5. Использование рекомендуемых пакетов (geometry, setspace)
/// 6. Отсутствие устаревших команд
/// </summary>
public class TestMarginsAndSpacing : TestFunction
{
    private readonly MarginsAndSpacingRule _config;
    
    // LaTeX команды для прямого изменения маргинов
    private readonly HashSet<string> _marginCommands = new()
    {
        "oddsidemargin", "evensidemargin", "topmargin", "bottommargin",
        "textwidth", "textheight", "headheight", "headsep", 
        "footskip", "marginparwidth", "marginparsep"
    };
    
    // LaTeX команды для межстрочных интервалов
    private readonly HashSet<string> _spacingCommands = new()
    {
        "baselineskip", "baselinestretch", "linespread", "parskip",
        "singlespacing", "onehalfspacing", "doublespacing", "setstretch"
    };
    
    // LaTeX команды для прямого изменения шрифтов
    private readonly HashSet<string> _fontCommands = new()
    {
        "fontfamily", "fontsize", "fontshape", "fontseries", "fontencoding",
        "selectfont", "usefont", "DeclareFixedFont", "newfont"
    };

    // Состояние проверки
    private bool _hasGeometryPackage = false;
    private bool _hasSetspacePackage = false;
    private bool _hasMarginDefinition = false;
    private bool _hasSpacingDefinition = false;
    private bool _hasFontDefinition = false;
    private int _marginDefinitionCount = 0;
    private int _spacingDefinitionCount = 0;
    private int _fontDefinitionCount = 0;
    private string? _detectedFontPackage = null;
    private string? _detectedFontSize = null;

    public TestMarginsAndSpacing(ILatexConfigurationService configurationService, string requestId)
        : base(configurationService, requestId)
    {
        // Загружаем конфигурацию
        var lintRulesJson = File.ReadAllText(PathToLintRulesJson);
        var lintRules = JsonSerializer.Deserialize<LintRules>(lintRulesJson);
        _config = lintRules?.MarginsAndSpacing ?? new MarginsAndSpacingRule();
        
        RunCheck();
    }

    private void RunCheck()
    {
        Console.WriteLine($"[DEBUG] TestMarginsAndSpacing: Начало проверки для запроса {_requestId}");
        
        // ВАЖНО: Сначала проверяем подключенные пакеты, чтобы установить флаги
        CheckPackages();
        
        Console.WriteLine($"[DEBUG] После CheckPackages: _hasGeometryPackage={_hasGeometryPackage}, _hasSetspacePackage={_hasSetspacePackage}");
        
        if (_config.CheckMargins)
        {
            CheckMarginSettings();
        }
        
        if (_config.CheckLineSpacing)
        {
            CheckLineSpacingSettings();
        }
        
        if (_config.CheckFonts)
        {
            CheckFontSettings();
        }
        
        // Проверяем общие требования
        CheckSingleDefinitionRequirement();
        CheckForForbiddenCommands();
        
        Console.WriteLine($"[DEBUG] TestMarginsAndSpacing: Проверка завершена. Найдено ошибок: {Errors.Count}");
    }

    /// <summary>
    /// Проверяет подключенные пакеты
    /// </summary>
    private void CheckPackages()
    {
        var usepackageCommands = GetAllCommandsByName("usepackage");
        Console.WriteLine($"[DEBUG] CheckPackages: Найдено usepackage команд: {usepackageCommands.Count}");
        
        // Сначала определяем какие пакеты подключены
        foreach (var command in usepackageCommands)
        {
            var packageName = GetPackageName(command);
            Console.WriteLine($"[DEBUG] CheckPackages: Найден пакет '{packageName}' в строке {command.StringNumber}");
            
            switch (packageName)
            {
                case "geometry":
                    _hasGeometryPackage = true;
                    Console.WriteLine($"[DEBUG] CheckPackages: Установлен флаг _hasGeometryPackage = true");
                    break;
                case "setspace":
                    _hasSetspacePackage = true;
                    Console.WriteLine($"[DEBUG] CheckPackages: Установлен флаг _hasSetspacePackage = true");
                    break;
                case var fontPkg when _config.AllowedFontPackages.Contains(fontPkg) || _config.ForbiddenFontPackages.Contains(fontPkg):
                    _detectedFontPackage = fontPkg;
                    _hasFontDefinition = true;
                    _fontDefinitionCount++;
                    Console.WriteLine($"[DEBUG] CheckPackages: Обнаружен шрифтовой пакет '{fontPkg}'");
                    break;
            }
        }
        
        // Теперь проверяем опции и ошибки
        foreach (var command in usepackageCommands)
        {
            var packageName = GetPackageName(command);
            
            switch (packageName)
            {
                case "geometry":
                    CheckGeometryPackageOptions(command);
                    break;
                case var forbidden when _config.ForbiddenMarginCommands.Contains(forbidden):
                    AddError(ErrorType.Warning,
                        $"Использование устаревшего пакета '{forbidden}' для настройки маргинов. Рекомендуется использовать пакет geometry",
                        command,
                        $"Замените \\usepackage{{{forbidden}}} на \\usepackage[margin=2.5cm,a4paper]{{geometry}}");
                    break;
                case var forbiddenFont when _config.ForbiddenFontPackages.Contains(forbiddenFont):
                    AddError(ErrorType.Error,
                        $"Использование неподходящего шрифтового пакета '{forbiddenFont}' для академических документов",
                        command,
                        $"Замените на рекомендуемый пакет: \\usepackage{{{_config.PreferredFontPackage}}}");
                    break;
            }
        }
        
        // Проверяем documentclass для определения размера шрифта
        CheckDocumentClass();
    }

    /// <summary>
    /// Проверяет настройки шрифтов
    /// </summary>
    private void CheckFontSettings()
    {
        Console.WriteLine($"[DEBUG] CheckFontSettings: Начало проверки шрифтов");
        
        // Проверяем основной шрифт
        CheckMainFontPackage();
        
        // Проверяем размер шрифта
        CheckFontSize();
        
        // Проверяем прямые команды изменения шрифтов
        CheckDirectFontCommands();
        
        // Проверяем согласованность шрифтов
        if (_config.CheckFontConsistency)
        {
            CheckFontConsistency();
        }
    }

    /// <summary>
    /// Проверяет основной шрифтовой пакет
    /// </summary>
    private void CheckMainFontPackage()
    {
        if (string.IsNullOrEmpty(_detectedFontPackage))
        {
            AddError(ErrorType.Info,
                "Не найдено явного определения шрифта. Рекомендуется использовать специальный шрифтовой пакет",
                null,
                $"Добавьте \\usepackage{{{_config.PreferredFontPackage}}} в преамбулу для Times New Roman");
        }
        else if (!_config.AllowedFontPackages.Contains(_detectedFontPackage))
        {
            AddError(ErrorType.Warning,
                $"Использование нестандартного шрифтового пакета '{_detectedFontPackage}' для академических документов",
                null,
                $"Рекомендуется использовать один из стандартных пакетов: {string.Join(", ", _config.AllowedFontPackages)}");
        }
        else if (_detectedFontPackage != _config.PreferredFontPackage)
        {
            AddError(ErrorType.Info,
                $"Используется шрифтовой пакет '{_detectedFontPackage}'. Рекомендуется '{_config.PreferredFontPackage}'",
                null,
                $"Для единообразия рекомендуется использовать \\usepackage{{{_config.PreferredFontPackage}}}");
        }
    }

    /// <summary>
    /// Проверяет размер шрифта
    /// </summary>
    private void CheckFontSize()
    {
        if (string.IsNullOrEmpty(_detectedFontSize))
        {
            AddError(ErrorType.Info,
                $"Не найдено явного определения размера шрифта. Рекомендуется использовать {_config.RequiredFontSize}",
                null,
                $"Добавьте опцию размера в documentclass: \\documentclass[{_config.RequiredFontSize}]{{article}}");
        }
        else if (!_config.AllowedFontSizes.Contains(_detectedFontSize))
        {
            AddError(ErrorType.Warning,
                $"Размер шрифта '{_detectedFontSize}' не соответствует стандартным требованиям",
                null,
                $"Используйте один из разрешенных размеров: {string.Join(", ", _config.AllowedFontSizes)}");
        }
        else if (_detectedFontSize != _config.RequiredFontSize)
        {
            AddError(ErrorType.Info,
                $"Размер шрифта '{_detectedFontSize}' отличается от рекомендуемого '{_config.RequiredFontSize}'",
                null,
                $"Для соответствия требованиям рекомендуется использовать {_config.RequiredFontSize}");
        }
    }

    /// <summary>
    /// Проверяет documentclass для определения размера шрифта
    /// </summary>
    private void CheckDocumentClass()
    {
        var documentclassCommands = GetAllCommandsByName("documentclass");
        
        foreach (var command in documentclassCommands)
        {
            // Ищем размер шрифта в опциях documentclass
            foreach (var parameter in command.Parameters)
            {
                var options = parameter.ToString();
                var fontSizeMatch = Regex.Match(options, @"(\d+pt)");
                
                if (fontSizeMatch.Success)
                {
                    _detectedFontSize = fontSizeMatch.Groups[1].Value;
                    Console.WriteLine($"[DEBUG] CheckDocumentClass: Обнаружен размер шрифта '{_detectedFontSize}' в строке {command.StringNumber}");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Проверяет прямые команды изменения шрифтов
    /// </summary>
    private void CheckDirectFontCommands()
    {
        if (!_config.WarnAboutDirectFontCommands) return;

        foreach (var fontCommand in _fontCommands)
        {
            var commands = GetAllCommandsByName(fontCommand);
            
            foreach (var command in commands)
            {
                _fontDefinitionCount++;
                
                AddError(ErrorType.Warning,
                    $"Прямое использование команды \\{fontCommand}. Рекомендуется использовать шрифтовые пакеты для единообразного управления шрифтами",
                    command,
                    $"Используйте \\usepackage{{{_config.PreferredFontPackage}}} вместо прямых команд шрифта");
            }
        }
    }

    /// <summary>
    /// Проверяет согласованность шрифтов (основной и математический)
    /// </summary>
    private void CheckFontConsistency()
    {
        if (_detectedFontPackage == null) return;

        // Рекомендации по согласованности математических шрифтов
        var mathPackageRecommendations = new Dictionary<string, string>
        {
            { "times", "newtxmath" },
            { "mathptmx", "mathptmx" }, // уже включает математический шрифт
            { "newtxtext", "newtxmath" }
        };

        if (mathPackageRecommendations.TryGetValue(_detectedFontPackage, out var recommendedMathPackage))
        {
            if (_detectedFontPackage != "mathptmx") // mathptmx уже включает математический шрифт
            {
                var mathPackageCommands = GetAllCommandsByName("usepackage");
                bool hasMathPackage = mathPackageCommands.Any(cmd => GetPackageName(cmd) == recommendedMathPackage);

                if (!hasMathPackage)
                {
                    AddError(ErrorType.Info,
                        $"Для согласованности с основным шрифтом '{_detectedFontPackage}' рекомендуется использовать математический пакет '{recommendedMathPackage}'",
                        null,
                        $"Добавьте \\usepackage{{{recommendedMathPackage}}} после \\usepackage{{{_detectedFontPackage}}}");
                }
            }
        }
    }

    /// <summary>
    /// Проверяет настройки маргинов
    /// </summary>
    private void CheckMarginSettings()
    {
        // Проверяем использование geometry package
        if (!_hasGeometryPackage && _config.PreferGeometryPackage)
        {
            AddError(ErrorType.Warning,
                "Рекомендуется использовать пакет geometry для настройки маргинов документа А4",
                null,
                "Добавьте \\usepackage[margin=2.5cm,a4paper]{geometry} в преамбулу");
        }

        // Проверяем прямые команды изменения маргинов
        CheckDirectMarginCommands();
    }

    /// <summary>
    /// Проверяет настройки межстрочных интервалов
    /// </summary>
    private void CheckLineSpacingSettings()
    {
        // Проверяем использование setspace package
        if (!_hasSetspacePackage && _config.PreferSetspacePackage)
        {
            AddError(ErrorType.Info,
                "Рекомендуется использовать пакет setspace для управления межстрочными интервалами",
                null,
                "Добавьте \\usepackage{setspace} в преамбулу и используйте \\onehalfspacing для интервала 1.5");
        }

        // Проверяем команды межстрочного интервала
        CheckSpacingCommands();
    }

    /// <summary>
    /// Проверяет опции пакета geometry
    /// </summary>
    private void CheckGeometryPackageOptions(Command geometryCommand)
    {
        _marginDefinitionCount++;
        _hasMarginDefinition = true;
        
        if(!geometryCommand.Parameters.Any(p => p.ToString().Contains("a4paper")))
        {
            AddError(ErrorType.Warning,
                "Рекомендуется явно указать формат a4paper в опциях geometry",
                geometryCommand,
                "Добавьте a4paper в опции: \\usepackage[margin=2.5cm,a4paper]{geometry}");
        }
        foreach (var parameter in geometryCommand.Parameters)
        {
            var options = parameter.ToString();
            
            // Проверяем значения маргинов
            var marginMatches = Regex.Matches(options, @"margin\s*=\s*([^,\]]+)");
            foreach (Match match in marginMatches)
            {
                var marginValue = match.Groups[1].Value.Trim();
                if (!IsValidMarginValue(marginValue))
                {
                    AddError(ErrorType.Warning,
                        $"Нестандартное значение маргина для А4: {marginValue}. Рекомендуется использовать стандартные значения",
                        geometryCommand,
                        $"Используйте одно из рекомендованных значений: {string.Join(", ", _config.AllowedMargins)}");
                }
            }
        }
    }

    /// <summary>
    /// Проверяет команды межстрочного интервала
    /// </summary>
    private void CheckSpacingCommands()
    {
        Console.WriteLine($"[DEBUG] CheckSpacingCommands: Состояние _hasSetspacePackage = {_hasSetspacePackage}");
        
        // Проверяем команды setspace package
        var onehalfspacingCommands = GetAllCommandsByName("onehalfspacing");
        var setstretchCommands = GetAllCommandsByName("setstretch");
        var linespreadCommands = GetAllCommandsByName("linespread");
        
        Console.WriteLine($"[DEBUG] CheckSpacingCommands: onehalfspacing={onehalfspacingCommands.Count}, setstretch={setstretchCommands.Count}, linespread={linespreadCommands.Count}");
        
        // onehalfspacing (это интервал 1.5)
        foreach (var command in onehalfspacingCommands)
        {
            _spacingDefinitionCount++;
            _hasSpacingDefinition = true;
            
            if (!_hasSetspacePackage)
            {
                AddError(ErrorType.Error,
                    "Команда \\onehalfspacing требует подключения пакета setspace",
                    command,
                    "Добавьте \\usepackage{setspace} в преамбулу документа");
            }
        }
        
        // setstretch
        foreach (var command in setstretchCommands)
        {
            _spacingDefinitionCount++;
            _hasSpacingDefinition = true;
            
            Console.WriteLine($"[DEBUG] CheckSpacingCommands: Обрабатываем setstretch в строке {command.StringNumber}, _hasSetspacePackage={_hasSetspacePackage}");
            
            if (!_hasSetspacePackage)
            {
                Console.WriteLine($"[DEBUG] CheckSpacingCommands: Добавляем ошибку для setstretch - пакет setspace не найден");
                AddError(ErrorType.Error,
                    "Команда \\setstretch требует подключения пакета setspace",
                    command,
                    "Добавьте \\usepackage{setspace} в преамбулу документа");
            }
            
            var value = GetCommandValue(command);
            if (!string.IsNullOrEmpty(value) && double.TryParse(value.Replace(",", "."), out double spacing))
            {
                if (Math.Abs(spacing - _config.RequiredLineSpacing) > 0.01)
                {
                    AddError(ErrorType.Warning,
                        $"Межстрочный интервал {spacing} не соответствует требуемому значению {_config.RequiredLineSpacing}",
                        command,
                        $"Измените на \\setstretch{{{_config.RequiredLineSpacing}}} или используйте \\onehalfspacing");
                }
            }
        }
        
        // linespread
        foreach (var command in linespreadCommands)
        {
            _spacingDefinitionCount++;
            _hasSpacingDefinition = true;
            
            var value = GetCommandValue(command);
            if (!string.IsNullOrEmpty(value) && double.TryParse(value.Replace(",", "."), out double spacing))
            {
                if (Math.Abs(spacing - _config.RequiredLineSpacing) > 0.01)
                {
                    AddError(ErrorType.Warning,
                        $"Межстрочный интервал {spacing} не соответствует требуемому значению {_config.RequiredLineSpacing}",
                        command,
                        $"Измените на \\linespread{{{_config.RequiredLineSpacing}}} или лучше используйте \\usepackage{{setspace}} и \\onehalfspacing");
                }
            }
            
            if (_hasSetspacePackage)
            {
                AddError(ErrorType.Info,
                    "При использовании пакета setspace рекомендуется использовать \\onehalfspacing вместо \\linespread",
                    command,
                    "Замените \\linespread{1.5} на \\onehalfspacing");
            }
        }
        
        // Проверяем другие команды интервалов
        var singlespacingCommands = GetAllCommandsByName("singlespacing");
        var doublespacingCommands = GetAllCommandsByName("doublespacing");
        
        Console.WriteLine($"[DEBUG] CheckSpacingCommands: singlespacing={singlespacingCommands.Count}, doublespacing={doublespacingCommands.Count}");
        
        foreach (var command in singlespacingCommands.Concat(doublespacingCommands))
        {
            _spacingDefinitionCount++;
            
            var commandName = command.Name;
            Console.WriteLine($"[DEBUG] CheckSpacingCommands: Обрабатываем команду '{commandName}' в строке {command.StringNumber}, _hasSetspacePackage={_hasSetspacePackage}");
            
            if (commandName == "singlespacing")
            {
                AddError(ErrorType.Warning,
                    "Используется одинарный межстрочный интервал вместо требуемого 1.5",
                    command,
                    "Замените \\singlespacing на \\onehalfspacing для получения интервала 1.5");
            }
            else if (commandName == "doublespacing")
            {
                AddError(ErrorType.Warning,
                    "Используется двойной межстрочный интервал вместо требуемого 1.5",
                    command,
                    "Замените \\doublespacing на \\onehalfspacing для получения интервала 1.5");
            }
            
            if (!_hasSetspacePackage)
            {
                Console.WriteLine($"[DEBUG] CheckSpacingCommands: Добавляем ошибку для команды '{commandName}' - пакет setspace не найден");
                AddError(ErrorType.Error,
                    $"Команда \\{commandName} требует подключения пакета setspace",
                    command,
                    "Добавьте \\usepackage{setspace} в преамбулу документа");
            }
        }
    }

    /// <summary>
    /// Проверяет прямые команды изменения маргинов
    /// </summary>
    private void CheckDirectMarginCommands()
    {
        var setlengthCommands = GetAllCommandsByName("setlength");
        var addtolengthCommands = GetAllCommandsByName("addtolength");
        
        var allLengthCommands = setlengthCommands.Concat(addtolengthCommands);
        
        foreach (var command in allLengthCommands)
        {
            var parameterName = GetFirstArgumentValue(command);
            var cleanParameterName = parameterName?.TrimStart('\\');
            
            if (_marginCommands.Contains(cleanParameterName))
            {
                _marginDefinitionCount++;
                
                if (_config.WarnAboutDirectParameterModification)
                {
                    AddError(ErrorType.Warning,
                        $"Прямое изменение параметра {parameterName}. Рекомендуется использовать пакет geometry для единообразного управления маргинами",
                        command,
                        "Используйте \\usepackage[margin=2.5cm,a4paper]{geometry} для настройки маргинов");
                }
            }
        }
        
        // Проверяем renewcommand для baselinestretch
        var renewcommandCommands = GetAllCommandsByName("renewcommand");
        foreach (var command in renewcommandCommands)
        {
            var parameterName = GetFirstArgumentValue(command);
            if (parameterName == "\\baselinestretch")
            {
                _spacingDefinitionCount++;
                
                if (_config.WarnAboutDirectParameterModification)
                {
                    AddError(ErrorType.Warning,
                        "Прямое изменение \\baselinestretch. Рекомендуется использовать пакет setspace",
                        command,
                        "Используйте \\usepackage{setspace} и \\onehalfspacing для интервала 1.5");
                }
            }
        }
    }

    /// <summary>
    /// Проверяет требование единого определения параметров
    /// </summary>
    private void CheckSingleDefinitionRequirement()
    {
        if (!_config.CheckSingleDefinition) return;

        // Проверяем множественные определения маргинов
        if (_marginDefinitionCount > 1)
        {
            AddError(ErrorType.Warning,
                $"Маргины определяются в {_marginDefinitionCount} местах. Рекомендуется задавать маргины только один раз в преамбуле",
                null,
                "Оставьте только одно определение маргинов в преамбуле документа");
        }

        // Проверяем множественные определения интервалов
        if (_spacingDefinitionCount > 1)
        {
            AddError(ErrorType.Warning,
                $"Межстрочный интервал определяется в {_spacingDefinitionCount} местах. Рекомендуется задавать интервал только один раз в преамбуле",
                null,
                "Оставьте только одно определение межстрочного интервала в преамбуле документа");
        }

        // Проверяем множественные определения шрифтов
        if (_fontDefinitionCount > 1)
        {
            AddError(ErrorType.Warning,
                $"Шрифт определяется в {_fontDefinitionCount} местах. Рекомендуется задавать шрифт только один раз в преамбуле",
                null,
                "Оставьте только одно определение шрифта в преамбуле документа");
        }

        // Проверяем отсутствие определений
        if (_config.CheckMargins && !_hasMarginDefinition && !_hasGeometryPackage)
        {
            AddError(ErrorType.Info,
                "Не найдено явного определения маргинов. Рекомендуется использовать geometry package",
                null,
                "Добавьте \\usepackage[margin=2.5cm,a4paper]{geometry} в преамбулу");
        }

        if (_config.CheckLineSpacing && !_hasSpacingDefinition)
        {
            AddError(ErrorType.Info,
                "Не найдено определения межстрочного интервала 1.5. Рекомендуется использовать setspace package",
                null,
                "Добавьте \\usepackage{setspace} и \\onehalfspacing в преамбулу");
        }

        if (_config.CheckFonts && !_hasFontDefinition)
        {
            AddError(ErrorType.Info,
                "Не найдено определения основного шрифта. Рекомендуется использовать специальный шрифтовой пакет",
                null,
                $"Добавьте \\usepackage{{{_config.PreferredFontPackage}}} в преамбулу");
        }
    }

    /// <summary>
    /// Проверяет запрещенные команды
    /// </summary>
    private void CheckForForbiddenCommands()
    {
        var usepackageCommands = GetAllCommandsByName("usepackage");
        
        foreach (var command in usepackageCommands)
        {
            var packageName = GetPackageName(command);
            
            if (_config.ForbiddenMarginCommands.Contains(packageName))
            {
                AddError(ErrorType.Warning,
                    $"Пакет '{packageName}' устарел и не рекомендуется к использованию для документов А4",
                    command,
                    "Используйте пакет geometry для современной настройки layout страницы");
            }

            if (_config.ForbiddenFontPackages.Contains(packageName))
            {
                AddError(ErrorType.Error,
                    $"Пакет '{packageName}' неподходящий для академических документов",
                    command,
                    $"Используйте рекомендуемый шрифтовой пакет: \\usepackage{{{_config.PreferredFontPackage}}}");
            }
        }
    }

    // ========== Вспомогательные методы ==========

    /// <summary>
    /// Извлекает имя пакета из команды usepackage
    /// </summary>
    private string GetPackageName(Command usepackageCommand)
    {
        var packageName = usepackageCommand.Arguments.FirstOrDefault()?.Text ?? "";
        Console.WriteLine($"[DEBUG] GetPackageName: usepackage в строке {usepackageCommand.StringNumber}, аргументов: {usepackageCommand.Arguments.Count}, имя пакета: '{packageName}'");
        
        // Отладочная информация об аргументах
        for (int i = 0; i < usepackageCommand.Arguments.Count; i++)
        {
            Console.WriteLine($"[DEBUG] GetPackageName: Аргумент {i} - Text: '{usepackageCommand.Arguments[i].Text}', Value: '{usepackageCommand.Arguments[i].Value}'");
        }
        
        return packageName;
    }

    /// <summary>
    /// Извлекает значение первого аргумента команды
    /// </summary>
    private string? GetFirstArgumentValue(Command command)
    {
        return command.Arguments.FirstOrDefault()?.Text;
    }

    /// <summary>
    /// Извлекает значение команды (первый аргумент)
    /// </summary>
    private string GetCommandValue(Command command)
    {
        return command.Arguments.FirstOrDefault()?.Text ?? "";
    }

    /// <summary>
    /// Проверяет, является ли значение маргина допустимым для А4
    /// </summary>
    private bool IsValidMarginValue(string marginValue)
    {
        return _config.AllowedMargins.Contains(marginValue.Trim());
    }
} 