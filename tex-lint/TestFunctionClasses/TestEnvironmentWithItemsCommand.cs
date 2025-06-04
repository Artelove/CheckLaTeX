using System.Net.Security;
using System.Text.RegularExpressions;
using TexLint.Models;
using System.Linq; 
using System.Collections.Generic; 
using System; 

namespace TexLint.TestFunctionClasses;

public class TestEnvironmentWithItemsCommand : TestFunction
{
    private const string PATTERN_END_SENTENCE = @"[.?!]";
    private const string PATTERN_UPPER_CASE = @"^[\dA-ZА-ЯЁ]+$";
    private const string PATTERN_LOWER_CASE = @"^[\da-zа-яё]+$";
    private const string PATTERN_END_OF_STRING = @"^[\s]+$";
    private const string END_SENTENCE_CHARS = ".?!";
    private readonly Regex _regexEndSentence = new Regex(PATTERN_END_SENTENCE); 
    private readonly Regex _regexUpperCase = new Regex(PATTERN_UPPER_CASE); 
    private readonly Regex _regexLowerCase = new Regex(PATTERN_LOWER_CASE); 
    private readonly Regex _regexSpace = new Regex(PATTERN_END_OF_STRING); 
    
    private PunctuationCaseWritingType _writingType = PunctuationCaseWritingType.СolonLowercaseSemicolon;
    
    public enum PunctuationCaseWritingType
    {
        СolonLowercaseSemicolon,
        DotUppercaseDot
    }

