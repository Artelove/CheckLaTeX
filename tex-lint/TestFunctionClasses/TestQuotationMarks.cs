using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

public class TestQuotationMarks : TestFunction
{
    private readonly char[] _invalidQuotes;

    public TestQuotationMarks(ILatexConfigurationService configurationService, string requestId) 
        : base(configurationService, requestId)
    {
        Console.WriteLine($"[DEBUG] TestQuotationMarks: Начало проверки для запроса {requestId}");
        
        var lintRulesJson = File.ReadAllText(PathToLintRulesJson);
        var lintRules = JsonSerializer.Deserialize<LintRules>(lintRulesJson);

        _invalidQuotes = lintRules?.QuotationMarks.Forbidden.Select(s => s[0]).ToArray() ?? Array.Empty<char>();
        
        Console.WriteLine($"[DEBUG] TestQuotationMarks: Запрещенные кавычки: {string.Join(", ", _invalidQuotes.Select(c => $"'{c}'"))}");
        Console.WriteLine($"[DEBUG] TestQuotationMarks: Всего команд найдено: {FoundsCommandsWithLstlisting.Count}");

        List<string> commandsNamesWherePhraseArgOrParam = new();
        foreach (var command in _configurationService.Commands)
        {
            if (command?.Arg.ParseType == ParameterParseType.Phrase ||
                command?.Param.ParseType == ParameterParseType.Phrase)
            {
                commandsNamesWherePhraseArgOrParam.Add(command.Name);
            }
        }

        var commandsWherePhraseArgOrParam = new List<Command>();
        foreach (var command in FoundsCommandsWithLstlisting)
        {
            if (commandsNamesWherePhraseArgOrParam.Contains(command.Name))
                commandsWherePhraseArgOrParam.Add(command);
        }

        foreach (var command in commandsWherePhraseArgOrParam)
        {
            // Обрабатываем аргументы команды
            foreach (var argument in command.Arguments)
            {
                var argText = argument.Text ?? string.Empty;
                var argValue = argument.Value ?? string.Empty;
                
                // Ищем ошибки в тексте аргумента
                var textErrors = FindQuotationMarkErrors(argText, command.FileOwner, command.StringNumber, command);
                foreach (var error in textErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в аргументе команды",
                        error.FileName ?? command.FileOwner,
                        error.LineNumber ?? command.StringNumber,
                        error.ColumnNumber ?? 1,
                        error.OriginalText ?? argText,
                        error.EndLineNumber,
                        error.EndColumnNumber,
                        suggestedFix: error.SuggestedFix,
                        errorCommand: command
                    ));
                }
                
                // Ищем ошибки в значении аргумента
                var valueErrors = FindQuotationMarkErrors(argValue, command.FileOwner, command.StringNumber, command);
                foreach (var error in valueErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в аргументе команды",
                        error.FileName ?? command.FileOwner,
                        error.LineNumber ?? command.StringNumber,
                        error.ColumnNumber ?? 1,
                        error.OriginalText ?? argValue,
                        error.EndLineNumber,
                        error.EndColumnNumber,
                        suggestedFix: error.SuggestedFix,
                        errorCommand: command
                    ));
                }
            }
            
            // Обрабатываем параметры команды
            foreach (var parameter in command.Parameters)
            {
                var paramText = parameter.Text ?? string.Empty;
                var paramValue = parameter.Value ?? string.Empty;
                
                // Ищем ошибки в тексте параметра
                var textErrors = FindQuotationMarkErrors(paramText, command.FileOwner, command.StringNumber, command);
                foreach (var error in textErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в параметре команды",
                        error.FileName ?? command.FileOwner,
                        error.LineNumber ?? command.StringNumber,
                        error.ColumnNumber ?? 1,
                        error.OriginalText ?? paramText,
                        error.EndLineNumber,
                        error.EndColumnNumber,
                        suggestedFix: error.SuggestedFix,
                        errorCommand: command
                    ));
                }
                
                // Ищем ошибки в значении параметра
                var valueErrors = FindQuotationMarkErrors(paramValue, command.FileOwner, command.StringNumber, command);
                foreach (var error in valueErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в параметре команды",
                        error.FileName ?? command.FileOwner,
                        error.LineNumber ?? command.StringNumber,
                        error.ColumnNumber ?? 1,
                        error.OriginalText ?? paramValue,
                        error.EndLineNumber,
                        error.EndColumnNumber,
                        suggestedFix: error.SuggestedFix,
                        errorCommand: command
                    ));
                }
            }
        }  
        
        var textCommands = GetAllCommandsByName("TEXT_NAME");
        Console.WriteLine($"[DEBUG] TestQuotationMarks: Найдено TEXT команд: {textCommands.Count}");
        
        foreach (var environmentCommand in GetAllEnvironment())
        {
            if (environmentCommand.EnvironmentName != "lstlisting")
                continue;
            
            foreach (var innerCommand in environmentCommand.InnerCommands)
            {
                if (innerCommand is TextCommand textCommand)
                {
                    textCommands.Remove(textCommand);
                }
            }
        }
        
        Console.WriteLine($"[DEBUG] TestQuotationMarks: TEXT команд после исключения lstlisting: {textCommands.Count}");

        foreach (var command in textCommands)
        {
            var textCommand = (TextCommand)command;
            var text = textCommand?.Text ?? string.Empty;
            
            Console.WriteLine($"[DEBUG] TestQuotationMarks: Анализирую текст в строке {textCommand.StringNumber}: '{text}'");
            
            // Проверяем, находится ли эта текстовая команда внутри математического окружения
            bool isInMathEnvironment = IsTextCommandInMathEnvironment(textCommand);
            Console.WriteLine($"[DEBUG] TestQuotationMarks: Текстовая команда в математическом окружении: {isInMathEnvironment}");
            
            // Проверяем, находится ли команда внутри математической команды \(...\) или \[...\]
            bool isInMathCommand = !isInMathEnvironment && IsTextCommandInMathCommand(textCommand);
            Console.WriteLine($"[DEBUG] TestQuotationMarks: Текстовая команда в математической команде: {isInMathCommand}");
            
            if (isInMathEnvironment || isInMathCommand)
            {
                Console.WriteLine($"[DEBUG] TestQuotationMarks: Пропускаем анализ кавычек - текст в математическом контексте");
                continue; // Пропускаем анализ кавычек для текста в математических окружениях и командах
            }
            
            var errors = FindQuotationMarkErrors(text, textCommand.FileOwner, textCommand.StringNumber, textCommand);
            
            Console.WriteLine($"[DEBUG] TestQuotationMarks: Найдено ошибок в этом тексте: {errors.Count}");
            
            foreach (var error in errors)
            {
                Console.WriteLine($"[DEBUG] TestQuotationMarks: Ошибка в позиции {error.LineNumber}:{error.ColumnNumber}, текст: '{error.OriginalText}'");
                
                Errors.Add(TestError.CreateWithDiagnostics(
                    ErrorType.Warning,
                    "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в тексте",
                    error.FileName ?? textCommand.FileOwner,
                    error.LineNumber ?? textCommand.StringNumber,
                    error.ColumnNumber ?? 1,
                    error.OriginalText ?? text,
                    error.EndLineNumber,
                    error.EndColumnNumber,
                    suggestedFix: error.SuggestedFix,
                    errorCommand: textCommand
                ));
            }
        }
        
        Console.WriteLine($"[DEBUG] TestQuotationMarks: Проверка завершена. Найдено ошибок: {Errors.Count}");
    }

    /// <summary>
    /// Проверяет, находится ли текстовая команда внутри математического окружения
    /// </summary>
    /// <param name="textCommand">Текстовая команда для проверки</param>
    /// <returns>True, если команда находится внутри математического окружения</returns>
    private bool IsTextCommandInMathEnvironment(TextCommand textCommand)
    {
        var environments = GetAllEnvironment();
        var mathEnvironments = new[] { "equation", "align", "gather", "multline", "flalign", "alignat", "eqnarray", "math", "displaymath" };
        
        foreach (var environment in environments)
        {
            // Проверяем, является ли окружение математическим (включая версии со звездочкой)
            bool isMathEnv = mathEnvironments.Any(mathEnv => 
                environment.EnvironmentName == mathEnv || 
                environment.EnvironmentName == mathEnv + "*");
            
            if (isMathEnv)
            {
                // Проверяем, содержится ли наша текстовая команда в этом окружении
                foreach (var innerCommand in environment.InnerCommands)
                {
                    if (innerCommand is TextCommand && 
                        innerCommand.GlobalIndex == textCommand.GlobalIndex)
                    {
                        Console.WriteLine($"[DEBUG] IsTextCommandInMathEnvironment: Текстовая команда найдена в математическом окружении '{environment.EnvironmentName}'");
                        return true;
                    }
                }
            }
        }
        
        Console.WriteLine($"[DEBUG] IsTextCommandInMathEnvironment: Текстовая команда НЕ в математическом окружении");
        return false;
    }

    /// <summary>
    /// Проверяет, находится ли текстовая команда внутри математической команды типа \(...\) или $...$
    /// </summary>
    /// <param name="textCommand">Текстовая команда для проверки</param>
    /// <returns>True, если команда находится внутри математической команды</returns>
    private bool IsTextCommandInMathCommand(TextCommand textCommand)
    {
        Console.WriteLine($"[DEBUG] IsTextCommandInMathCommand: Проверяю текстовую команду в строке {textCommand.StringNumber}, позиция {textCommand.SourceStartPosition}-{textCommand.SourceEndPosition}");
        
        // Сначала посмотрим на ВСЕ команды, чтобы понять структуру
        var allCommands = GetAllCommandsByName("");
        Console.WriteLine($"[DEBUG] IsTextCommandInMathCommand: Всего команд в системе: {allCommands.Count}");
        
        // Покажем все команды для отладки
        foreach (var cmd in allCommands.Take(30)) // Показываем первые 30 команд
        {
            var arguments = cmd.Arguments.Select(a => a.Text).ToList();
            var parameters = cmd.Parameters.Select(p => p.Text).ToList();
            Console.WriteLine($"[DEBUG] IsTextCommandInMathCommand: Команда: '{cmd.Name}', тип: {cmd.GetType().Name}, аргументы: {string.Join(", ", arguments)}, параметры: {string.Join(", ", parameters)},  позиция: {cmd.SourceStartPosition}-{cmd.SourceEndPosition}, строка: {cmd.StringNumber}");
        }
        
        // Ищем конкретные математические команды
        var mathCommandNames = new[] { "(", "[", "$" };
        var foundMathCommands = new List<Command>();
        
        foreach (var cmdName in mathCommandNames)
        {
            var commands = GetAllCommandsByName(cmdName);
            foundMathCommands.AddRange(commands);
            Console.WriteLine($"[DEBUG] IsTextCommandInMathCommand: Найдено команд '\\{cmdName}': {commands.Count}");
        }
        
        Console.WriteLine($"[DEBUG] IsTextCommandInMathCommand: Всего математических команд найдено: {foundMathCommands.Count}");
        
        // Отладочная информация: показываем все математические команды
        foreach (var cmd in foundMathCommands)
        {
            Console.WriteLine($"[DEBUG] IsTextCommandInMathCommand: Математическая команда: '\\{cmd.Name}', позиция {cmd.SourceStartPosition}-{cmd.SourceEndPosition}, строка {cmd.StringNumber}");
        }
        
        foreach (var command in foundMathCommands)
        {
            Console.WriteLine($"[DEBUG] IsTextCommandInMathCommand: Проверяю, находится ли текстовая команда [{textCommand.SourceStartPosition}-{textCommand.SourceEndPosition}] внутри '\\{command.Name}' [{command.SourceStartPosition}-{command.SourceEndPosition}]");
            
            // Проверяем, находится ли текстовая команда внутри математической команды
            if (textCommand.SourceStartPosition >= command.SourceStartPosition && 
                textCommand.SourceEndPosition <= command.SourceEndPosition &&
                textCommand.FileOwner == command.FileOwner &&
                textCommand.StringNumber == command.StringNumber)
            {
                Console.WriteLine($"[DEBUG] IsTextCommandInMathCommand: ✅ Текстовая команда находится внутри математической команды '\\{command.Name}'");
                return true;
            }
        }
        
        Console.WriteLine($"[DEBUG] IsTextCommandInMathCommand: ❌ Текстовая команда НЕ находится внутри математической команды");
        return false;
    }

    /// <summary>
    /// Определяет, находится ли символ в математическом контексте LaTeX
    /// </summary>
    /// <param name="text">Полный текст</param>
    /// <param name="position">Позиция символа в тексте</param>
    /// <returns>True, если символ находится в математическом контексте</returns>
    private bool IsInMathContext(string text, int position)
    {
        Console.WriteLine($"[DEBUG] IsInMathContext: Проверяю позицию {position} в тексте длиной {text.Length}");
        
        if (string.IsNullOrEmpty(text) || position < 0 || position >= text.Length)
        {
            Console.WriteLine($"[DEBUG] IsInMathContext: Неверная позиция, возвращаю false");
            return false;
        }

        // Проверяем inline math: $...$
        var dollarMath = FindMathRanges(text, @"\$", @"\$");
        Console.WriteLine($"[DEBUG] IsInMathContext: Найдено $ math диапазонов: {dollarMath.Count}");
        foreach (var range in dollarMath)
        {
            Console.WriteLine($"[DEBUG] IsInMathContext: $ math диапазон: {range.Start}-{range.End}");
        }
        if (dollarMath.Any(range => position >= range.Start && position <= range.End))
        {
            Console.WriteLine($"[DEBUG] IsInMathContext: Позиция в $ math контексте");
            return true;
        }

        // Проверяем display math: \[...\]
        var displayMath = FindMathRanges(text, @"\\\[", @"\\\]");
        Console.WriteLine($"[DEBUG] IsInMathContext: Найдено \\[\\] math диапазонов: {displayMath.Count}");
        foreach (var range in displayMath)
        {
            Console.WriteLine($"[DEBUG] IsInMathContext: \\[\\] math диапазон: {range.Start}-{range.End}");
        }
        if (displayMath.Any(range => position >= range.Start && position <= range.End))
        {
            Console.WriteLine($"[DEBUG] IsInMathContext: Позиция в \\[\\] math контексте");
            return true;
        }

        // Проверяем inline math: \(...\)
        var parenMath = FindMathRanges(text, @"\\\(", @"\\\)");
        Console.WriteLine($"[DEBUG] IsInMathContext: Найдено \\(\\) math диапазонов: {parenMath.Count}");
        foreach (var range in parenMath)
        {
            Console.WriteLine($"[DEBUG] IsInMathContext: \\(\\) math диапазон: {range.Start}-{range.End}");
        }
        if (parenMath.Any(range => position >= range.Start && position <= range.End))
        {
            Console.WriteLine($"[DEBUG] IsInMathContext: Позиция в \\(\\) math контексте");
            return true;
        }

        // Проверяем math environments
        var mathEnvironments = new[] { "equation", "align", "gather", "multline", "flalign", "alignat", "eqnarray" };
        foreach (var env in mathEnvironments)
        {
            var envRanges = FindMathRanges(text, $@"\\begin\{{{env}\*?\}}", $@"\\end\{{{env}\*?\}}");
            Console.WriteLine($"[DEBUG] IsInMathContext: Найдено {env} environment диапазонов: {envRanges.Count}");
            foreach (var range in envRanges)
            {
                Console.WriteLine($"[DEBUG] IsInMathContext: {env} environment диапазон: {range.Start}-{range.End}");
            }
            if (envRanges.Any(range => position >= range.Start && position <= range.End))
            {
                Console.WriteLine($"[DEBUG] IsInMathContext: Позиция в {env} environment контексте");
                return true;
            }
        }

        Console.WriteLine($"[DEBUG] IsInMathContext: Позиция НЕ в математическом контексте");
        return false;
    }

    /// <summary>
    /// Находит диапазоны математических выражений в тексте
    /// </summary>
    /// <param name="text">Текст для анализа</param>
    /// <param name="startPattern">Регулярное выражение для начала</param>
    /// <param name="endPattern">Регулярное выражение для конца</param>
    /// <returns>Список диапазонов математических выражений</returns>
    private List<(int Start, int End)> FindMathRanges(string text, string startPattern, string endPattern)
    {
        var ranges = new List<(int Start, int End)>();
        
        try
        {
            var startMatches = Regex.Matches(text, startPattern);
            var endMatches = Regex.Matches(text, endPattern);

            int endIndex = 0;
            foreach (Match startMatch in startMatches)
            {
                // Находим соответствующий закрывающий тег
                while (endIndex < endMatches.Count && endMatches[endIndex].Index <= startMatch.Index)
                {
                    endIndex++;
                }

                if (endIndex < endMatches.Count)
                {
                    ranges.Add((startMatch.Index, endMatches[endIndex].Index + endMatches[endIndex].Length - 1));
                    endIndex++;
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // В случае timeout возвращаем пустой список
            return new List<(int Start, int End)>();
        }

        return ranges;
    }

    /// <summary>
    /// Проверяет, является ли символ математическим штрихом (прайм)
    /// </summary>
    /// <param name="text">Текст</param>
    /// <param name="position">Позиция символа</param>
    /// <param name="quote">Символ кавычки</param>
    /// <returns>True, если это математический штрих</returns>
    private bool IsMathematicalPrime(string text, int position, char quote)
    {
        Console.WriteLine($"[DEBUG] IsMathematicalPrime: Проверяю символ '{quote}' в позиции {position}");
        
        // Апострофы (') и двойные апострофы ('') в математическом контексте являются штрихами
        if (quote != '\'' && quote != '"')
        {
            Console.WriteLine($"[DEBUG] IsMathematicalPrime: Символ '{quote}' не является апострофом");
            return false;
        }

        if (!IsInMathContext(text, position))
        {
            Console.WriteLine($"[DEBUG] IsMathematicalPrime: Не в математическом контексте");
            
            // Дополнительная проверка: косвенные признаки математических штрихов
            // Даже если не в явном математическом контексте, проверим признаки
            if (HasMathematicalPrimePattern(text, position, quote))
            {
                Console.WriteLine($"[DEBUG] IsMathematicalPrime: Обнаружен паттерн математического штриха");
                return true;
            }
            
            return false;
        }

        // Находим символ, который предшествует штриху(ам)
        // Пропускаем предыдущие штрихи, чтобы найти основной символ
        int checkPosition = position - 1;
        while (checkPosition >= 0 && text[checkPosition] == '\'')
        {
            checkPosition--;
            Console.WriteLine($"[DEBUG] IsMathematicalPrime: Пропускаю предыдущий штрих в позиции {checkPosition + 1}");
        }
        
        // Если после пропуска штрихов мы можем проверить символ
        if (checkPosition >= 0)
        {
            char prevChar = text[checkPosition];
            Console.WriteLine($"[DEBUG] IsMathematicalPrime: Проверяю символ перед штрихами: '{prevChar}'");
            
            if (char.IsLetterOrDigit(prevChar) || prevChar == ')' || prevChar == '}' || prevChar == ']')
            {
                Console.WriteLine($"[DEBUG] IsMathematicalPrime: Символ '{prevChar}' подходит для штриха");
                return true;
            }
        }
        
        // Дополнительная проверка: может быть пробел между символом и штрихом
        if (position > 1)
        {
            char prevChar = text[position - 1];
            if (char.IsWhiteSpace(prevChar))
            {
                // Проверяем символ перед пробелом, пропуская штрихи
                checkPosition = position - 2;
                while (checkPosition >= 0 && text[checkPosition] == '\'')
                {
                    checkPosition--;
                }
                
                if (checkPosition >= 0)
                {
                    char prevCharWithSpace = text[checkPosition];
                    Console.WriteLine($"[DEBUG] IsMathematicalPrime: Проверяю символ перед пробелом и штрихами: '{prevCharWithSpace}'");
                    
                    if (char.IsLetterOrDigit(prevCharWithSpace) || prevCharWithSpace == ')' || prevCharWithSpace == '}' || prevCharWithSpace == ']')
                    {
                        Console.WriteLine($"[DEBUG] IsMathematicalPrime: Символ '{prevCharWithSpace}' подходит для штриха через пробел");
                        return true;
                    }
                }
            }
        }

        Console.WriteLine($"[DEBUG] IsMathematicalPrime: Не является математическим штрихом");
        return false;
    }

    /// <summary>
    /// Проверяет косвенные признаки математических штрихов (паттерны типа f' = или c'' = )
    /// </summary>
    /// <param name="text">Текст для анализа</param>
    /// <param name="position">Позиция кавычки</param>
    /// <param name="quote">Символ кавычки</param>
    /// <returns>True, если обнаружен паттерн математического штриха</returns>
    private bool HasMathematicalPrimePattern(string text, int position, char quote)
    {
        Console.WriteLine($"[DEBUG] HasMathematicalPrimePattern: Проверяю косвенные признаки для '{quote}' в позиции {position}");
        
        // Проверяем паттерн: [переменная][штрихи][пробелы]=
        // Например: f' =, c'' =, x''' =
        
        // Сначала находим символ перед штрихами
        int checkPosition = position - 1;
        
        // Пропускаем предыдущие штрихи
        int primeCount = 1; // Текущий штрих
        while (checkPosition >= 0 && text[checkPosition] == '\'')
        {
            checkPosition--;
            primeCount++;
        }
        
        Console.WriteLine($"[DEBUG] HasMathematicalPrimePattern: Обнаружено штрихов: {primeCount}");
        
        // Проверяем, есть ли переменная перед штрихами
        if (checkPosition >= 0)
        {
            char prevChar = text[checkPosition];
            Console.WriteLine($"[DEBUG] HasMathematicalPrimePattern: Символ перед штрихами: '{prevChar}'");
            
            // Переменная должна быть буквой или цифрой или закрывающей скобкой
            if (char.IsLetterOrDigit(prevChar) || prevChar == ')' || prevChar == '}' || prevChar == ']')
            {
                // Теперь проверяем что идет после штрихов
                int afterPrimePosition = position + 1;
                
                // Пропускаем остальные штрихи после текущего
                while (afterPrimePosition < text.Length && text[afterPrimePosition] == '\'')
                {
                    afterPrimePosition++;
                }
                
                Console.WriteLine($"[DEBUG] HasMathematicalPrimePattern: Позиция после всех штрихов: {afterPrimePosition}");
                
                // Пропускаем пробелы после штрихов
                while (afterPrimePosition < text.Length && char.IsWhiteSpace(text[afterPrimePosition]))
                {
                    afterPrimePosition++;
                }
                
                // Проверяем наличие знака равенства
                if (afterPrimePosition < text.Length && text[afterPrimePosition] == '=')
                {
                    Console.WriteLine($"[DEBUG] HasMathematicalPrimePattern: Обнаружен паттерн математического штриха: переменная + штрихи + равно");
                    return true;
                }
                
                // Дополнительная проверка: штрихи в конце выражения перед скобкой или оператором
                if (afterPrimePosition < text.Length)
                {
                    char nextChar = text[afterPrimePosition];
                    if (nextChar == '(' || nextChar == '+' || nextChar == '-' || nextChar == '*' || nextChar == '/')
                    {
                        Console.WriteLine($"[DEBUG] HasMathematicalPrimePattern: Обнаружен паттерн: переменная + штрихи + математический оператор '{nextChar}'");
                        return true;
                    }
                }
                
                // Дополнительная проверка: штрихи в конце строки (может быть часть математического выражения)
                if (afterPrimePosition >= text.Length || text[afterPrimePosition] == '\n')
                {
                    Console.WriteLine($"[DEBUG] HasMathematicalPrimePattern: Штрихи в конце строки - возможно математическое выражение");
                    return true;
                }
            }
        }
        
        Console.WriteLine($"[DEBUG] HasMathematicalPrimePattern: Косвенные признаки не обнаружены");
        return false;
    }

    /// <summary>
    /// Находит позиции неправильных кавычек в тексте
    /// </summary>
    /// <param name="text">Текст для анализа</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="baseLineNumber">Базовый номер строки</param>
    /// <param name="command">Команда, содержащая информацию о позициях (опционально)</param>
    /// <returns>Список ошибок с диагностической информацией</returns>
    private List<TestError> FindQuotationMarkErrors(string text, string fileName, int baseLineNumber, Command command = null)
    {
        var errors = new List<TestError>();
        
        Console.WriteLine($"[DEBUG] FindQuotationMarkErrors: Анализирую текст: '{text}'");
        
        if (string.IsNullOrEmpty(text))
        {
            Console.WriteLine($"[DEBUG] FindQuotationMarkErrors: Текст пустой, пропускаем");
            return errors;
        }

        // Ищем кавычки в тексте
        for (int charIndex = 0; charIndex < text.Length; charIndex++)
        {
            char currentChar = text[charIndex];
            
            foreach (var invalidQuote in _invalidQuotes)
            {
                if (currentChar == invalidQuote)
                {
                    Console.WriteLine($"[DEBUG] FindQuotationMarkErrors: Найдена запрещенная кавычка '{invalidQuote}' в позиции {charIndex}");
                    
                    // Проверяем контекст математики
                    bool isInMath = IsInMathContext(text, charIndex);
                    Console.WriteLine($"[DEBUG] FindQuotationMarkErrors: В математическом контексте: {isInMath}");
                    
                    // Проверяем, не является ли это математическим штрихом
                    bool isMathPrime = IsMathematicalPrime(text, charIndex, invalidQuote);
                    Console.WriteLine($"[DEBUG] FindQuotationMarkErrors: Является математическим штрихом: {isMathPrime}");
                    
                    if (isMathPrime)
                    {
                        Console.WriteLine($"[DEBUG] FindQuotationMarkErrors: Пропускаем математический штрих");
                        continue; // Пропускаем математические штрихи
                    }

                    int lineNumber;
                    int columnNumber;
                    
                    // Если у нас есть команда с позиционной информацией, используем её
                    if (command != null && command.SourceStartLine > 0)
                    {
                        // Вычисляем относительную позицию внутри текста команды
                        int relativeLineOffset = 0;
                        int relativeColumnOffset = charIndex;
                        
                        // Подсчитываем количество переносов строк до текущей позиции
                        for (int i = 0; i < charIndex; i++)
                        {
                            if (text[i] == '\n')
                            {
                                relativeLineOffset++;
                                relativeColumnOffset = charIndex - i - 1;
                            }
                        }
                        
                        lineNumber = command.SourceStartLine + relativeLineOffset;
                        columnNumber = (relativeLineOffset == 0) 
                            ? command.SourceStartColumn + relativeColumnOffset 
                            : relativeColumnOffset + 1;
                    }
                    else
                    {
                        // Fallback: вычисляем позицию в тексте
                        lineNumber = baseLineNumber;
                        columnNumber = 1;
                        
                        for (int i = 0; i < charIndex; i++)
                        {
                            if (text[i] == '\n')
                            {
                                lineNumber++;
                                columnNumber = 1;
                            }
                            else
                            {
                                columnNumber++;
                            }
                        }
                    }
                    
                    // Получаем строку для контекста
                    var lines = text.Split('\n');
                    var relativeLineIndex = 0;
                    int currentPos = 0;
                    
                    // Находим в какой строке находится символ
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (currentPos + lines[i].Length >= charIndex)
                        {
                            relativeLineIndex = i;
                            break;
                        }
                        currentPos += lines[i].Length + 1; // +1 для \n
                    }
                    
                    var currentLine = relativeLineIndex < lines.Length ? lines[relativeLineIndex] : "";
                    
                    // Определяем предлагаемую замену
                    string suggestedReplacement = invalidQuote switch
                    {
                        '"' => "«»", // Обычные двойные кавычки  
                        '\'' => "«»", // Обычные одинарные кавычки
                        '`' => "«»", // Обратные кавычки
                        _ => "«»",
                    };

                    // Создаем контекст ошибки (часть строки вокруг кавычки)
                    // Определяем позицию кавычки в текущей строке для создания контекста
                    var quotePositionInLine = 1;
                    for (int pos = currentPos; pos < charIndex; pos++)
                    {
                        if (text[pos] != '\n')
                        {
                            quotePositionInLine++;
                        }
                    }
                    
                    var quoteIndex = quotePositionInLine - 1; // 0-based позиция кавычки в currentLine
                    var contextStart = Math.Max(0, quoteIndex - 5); // 5 символов слева
                    var contextEnd = Math.Min(currentLine.Length, quoteIndex + 6); // 5 символов справа + сама кавычка
                    var context = contextStart < currentLine.Length ? 
                        currentLine.Substring(contextStart, contextEnd - contextStart) : 
                        currentLine;
                    
                    // Создаем предлагаемое исправление строки
                    var suggestedFix = currentLine.Replace(invalidQuote.ToString(), suggestedReplacement);

                    var error = new TestError
                    {
                        ErrorType = ErrorType.Warning,
                        FileName = fileName,
                        LineNumber = lineNumber,
                        ColumnNumber = columnNumber,
                        EndLineNumber = lineNumber,
                        EndColumnNumber = columnNumber + 1,
                        OriginalText = context,
                        SuggestedFix = suggestedFix
                    };
                    
                    errors.Add(error);
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Устаревший метод для обратной совместимости
    /// </summary>
    [Obsolete("Используйте FindQuotationMarkErrors для получения точных позиций")]
    private int FindMistakeQuatationMarksInText(string text)
    {
        var errors = FindQuotationMarkErrors(text, "", 1);
        return errors.Count > 0 ? errors[0].ColumnNumber ?? 0 : 0;
    }
}
