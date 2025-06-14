using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

public class TestHyphenInsteadOfDash : TestFunction
{
    private readonly char _wrongHyphen;
    private readonly ILatexConfigurationService _configurationService;

    public TestHyphenInsteadOfDash(ILatexConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        
        var rulesJson = File.ReadAllText(TestUtilities.FindConfigFile("lint-rules.json"));
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
        
        foreach (var command in TestUtilities.FoundsCommandsWithLstlisting)
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
                var textErrors = FindHyphenErrors(argText, command.FileOwner, command.StringNumber);
                foreach (var error in textErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование дефиса вместо тире в аргументе команды",
                        error.FileName ?? command.FileOwner,
                        error.LineNumber ?? command.StringNumber,
                        error.ColumnNumber ?? 1,
                        error.OriginalText ?? argText,
                        suggestedFix: error.SuggestedFix,
                        errorCommand: command
                    ));
                }
                
                // Ищем ошибки в значении аргумента
                var valueErrors = FindHyphenErrors(argValue, command.FileOwner, command.StringNumber);
                foreach (var error in valueErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование дефиса вместо тире в аргументе команды",
                        error.FileName ?? command.FileOwner,
                        error.LineNumber ?? command.StringNumber,
                        error.ColumnNumber ?? 1,
                        error.OriginalText ?? argValue,
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
                var textErrors = FindHyphenErrors(paramText, command.FileOwner, command.StringNumber);
                foreach (var error in textErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование дефиса вместо тире в параметре команды",
                        error.FileName ?? command.FileOwner,
                        error.LineNumber ?? command.StringNumber,
                        error.ColumnNumber ?? 1,
                        error.OriginalText ?? paramText,
                        suggestedFix: error.SuggestedFix,
                        errorCommand: command
                    ));
                }
                
                // Ищем ошибки в значении параметра
                var valueErrors = FindHyphenErrors(paramValue, command.FileOwner, command.StringNumber);
                foreach (var error in valueErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование дефиса вместо тире в параметре команды",
                        error.FileName ?? command.FileOwner,
                        error.LineNumber ?? command.StringNumber,
                        error.ColumnNumber ?? 1,
                        error.OriginalText ?? paramValue,
                        suggestedFix: error.SuggestedFix,
                        errorCommand: command
                    ));
                }
            }
        }
        
        var textCommands = TestUtilities.GetAllCommandsByName(TextCommand.TEXT_COMMAND_NAME);
        
        foreach (var environmentCommand in TestUtilities.GetAllEnvironment())
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
        foreach (var environment in TestUtilities.GetAllEnvironment())
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
            var errors = FindHyphenErrors(text, textCommand.FileOwner, textCommand.StringNumber);
            
            foreach (var error in errors)
            {
                Errors.Add(TestError.CreateWithDiagnostics(
                    ErrorType.Warning,
                    "Обнаружено использование дефиса вместо тире в тексте",
                    error.FileName ?? textCommand.FileOwner,
                    error.LineNumber ?? textCommand.StringNumber,
                    error.ColumnNumber ?? 1,
                    error.OriginalText ?? text,
                    suggestedFix: error.SuggestedFix,
                    errorCommand: textCommand
                ));
            }
        }
    }

    /// <summary>
    /// Находит позиции неправильного использования дефиса вместо тире
    /// </summary>
    /// <param name="text">Текст для анализа</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="baseLineNumber">Базовый номер строки</param>
    /// <returns>Список ошибок с диагностической информацией</returns>
    private List<TestError> FindHyphenErrors(string text, string fileName, int baseLineNumber)
    {
        var errors = new List<TestError>();
        
        if (string.IsNullOrEmpty(text))
            return errors;

        var lines = text.Split('\n');
        
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            
            for (int charIndex = 0; charIndex < line.Length; charIndex++)
            {
                if (line[charIndex] != _wrongHyphen)
                    continue;
                
                bool left = true;
                bool right = true;
                
                // Проверяем, что слева и справа от символа есть пробелы (тире должно быть окружено пробелами)
                if (charIndex - 1 >= 0)
                    left = char.IsWhiteSpace(line[charIndex - 1]);
                
                if (charIndex + 1 < line.Length)
                    right = char.IsWhiteSpace(line[charIndex + 1]);
                
                if (left && right)
                {
                    // Создаем контекст ошибки
                    var contextStart = Math.Max(0, charIndex - 10);
                    var contextEnd = Math.Min(line.Length, charIndex + 10);
                    var context = line.Substring(contextStart, contextEnd - contextStart);
                    
                    // Создаем предлагаемое исправление
                    var suggestedFix = line.Replace(_wrongHyphen.ToString(), "---"); // Заменяем на тире LaTeX
                    
                    var error = new TestError
                    {
                        ErrorType = ErrorType.Warning,
                        FileName = fileName,
                        LineNumber = baseLineNumber + lineIndex,
                        ColumnNumber = charIndex + 1, // 1-based
                        EndLineNumber = baseLineNumber + lineIndex,
                        EndColumnNumber = charIndex + 2, // После символа
                        OriginalText = context,
                        SuggestedFix = suggestedFix
                    };
                    error.ErrorInfo = $"Дефис '{_wrongHyphen}' должен быть заменен на тире в позиции {charIndex + 1}";
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
