using System.Text.Json;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

public class TestQuotationMarks : TestFunction
{
    private readonly char[] _invalidQuotes;
    private readonly ILatexConfigurationService _configurationService;

    public TestQuotationMarks(ILatexConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        
        var lintRulesJson = File.ReadAllText(TestUtilities.FindConfigFile("lint-rules.json"));
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
                var textErrors = FindQuotationMarkErrors(argText, command.FileOwner, command.StringNumber);
                foreach (var error in textErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в аргументе команды",
                        error.FileName ?? command.FileOwner,
                        error.LineNumber ?? command.StringNumber,
                        error.ColumnNumber ?? 1,
                        error.OriginalText ?? argText,
                        suggestedFix: error.SuggestedFix,
                        errorCommand: command
                    ));
                }
                
                // Ищем ошибки в значении аргумента
                var valueErrors = FindQuotationMarkErrors(argValue, command.FileOwner, command.StringNumber);
                foreach (var error in valueErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в аргументе команды",
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
                var paramText = parameter.Text ?? string.Empty;
                var paramValue = parameter.Value ?? string.Empty;
                
                // Ищем ошибки в тексте параметра
                var textErrors = FindQuotationMarkErrors(paramText, command.FileOwner, command.StringNumber);
                foreach (var error in textErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в параметре команды",
                        error.FileName ?? command.FileOwner,
                        error.LineNumber ?? command.StringNumber,
                        error.ColumnNumber ?? 1,
                        error.OriginalText ?? paramText,
                        suggestedFix: error.SuggestedFix,
                        errorCommand: command
                    ));
                }
                
                // Ищем ошибки в значении параметра
                var valueErrors = FindQuotationMarkErrors(paramValue, command.FileOwner, command.StringNumber);
                foreach (var error in valueErrors)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в параметре команды",
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
        
        var textCommands = TestUtilities.GetAllCommandsByName("TEXT_NAME");
        foreach (var environmentCommand in TestUtilities.GetAllEnvironment())
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
            var errors = FindQuotationMarkErrors(text, textCommand.FileOwner, textCommand.StringNumber);
            
            foreach (var error in errors)
            {
                Errors.Add(TestError.CreateWithDiagnostics(
                    ErrorType.Warning,
                    "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в тексте",
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
    /// Находит позиции неправильных кавычек в тексте
    /// </summary>
    /// <param name="text">Текст для анализа</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="baseLineNumber">Базовый номер строки</param>
    /// <returns>Список ошибок с диагностической информацией</returns>
    private List<TestError> FindQuotationMarkErrors(string text, string fileName, int baseLineNumber)
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
                char currentChar = line[charIndex];
                
                foreach (var invalidQuote in _invalidQuotes)
                {
                    if (currentChar == invalidQuote)
                    {
                        // Определяем предлагаемую замену
                        string suggestedReplacement = invalidQuote switch
                        {
                            '"' => "<<>>", // Обычные двойные кавычки
                            '\'' => "<<>>", // Обычные одинарные кавычки
                            _ => "<<>>"
                        };

                        // Создаем контекст ошибки (слово или предложение вокруг кавычки)
                        var contextStart = Math.Max(0, charIndex - 10);
                        var contextEnd = Math.Min(line.Length, charIndex + 10);
                        var context = line.Substring(contextStart, contextEnd - contextStart);
                        
                        // Создаем предлагаемое исправление всей строки
                        var suggestedFix = line.Replace(invalidQuote.ToString(), suggestedReplacement);

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
                        error.ErrorInfo = $"Неправильная кавычка '{invalidQuote}' в позиции {charIndex + 1}";
                        errors.Add(error);
                    }
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
