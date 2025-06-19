using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

public class TestQuotationMarks : TestFunction
{
    private readonly char[] _invalidQuotes;
    private readonly HashSet<string> _reportedErrors; // Для дедупликации ошибок

    public TestQuotationMarks(ILatexConfigurationService configurationService, string requestId) 
        : base(configurationService, requestId)
    {
        _reportedErrors = new HashSet<string>();
        
        var lintRulesJson = File.ReadAllText(PathToLintRulesJson);
        var lintRules = JsonSerializer.Deserialize<LintRules>(lintRulesJson);

        _invalidQuotes = lintRules?.QuotationMarks.Forbidden.Select(s => s[0]).ToArray() ?? Array.Empty<char>();

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
                    AddErrorWithDeduplication(error, 
                        "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в аргументе команды",
                        command, argText);
                }
                
                // Ищем ошибки в значении аргумента (только если отличается от текста)
                if (argValue != argText)
                {
                    var valueErrors = FindQuotationMarkErrors(argValue, command.FileOwner, command.StringNumber, command);
                    foreach (var error in valueErrors)
                    {
                        AddErrorWithDeduplication(error,
                            "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в аргументе команды",
                            command, argValue);
                    }
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
                    AddErrorWithDeduplication(error,
                        "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в параметре команды",
                        command, paramText);
                }
                