    public TestEnvironmentWithItemsCommand()
    {
        if (TestUtilities.FoundsCommands == null || !TestUtilities.FoundsCommands.Any()) return;

        var itemCommandEnvironments =
            TestUtilities.GetAllEnvironment().Where(envCmd => // Changed variable name for clarity
                envCmd != null && (
                envCmd.EnvironmentName == "enumerate" ||
                envCmd.EnvironmentName == "itemize" ||
                envCmd.EnvironmentName == "description" ||
                envCmd.EnvironmentName == "list")
                ).ToList();
       
        foreach (var environmentCommand in itemCommandEnvironments)
        {
            if (environmentCommand == null || string.IsNullOrEmpty(environmentCommand.FileOwner)) continue;

            var commandsInFile = TestUtilities.FoundsCommands
                                     .Where(command => command != null && command.FileOwner == environmentCommand.FileOwner)
                                     .OrderBy(c => c.GlobalIndex) 
                                     .ToList();
            
            Command? textBeforeEnvironment = null; 
            if (environmentCommand.GlobalIndex > 0) {
                textBeforeEnvironment = commandsInFile.LastOrDefault(c => c != null && c.GlobalIndex < environmentCommand.GlobalIndex);
            }
            
            if (textBeforeEnvironment is TextCommand textCmd && textCmd.Text != null) 
            {
                if (string.IsNullOrWhiteSpace(textCmd.Text)) 
                {
                    Errors.Add(new TestError()
                    {
                        ErrorType = ErrorType.Warning,
                        ErrorCommand = textBeforeEnvironment,
                        ErrorInfo = $"Отсутствует пунктуационный переход перед окружением \"{environmentCommand.EnvironmentName}\""
                    });
                    continue;
                }

                char chBeforeEnvironment = '\0';
                string relevantText = textCmd.Text.TrimEnd(); 
                
                if (relevantText.Length > 0) {
                    chBeforeEnvironment = relevantText[relevantText.Length -1];

                    if (END_SENTENCE_CHARS.Contains(chBeforeEnvironment))
                    {
                        _writingType = PunctuationCaseWritingType.DotUppercaseDot;
                    }
                    else if (chBeforeEnvironment == ':')
                    {
                        _writingType = PunctuationCaseWritingType.СolonLowercaseSemicolon;
                    }
                    else
                    {
                        Errors.Add(new TestError()
                        {
                            ErrorType = ErrorType.Error,
                            ErrorCommand = textBeforeEnvironment,
                            ErrorInfo = $"Ожидался символ перехода на окружение  \"{environmentCommand.EnvironmentName}\": [{END_SENTENCE_CHARS}:], однако найден символ '{chBeforeEnvironment}'."
                        });
                    }
                } else { 
                     Errors.Add(new TestError() { 
                        ErrorType = ErrorType.Warning,
                        ErrorCommand = textBeforeEnvironment,
                        ErrorInfo = $"Текст перед окружением \"{environmentCommand.EnvironmentName}\" состоит только из пробельных символов или пуст."
                     });
                     continue;
                }

                var items = TestUtilities.GetAllCommandsByNameFromList("item", environmentCommand.InnerCommands ?? new List<Command>());
                if (!items.Any()) continue; 
                
                for (int i = 0; i < items.Count; i++) 
                {
                    Command currentItem = items[i];
                    if (currentItem == null) continue;

                    Command? itemStartText = environmentCommand.InnerCommands?
                                            .Where(cmd => cmd != null && cmd.GlobalIndex > currentItem.GlobalIndex)
                                            .OrderBy(cmd => cmd.GlobalIndex)
                                            .FirstOrDefault();

                    Command? itemEndText; 
                    if (i < items.Count - 1) 
                    {
                         Command nextItem = items[i+1];
                         if (nextItem == null) continue; 
                         itemEndText = environmentCommand.InnerCommands?
                                       .Where(cmd => cmd != null && cmd.GlobalIndex < nextItem.GlobalIndex && cmd.GlobalIndex > currentItem.GlobalIndex)
                                       .OrderByDescending(cmd => cmd.GlobalIndex) 
                                       .FirstOrDefault();
                    }
                    else // Last item
                    {
                         itemEndText = environmentCommand.InnerCommands?
                                      .Where(cmd => cmd != null && cmd.GlobalIndex > currentItem.GlobalIndex && (environmentCommand.EndCommand == null || cmd.GlobalIndex < environmentCommand.EndCommand.GlobalIndex) )
                                      .OrderBy(cmd => cmd.GlobalIndex) 
                                      .LastOrDefault();
                    }
                    CheckTextWritingType(itemStartText, itemEndText, _writingType, i == items.Count - 1, environmentCommand, currentItem);
                }
            }
            else 
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Warning,
                    ErrorCommand = environmentCommand, 
                    ErrorInfo = $"Отсутствует текст или текстовый переход перед окружением \"{environmentCommand.EnvironmentName}\""
                });
            }
        }
    }

    // Changed environmentContext to be of type EnvironmentCommand to access EndCommand and InnerCommands directly
    private void CheckTextWritingType(Command? startTextCmd, Command? endTextCmd, PunctuationCaseWritingType currentWritingType, bool isLastItem, EnvironmentCommand environmentCommand, Command currentItemContext)
    {
        if (startTextCmd is TextCommand startCmd && startCmd.Text != null) 
        {
            var effectiveStartText = startCmd.Text.TrimStart();
            if (string.IsNullOrEmpty(effectiveStartText))
            {
                Errors.Add(new TestError() { ErrorCommand = currentItemContext, ErrorType = ErrorType.Warning, ErrorInfo = "Элемент списка начинается с пробельных символов или пуст." });
            }
            else
            {
                char firstChar = effectiveStartText[0];
                if (!char.IsLetter(firstChar))
                {
                    Errors.Add(new TestError() { ErrorCommand = startCmd, ErrorType = ErrorType.Warning, ErrorInfo = $"Элемент списка не начинается с буквы: '{firstChar}'." });
                }
                else
                {
                    bool isUpper = char.IsUpper(firstChar);
                    if (currentWritingType == PunctuationCaseWritingType.DotUppercaseDot && !isUpper)
                    {
                        Errors.Add(new TestError() { ErrorCommand = startCmd, ErrorType = ErrorType.Error, ErrorInfo = $"Ожидался символ в верхнем регистре в начале элемента списка, найден '{firstChar}'." });
                    }
                    else if (currentWritingType == PunctuationCaseWritingType.СolonLowercaseSemicolon && isUpper)
                    {
                        Errors.Add(new TestError() { ErrorCommand = startCmd, ErrorType = ErrorType.Error, ErrorInfo = $"Ожидался символ в нижнем регистре в начале элемента списка, найден '{firstChar}'." });
                    }
                }
            }
        }
        else
        {
            Errors.Add(new TestError() { ErrorCommand = (startTextCmd ?? currentItemContext), ErrorType = ErrorType.Warning, ErrorInfo = "Отсутствует текст после команды \\item." });
        }

        if (endTextCmd is TextCommand endCmd && endCmd.Text != null) 
        {
            var effectiveEndText = endCmd.Text.TrimEnd();
            if (!string.IsNullOrEmpty(effectiveEndText))
            {
                char lastChar = effectiveEndText[effectiveEndText.Length -1];
                bool correctEnding = false;
                if (isLastItem)
                {
                    if (END_SENTENCE_CHARS.Contains(lastChar)) correctEnding = true;
                }
                else 
                {
                    if (currentWritingType == PunctuationCaseWritingType.DotUppercaseDot && END_SENTENCE_CHARS.Contains(lastChar))
                    {
                        correctEnding = true;
                    }
                    else if (currentWritingType == PunctuationCaseWritingType.СolonLowercaseSemicolon && lastChar == ';')
                    {
                        correctEnding = true;
                    }
                }
                if (!correctEnding)
                {
                    string expected = isLastItem ? $"[{END_SENTENCE_CHARS}]" : (currentWritingType == PunctuationCaseWritingType.DotUppercaseDot ? $"[{END_SENTENCE_CHARS}]" : ";");
                    Errors.Add(new TestError { ErrorCommand = endCmd, ErrorType = ErrorType.Error, ErrorInfo = $"Некорректное окончание элемента списка. Ожидалось '{expected}', найдено '{lastChar}'." });
                }
            } else {
                 Errors.Add(new TestError() { ErrorCommand = endCmd, ErrorType = ErrorType.Warning, ErrorInfo = "Элемент списка оканчивается пробельными символами или пуст." });
            }
        } else {
            Command? errorContext = endTextCmd ?? currentItemContext;
            // No need to cast environmentCommand as it's already EnvironmentCommand type in this corrected version
            bool isMissingTerminalPunctuation = false;
            if (isLastItem) {
                if (endTextCmd == null && (environmentCommand.EndCommand == null || (currentItemContext != null && environmentCommand.EndCommand != null && currentItemContext.GlobalIndex < environmentCommand.EndCommand.GlobalIndex))) { 
                    isMissingTerminalPunctuation = true;
                }
            } else { 
                if (endTextCmd == null) isMissingTerminalPunctuation = true;
            }

            if (isMissingTerminalPunctuation) {
                    string expected = isLastItem ? $"[{END_SENTENCE_CHARS}]" : (currentWritingType == PunctuationCaseWritingType.DotUppercaseDot ? $"[{END_SENTENCE_CHARS}]" : ";");
                    // Check if the error should indeed be reported
                    if (currentItemContext != null && (!isLastItem || (environmentCommand.EndCommand != null && currentItemContext.GlobalIndex < environmentCommand.EndCommand.GlobalIndex) || environmentCommand.EndCommand == null )) {
                    Errors.Add(new TestError() { ErrorCommand = errorContext, ErrorType = ErrorType.Warning, ErrorInfo = $"Отсутствует текст с ожидаемым окончанием '{expected}' после элемента списка." });
                    }
            }
        }
    }
}
EOF_TestEnvironmentWithItemsCommand
echo "tex-lint/TestFunctionClasses/TestEnvironmentWithItemsCommand.cs replaced."

# Attempt build after fixing TestEnvironmentWithItemsCommand.cs
$HOME/.dotnet/dotnet build tex-lint/tex-lint.csproj
