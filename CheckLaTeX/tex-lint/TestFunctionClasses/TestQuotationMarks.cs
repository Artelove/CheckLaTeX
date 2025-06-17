using System.Text.Json;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

public class TestQuotationMarks : TestFunction
{
    private readonly char[] _invalidQuotes;

    public TestQuotationMarks(ILatexConfigurationService configurationService, string requestId) 
        : base(configurationService, requestId)
    {
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
            var errors = FindQuotationMarkErrors(text, textCommand.FileOwner, textCommand.StringNumber, textCommand);
            
            foreach (var error in errors)
            {
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
            return errors;

        // Ищем кавычки в тексте
        for (int charIndex = 0; charIndex < text.Length; charIndex++)
        {
            char currentChar = text[charIndex];
            
            foreach (var invalidQuote in _invalidQuotes)
            {
                if (currentChar == invalidQuote)
                {
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
                        '"' => "<<>>", // Обычные двойные кавычки  
                        '\'' => "<<>>", // Обычные одинарные кавычки
                        _ => "<<>>",
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