                // Ищем ошибки в значении параметра (только если отличается от текста)
                if (paramValue != paramText)
                {
                    var valueErrors = FindQuotationMarkErrors(paramValue, command.FileOwner, command.StringNumber, command);
                    foreach (var error in valueErrors)
                    {
                        AddErrorWithDeduplication(error,
                            "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в параметре команды",
                            command, paramValue);
                    }
                }
            }
        }  
        
        var textCommands = GetAllCommandsByName("TEXT_NAME");
        
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

        foreach (var command in textCommands)
        {
            var textCommand = (TextCommand)command;
            var text = textCommand?.Text ?? string.Empty;
            
            // Проверяем, находится ли эта текстовая команда внутри математического окружения
            bool isInMathEnvironment = IsTextCommandInMathEnvironment(textCommand);
            
            // Проверяем, находится ли команда внутри математической команды \(...\) или \[...\]
            bool isInMathCommand = !isInMathEnvironment && IsTextCommandInMathCommand(textCommand);
            
            if (isInMathEnvironment || isInMathCommand)
            {
                continue; // Пропускаем анализ кавычек для текста в математических окружениях и командах
            }
            
            var errors = FindQuotationMarkErrors(text, textCommand.FileOwner, textCommand.StringNumber, textCommand);
            
            foreach (var error in errors)
            {
                AddErrorWithDeduplication(error,
                    "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в тексте",
                    textCommand, text);
            }
        }
    }

    /// <summary>
    /// Добавляет ошибку с проверкой на дублирование
    /// </summary>
    private void AddErrorWithDeduplication(TestError baseError, string message, Command command, string originalText)
    {
        // Создаем уникальный ключ для ошибки на основе файла, строки и колонки
        var errorKey = $"{baseError.FileName}:{baseError.LineNumber}:{baseError.ColumnNumber}";
        
        // Проверяем, была ли уже зарегистрирована ошибка в этой позиции
        if (_reportedErrors.Contains(errorKey))
        {
            return;
        }
        
        // Регистрируем ошибку
        _reportedErrors.Add(errorKey);
        
        // Добавляем ошибку в список
        Errors.Add(TestError.CreateWithDiagnostics(
            ErrorType.Warning,
            message,
            baseError.FileName ?? command.FileOwner,
            baseError.LineNumber ?? command.StringNumber,
            baseError.ColumnNumber ?? 1,
            baseError.OriginalText ?? originalText,
            baseError.EndLineNumber,
            baseError.EndColumnNumber,
            suggestedFix: baseError.SuggestedFix,
            errorCommand: command
        ));
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
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Проверяет, находится ли текстовая команда внутри математической команды типа \(...\) или $...$
    /// </summary>
    /// <param name="textCommand">Текстовая команда для проверки</param>
    /// <returns>True, если команда находится внутри математической команды</returns>
    private bool IsTextCommandInMathCommand(TextCommand textCommand)
    {
        var allCommands = GetAllCommandsByName("");
        
        var mathCommandNames = new[] { "(", "[", "$" };
        var foundMathCommands = new List<Command>();
        
        foreach (var cmdName in mathCommandNames)
        {
            var commands = GetAllCommandsByName(cmdName);
            foundMathCommands.AddRange(commands);
        }
        
        foreach (var command in foundMathCommands)
        {
            if (textCommand.SourceStartPosition >= command.SourceStartPosition && 
                textCommand.SourceEndPosition <= command.SourceEndPosition &&
                textCommand.FileOwner == command.FileOwner &&
                textCommand.StringNumber == command.StringNumber)
            {
                return true;
            }
        }
        
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
        if (string.IsNullOrEmpty(text) || position < 0 || position >= text.Length)
        {
            return false;
        }

        // Проверяем inline math: $...$
        var dollarMath = FindMathRanges(text, @"\$", @"\$");
        if (dollarMath.Any(range => position >= range.Start && position <= range.End))
        {
            return true;
        }

        // Проверяем display math: \[...\]
        var displayMath = FindMathRanges(text, @"\\\[", @"\\\]");
        if (displayMath.Any(range => position >= range.Start && position <= range.End))
        {
            return true;
        }

        // Проверяем inline math: \(...\)
        var parenMath = FindMathRanges(text, @"\\\(", @"\\\)");
        if (parenMath.Any(range => position >= range.Start && position <= range.End))
        {
            return true;
        }

        // Проверяем math environments
        var mathEnvironments = new[] { "equation", "align", "gather", "multline", "flalign", "alignat", "eqnarray" };
        foreach (var env in mathEnvironments)
        {
            var envRanges = FindMathRanges(text, $@"\\begin\{{{env}\*?\}}", $@"\\end\{{{env}\*?\}}");
            if (envRanges.Any(range => position >= range.Start && position <= range.End))
            {
                return true;
            }
        }

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
        // Апострофы (') и двойные апострофы ('') в математическом контексте являются штрихами
        if (quote != '\'' && quote != '"')
        {
            return false;
        }

        if (!IsInMathContext(text, position))
        {
            // Дополнительная проверка: косвенные признаки математических штрихов
            // Даже если не в явном математическом контексте, проверим признаки
            if (HasMathematicalPrimePattern(text, position, quote))
            {
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
        }
        
        // Если после пропуска штрихов мы можем проверить символ
        if (checkPosition >= 0)
        {
            char prevChar = text[checkPosition];
            
            if (char.IsLetterOrDigit(prevChar) || prevChar == ')' || prevChar == '}' || prevChar == ']')
            {
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
                    
                    if (char.IsLetterOrDigit(prevCharWithSpace) || prevCharWithSpace == ')' || prevCharWithSpace == '}' || prevCharWithSpace == ']')
                    {
                        return true;
                    }
                }
            }
        }

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
        
        // Проверяем, есть ли переменная перед штрихами
        if (checkPosition >= 0)
        {
            char prevChar = text[checkPosition];
            
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
                
                // Пропускаем пробелы после штрихов
                while (afterPrimePosition < text.Length && char.IsWhiteSpace(text[afterPrimePosition]))
                {
                    afterPrimePosition++;
                }
                
                // Проверяем наличие знака равенства
                if (afterPrimePosition < text.Length && text[afterPrimePosition] == '=')
                {
                    return true;
                }
                
                // Дополнительная проверка: штрихи в конце выражения перед скобкой или оператором
                if (afterPrimePosition < text.Length)
                {
                    char nextChar = text[afterPrimePosition];
                    if (nextChar == '(' || nextChar == '+' || nextChar == '-' || nextChar == '*' || nextChar == '/')
                    {
                        return true;
                    }
                }
                
                // Дополнительная проверка: штрихи в конце строки (может быть часть математического выражения)
                if (afterPrimePosition >= text.Length || text[afterPrimePosition] == '\n')
                {
                    return true;
                }
            }
        }
        
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
    
        if (string.IsNullOrEmpty(text))
        {
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
                    // Проверяем контекст математики
                    bool isInMath = IsInMathContext(text, charIndex);
                    
                    // Проверяем, не является ли это математическим штрихом
                    bool isMathPrime = IsMathematicalPrime(text, charIndex, invalidQuote);
                    
                    if (isMathPrime)
                    {
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
