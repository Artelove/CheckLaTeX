using System.Text.RegularExpressions;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

/// <summary>
/// Тестовая функция для проверки одиночных переносов строк в LaTeX тексте
/// Находит одиночные \n после начала реального текста и предлагает заменить на \\
/// </summary>
public class TestLineBreaks : TestFunction
{
    private readonly LineBreakRule _config;

    public TestLineBreaks(ILatexConfigurationService configurationService, string requestId) 
        : base(configurationService, requestId)
    {
        _config = configurationService.GetLineBreakConfig();
        RunCheck();
    }

    private void RunCheck()
    {
        if (!_config.CheckSingleNewlines)
            return;
            
        var commands = FoundsCommandsWithLstlisting;
        
        // Получаем только текстовые команды для анализа переносов строк
        var textCommands = commands.OfType<TextCommand>().ToList();
        
        // Проверяем одиночные \n в текстовых командах
        CheckSingleNewlinesInTextCommands(textCommands);
    }

    /// <summary>
    /// Проверяет одиночные переносы строк в текстовых командах
    /// </summary>
    private void CheckSingleNewlinesInTextCommands(List<TextCommand> textCommands)
    {
        foreach (var textCommand in textCommands)
        {
            var text = textCommand.Text;
            if (string.IsNullOrEmpty(text))
                continue;
                
            CheckSingleNewlinesInText(textCommand, text);
        }
    }

    /// <summary>
    /// Проверяет одиночные переносы строк в тексте после начала реального контента
    /// Ищет случаи, где пользователь случайно оставил один \n и продолжил писать текст
    /// </summary>
    private void CheckSingleNewlinesInText(TextCommand textCommand, string text)
    {
        // Находим первый символ, который не является пробелом, табом или переносом строки
        var firstRealTextMatch = Regex.Match(text, @"[^\s]");
        if (!firstRealTextMatch.Success)
            return; // Только пробелы - игнорируем
            
        int realTextStart = firstRealTextMatch.Index;
        
        // Ищем одиночные \n после начала реального текста, за которыми следует еще текст
        // Паттерн: \n (не \\), после которого есть непустые символы
        var textAfterStart = text.Substring(realTextStart);
        
        // Ищем одиночный \n, которому не предшествует \, и после которого есть непустые символы
        var singleNewlinePattern = @"(?<!\\)\n(?=.*[^\s])";
        var matches = Regex.Matches(textAfterStart, singleNewlinePattern);
        
        foreach (Match match in matches)
        {
            // Проверяем, что после \n действительно есть текст (не только пробелы до конца)
            int newlinePosition = match.Index;
            string afterNewline = textAfterStart.Substring(newlinePosition + 1);
            
            // Если после \n есть непустые символы, это потенциальная ошибка
            if (Regex.IsMatch(afterNewline, @"[^\s]"))
            {
                // Проверяем контекст - не является ли это частью LaTeX команды
                int absolutePosition = realTextStart + match.Index;
                if (!IsPartOfLatexCommand(text, absolutePosition))
                {
                    AddError(
                        ErrorType.Warning,
                        _config.SingleNewlineMessage,
                        textCommand,
                        $"Замените одиночный перенос строки на {_config.PreferredLineBreak}"
                    );
                }
            }
        }
    }

    /// <summary>
    /// Проверяет, является ли символ частью LaTeX команды
    /// </summary>
    private bool IsPartOfLatexCommand(string text, int position)
    {
        // Ищем назад от позиции, чтобы найти ближайший \
        for (int i = position - 1; i >= 0; i--)
        {
            if (text[i] == '\\')
            {
                // Проверяем, что между \ и позицией только буквы (имя команды)
                var between = text.Substring(i + 1, position - i - 1);
                return Regex.IsMatch(between, @"^[a-zA-Z]*$");
            }
            else if (!char.IsLetter(text[i]))
            {
                break;
            }
        }
        
        return false;
    }
} 