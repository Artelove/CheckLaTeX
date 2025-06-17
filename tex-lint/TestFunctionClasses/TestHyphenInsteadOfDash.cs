using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

public class TestHyphenInsteadOfDash : TestFunction
{
    private readonly char _wrongHyphen;

    public TestHyphenInsteadOfDash(ILatexConfigurationService configurationService, string requestId) 
        : base(configurationService, requestId)
    {
        var rulesJson = File.ReadAllText(PathToLintRulesJson);
        var rules = JsonSerializer.Deserialize<LintRules>(rulesJson);
        
        _wrongHyphen = string.IsNullOrEmpty(rules?.Hyphen.WrongSymbol) ? '-' : rules.Hyphen.WrongSymbol[0];

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
                var textErrors = FindHyphenErrors(argText, command.FileOwner, command.StringNumber, command);
                foreach (var error in textErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование дефиса вместо тире в аргументе команды",
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
                var valueErrors = FindHyphenErrors(argValue, command.FileOwner, command.StringNumber, command);
                foreach (var error in valueErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование дефиса вместо тире в аргументе команды",
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
                var paramText = parameter?.Text ?? string.Empty;
                var paramValue = parameter?.Value ?? string.Empty;
                
                // Ищем ошибки в тексте параметра
                var textErrors = FindHyphenErrors(paramText, command.FileOwner, command.StringNumber, command);
                foreach (var error in textErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование дефиса вместо тире в параметре команды",
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
                var valueErrors = FindHyphenErrors(paramValue, command.FileOwner, command.StringNumber, command);
                foreach (var error in valueErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование дефиса вместо тире в параметре команды",
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
        
        var textCommands = GetAllCommandsByName(TextCommand.TEXT_COMMAND_NAME);
        
        foreach (var environmentCommand in GetAllEnvironment())
        {
            if (environmentCommand.EnvironmentName == "lstlisting")
            {
                foreach (var innerCommand in environmentCommand.InnerCommands)
                {
                    if (innerCommand is TextCommand textCommand)
                    {
                        textCommands.Remove(textCommand);
                    }
                }
            }
        }

        var ignoreEnvironments = new List<EnvironmentCommand> ();
        foreach (var environment in GetAllEnvironment())
        {
            if(environment.EnvironmentName == "equation" ||
               environment.EnvironmentName == "comment")
                ignoreEnvironments.Add(environment);
        }

        var  next = false;
        for (var i = 0; i < textCommands.Count; i++)
        {
            next = false;
            foreach (var environmentCommand in ignoreEnvironments)
            {
                foreach (var innerCommand in environmentCommand.InnerCommands)
                {
                    if (innerCommand is EnvironmentCommand _innerCommand)
                    {
                        foreach (var __innerCommand in _innerCommand.InnerCommands)
                        {
                            if (__innerCommand is TextCommand _textCommand)
                            {
                                if (_textCommand == textCommands[i])
                                {
                                    textCommands[i] = null;
                                    next = true;
                                    break;
                                }
                            }
                            
                            if(next)
                                break;
                        }
                    }
                    
                    if(next)
                        break;

                    if (innerCommand is not TextCommand textCommand)
                        continue;

                    if (textCommand != textCommands[i])
                        continue;
                    
                    textCommands[i] = null;
                    next = true;
                    break;
                }
                
                if(next)
                    break;
            }
        }

        foreach (var command in textCommands)
        {   
            if(command==null) 
                continue;
            
            var textCommand = (TextCommand)command;
            var text = textCommand?.Text ?? string.Empty;
            var errors = FindHyphenErrors(text, textCommand.FileOwner, textCommand.StringNumber, textCommand);
            
            foreach (var error in errors)
            {
                Errors.Add(TestError.CreateWithDiagnostics(
                    ErrorType.Warning,
                    "Обнаружено использование дефиса вместо тире в тексте",
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
    /// Находит позиции неправильных дефисов в тексте
    /// </summary>
    /// <param name="text">Текст для анализа</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="baseLineNumber">Базовый номер строки</param>
    /// <param name="command">Команда, содержащая информацию о позициях (опционально)</param>
    /// <returns>Список ошибок с диагностической информацией</returns>
    private List<TestError> FindHyphenErrors(string text, string fileName, int baseLineNumber, Command command = null)
    {
        var errors = new List<TestError>();
        
        if (string.IsNullOrEmpty(text))
            return errors;

        // Ищем дефисы в тексте
        for (int charIndex = 0; charIndex < text.Length; charIndex++)
        {
            char currentChar = text[charIndex];
            
            if (currentChar == _wrongHyphen)
            {
                // Вычисляем позицию в строке для проверки окружения пробелами
                int lineNumber = 1;
                int columnNumber = 1;
                
                // Подсчитываем строки и столбцы до текущей позиции
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
                
                // Получаем строку, в которой находится дефис
                var lines = text.Split('\n');
                var currentLine = lineNumber <= lines.Length ? lines[lineNumber - 1] : "";
                
                bool left = true;
                bool right = true;
                
                // Проверяем, что слева и справа от символа есть пробелы (тире должно быть окружено пробелами)
                if (columnNumber - 2 >= 0 && columnNumber - 2 < currentLine.Length)
                    left = char.IsWhiteSpace(currentLine[columnNumber - 2]);
                
                if (columnNumber < currentLine.Length)
                    right = char.IsWhiteSpace(currentLine[columnNumber]);
                
                // Только если дефис окружен пробелами, он должен быть заменен на тире
                if (left && right)
                {
                    int finalLineNumber;
                    int finalColumnNumber;
                    
                    // Если у нас есть команда с позиционной информацией, используем её для финальных координат
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
                        
                        finalLineNumber = command.SourceStartLine + relativeLineOffset;
                        finalColumnNumber = (relativeLineOffset == 0) 
                            ? command.SourceStartColumn + relativeColumnOffset 
                            : relativeColumnOffset + 1;
                    }
                    else
                    {
                        // Fallback: используем базовую позицию
                        finalLineNumber = baseLineNumber + lineNumber - 1;
                        finalColumnNumber = columnNumber;
                    }

                    // Создаем контекст ошибки (часть строки вокруг дефиса)
                    // columnNumber - это 1-based относительная позиция в тексте команды
                    var hyphenIndex = columnNumber - 1; // 0-based позиция дефиса в currentLine
                    var contextStart = Math.Max(0, hyphenIndex - 5); // 5 символов слева
                    var contextEnd = Math.Min(currentLine.Length, hyphenIndex + 6); // 5 символов справа + сам дефис
                    var context = contextStart < currentLine.Length ? 
                        currentLine.Substring(contextStart, contextEnd - contextStart) : 
                        currentLine;
                    
                    // Создаем предлагаемое исправление строки (заменяем дефис на тире LaTeX)
                    var suggestedFix = currentLine.Replace(_wrongHyphen.ToString(), "---");

                    var error = new TestError
                    {
                        ErrorType = ErrorType.Warning,
                        FileName = fileName,
                        LineNumber = finalLineNumber,
                        ColumnNumber = finalColumnNumber,
                        EndLineNumber = finalLineNumber,
                        EndColumnNumber = finalColumnNumber + 1,
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
    [Obsolete("Используйте FindHyphenErrors для получения точных позиций")]
    private int FindMistakeHyphenInText(string text)
    {
        var errors = FindHyphenErrors(text, "", 1);
        return errors.Count > 0 ? errors[0].ColumnNumber ?? 0 : 0;
    }
}
