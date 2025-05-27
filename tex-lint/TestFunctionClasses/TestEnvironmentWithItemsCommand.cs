using System.Net.Security;
using System.Text.RegularExpressions;
using TexLint.Models;

namespace TexLint.TestFunctionClasses;

public class TestEnvironmentWithItemsCommand : TestFunction
{
    private const string PATTERN_END_SENTENCE = @"[.?!]";
    private const string PATTERN_UPPER_CASE = @"^[\dA-ZА-ЯЁ]+$";
    private const string PATTERN_LOWER_CASE = @"^[\da-zа-яё]+$";
    private const string PATTERN_END_OF_STRING = @"^[\s]+$";
    private const string END_SENTENCE_CHARS = ".?!";
    private readonly Regex _regexEndSentence = new(PATTERN_END_SENTENCE);
    private readonly Regex _regexUpperCase = new(PATTERN_UPPER_CASE);
    private readonly Regex _regexLowerCase = new(PATTERN_LOWER_CASE);
    private readonly Regex _regexSpace = new(PATTERN_END_OF_STRING);
    
    private PunctuationCaseWritingType _writingType = PunctuationCaseWritingType.СolonLowercaseSemicolon;
    
    public enum PunctuationCaseWritingType
    {
        СolonLowercaseSemicolon,
        DotUppercaseDot
    }

    public TestEnvironmentWithItemsCommand()
    {
        var itemCommandEnvironment =
            TestUtilities.GetAllEnvironment().Where(command =>
                command.EnvironmentName == "enumerate" ||
                command.EnvironmentName == "itemize" ||
                command.EnvironmentName == "description" ||
                command.EnvironmentName == "list");
       
        foreach (var environmentCommand in itemCommandEnvironment)
        {
            var textBeforeEnvironment = TestUtilities.GetCommandByIndexInCollection(
                    environmentCommand.GlobalIndex - 1, 
                    TestUtilities.FoundsCommands.
                        Where(command => command.FileOwner==environmentCommand.FileOwner)
                    );
            
            if (textBeforeEnvironment is TextCommand textCommand)
            {
                if (_regexSpace.Match(textCommand.Text).Success)
                {
                    Errors.Add(new TestError()
                    {
                        ErrorType = ErrorType.Warning,
                        ErrorCommand = textBeforeEnvironment,
                        ErrorInfo =
                            $"Отсутствует пунктуационный переход перед окружением \"список\" или \"перечисление\""
                    });
                    
                    continue;
                }

                var chBeforeEnvironment = '\0';
                
                for (int i = textCommand.Text.Length - 1; i >= 0; i--)
                {
                    if (_regexSpace.Match(textCommand.Text[i].ToString()).Success == false)
                    {
                        chBeforeEnvironment = textCommand.Text[i];
                    }
                    else
                    {
                        continue;
                    }
                    
                    if (END_SENTENCE_CHARS.Contains(chBeforeEnvironment.ToString()))
                    {
                        _writingType = PunctuationCaseWritingType.DotUppercaseDot;
                        break;
                    }
                    
                    if (chBeforeEnvironment == ':')
                    {
                        _writingType = PunctuationCaseWritingType.СolonLowercaseSemicolon;
                        break;
                    }
                    
                    Errors.Add(new TestError()
                    {
                        ErrorType = ErrorType.Error,
                        ErrorCommand = textBeforeEnvironment,
                        ErrorInfo =
                            $"Ожидался символ перехода на окружение  \"список\" или \"перечисление\": [{END_SENTENCE_CHARS + ":"}]," +
                            $" однако найден символ {textCommand.Text[i]}."
                    });
                    
                    break;
                }

                if (chBeforeEnvironment == '\0')
                    continue;

                var items = TestUtilities.GetAllCommandsByNameFromList("item", environmentCommand.InnerCommands);
                
                for (int i = 1; i < items.Count; i++)
                {
                    CheckTextWritingType(
                        TestUtilities.GetCommandByIndexInCollection(
                            items[i-1].GlobalIndex + 1, 
                            environmentCommand.InnerCommands),
                        TestUtilities.GetCommandByIndexInCollection(
                            items[i].GlobalIndex - 1, 
                            environmentCommand.InnerCommands), 
                        _writingType);
                }

                CheckLastItemWritingType(
                    TestUtilities.GetCommandByIndexInCollection(
                        items[^1].GlobalIndex + 1,
                        environmentCommand.InnerCommands),
                    TestUtilities.GetCommandByIndexInCollection(
                        environmentCommand.InnerCommands[^1].GlobalIndex - 1, 
                        environmentCommand.InnerCommands),
                    _writingType);
            }
            else
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Warning,
                    ErrorCommand = textBeforeEnvironment,
                    ErrorInfo = $"Отсутствует текст перед окружением \"список\" или \"перечисление\""
                });
            }
        }
    }

    private void CheckLastItemWritingType(Command startText, Command endText, PunctuationCaseWritingType punctuationCaseWritingType)
    {
        if (startText is TextCommand startCommand)
        {
            if (_regexSpace.Match(startCommand.Text).Success)
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Warning,
                    ErrorCommand = startText,
                    ErrorInfo = $"Ожидался текст после команды \\item"
                });
                
                return;
            }

            for (var i = 0; i < startCommand.Text.Length; i++)
            {
                var t = startCommand.Text[i].ToString();
                
                if (_regexSpace.Match(t).Success == false &&
                    _regexUpperCase.Match(t).Success == false &&
                    _regexLowerCase.Match(t).Success == false)
                {
                    {
                        Errors.Add(new TestError()
                        {
                            ErrorType = ErrorType.Warning,
                            ErrorCommand = startText,
                            ErrorInfo = $"Ожидался алфавитный символ в начале текста, найден" +
                                        $" {startCommand.Text[i]}"
                        });
                        break;
                    }
                }

                if (punctuationCaseWritingType == PunctuationCaseWritingType.DotUppercaseDot)
                {
                    if (_regexUpperCase.Match(t).Success == false)
                        Errors.Add(new TestError()
                        {
                            ErrorType = ErrorType.Error,
                            ErrorCommand = startCommand,
                            ErrorInfo = $"Ожидался символ в верхнем регистре в начале текста списка, найден" +
                                        $" \"{startCommand.Text[i]}\""
                        });
                    
                    break;
                }

                if (punctuationCaseWritingType == PunctuationCaseWritingType.СolonLowercaseSemicolon)
                {
                    if (_regexLowerCase.Match(t).Success == false)
                        Errors.Add(new TestError()
                        {
                            ErrorType = ErrorType.Error,
                            ErrorCommand = startCommand,
                            ErrorInfo = $"Ожидался символ в нижнем регистре в начале текста списка, найден" +
                                        $" \"{startCommand.Text[i]}\""
                        });
                    
                    break;
                }
            }
        }
        else
        {
            Errors.Add(new TestError()
            {
                ErrorType = ErrorType.Warning,
                ErrorCommand = startText,
                ErrorInfo = $"Ожидался текст после команды \\item"
            });
            return;
        }
        
        if (endText is TextCommand endCommand)
        {
            for (int i = endCommand.Text.Length - 1; i >= 0; i--)
            {
                if (_regexSpace.Match(endCommand.Text[i].ToString()).Success == false)
                {
                    if (_regexEndSentence.Match(endCommand.Text[i].ToString()).Success == false)
                    {
                        Errors.Add(new TestError()
                            {
                                ErrorType = ErrorType.Warning,
                                ErrorCommand = endText,
                                ErrorInfo =
                                    $"Ожидался символ окончания последнего элемента листа: [{END_SENTENCE_CHARS}]," +
                                    $" найден" + $" {endCommand.Text[i]}"
                            });
                            break;
                    }

                    if (punctuationCaseWritingType == PunctuationCaseWritingType.DotUppercaseDot
                        || punctuationCaseWritingType == PunctuationCaseWritingType.СolonLowercaseSemicolon)
                    {
                        if (_regexEndSentence.Match(endCommand.Text[i].ToString()).Success == false)
                            Errors.Add(new TestError()
                            {
                                ErrorType = ErrorType.Error,
                                ErrorCommand = endText,
                                ErrorInfo =
                                    $"Ожидался символ окончания последнего элемента листа:[{END_SENTENCE_CHARS}]," +
                                    $" найден {endCommand.Text[i]}"
                            });
                        break;
                    }
                }
            }
        }
    }

    private void CheckTextWritingType(Command startText, Command endText,
        PunctuationCaseWritingType punctuationCaseWritingType)
    {
        if (startText is TextCommand startCommand)
        {
            if (_regexSpace.Match(startCommand.Text).Success)
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Warning,
                    ErrorCommand = startText,
                    ErrorInfo = $"Ожидался текст после команды \\item"
                });
                return;
            }

            for (int i = 0; i < startCommand.Text.Length; i++)
            {
                var t = startCommand.Text[i].ToString();
                
                if (_regexSpace.Match(t).Success == false)
                {
                    if (_regexUpperCase.Match(t).Success == false)
                    {
                        if (_regexLowerCase.Match(t).Success == false)
                        {
                            Errors.Add(new TestError()
                            {
                                ErrorType = ErrorType.Warning,
                                ErrorCommand = startText,
                                ErrorInfo = $"Ожидался алфавитный символ в начале текста, найден" +
                                            $" {startCommand.Text[i]}"
                            });
                            break;
                        }
                    }

                    if (punctuationCaseWritingType == PunctuationCaseWritingType.DotUppercaseDot)
                    {
                        if (_regexUpperCase.Match(t).Success == false)
                            Errors.Add(new TestError()
                            {
                                ErrorType = ErrorType.Error,
                                ErrorCommand = startCommand,
                                ErrorInfo = $"Ожидался символ в верхнем регистре в начале текста списка, найден" +
                                            $" \"{startCommand.Text[i]}\""
                            });
                        break;
                    }

                    if (punctuationCaseWritingType == PunctuationCaseWritingType.СolonLowercaseSemicolon)
                    {
                        if (_regexLowerCase.Match(t).Success == false)
                            Errors.Add(new TestError()
                            {
                                ErrorType = ErrorType.Error,
                                ErrorCommand = startCommand,
                                ErrorInfo = $"Ожидался символ в нижнем регистре в начале текста списка, найден" +
                                            $" \"{startCommand.Text[i]}\""
                            });
                        break;
                    }
                }
            }
        }
        else
        {
            Errors.Add(new TestError()
            {
                ErrorType = ErrorType.Warning,
                ErrorCommand = startText,
                ErrorInfo = $"Ожидался текст после команды \\item"
            });
            return;
        }
        if (endText is TextCommand endCommand)
        {
            for (int i = endCommand.Text.Length - 1; i >= 0; i--)
            {
                if (_regexSpace.Match(endCommand.Text[i].ToString()).Success == false)
                {
                    if (_regexEndSentence.Match(endCommand.Text[i].ToString()).Success == false)
                    {
                        if (endCommand.Text[i].ToString() != ";")
                        {
                            Errors.Add(new TestError()
                            {
                                ErrorType = ErrorType.Warning,
                                ErrorCommand = endText,
                                ErrorInfo =
                                    $"Ожидался символ окончания элемента листа: [{END_SENTENCE_CHARS + ":"}]," +
                                    $" найден" + $" {endCommand.Text[i]}"
                            });
                            break;
                        }
                    }

                    if (punctuationCaseWritingType == PunctuationCaseWritingType.DotUppercaseDot)
                    {
                        if (_regexEndSentence.Match(endCommand.Text[i].ToString()).Success == false)
                            Errors.Add(new TestError()
                            {
                                ErrorType = ErrorType.Error,
                                ErrorCommand = endText,
                                ErrorInfo =
                                    $"Ожидался символ окончания элемента листа: [{END_SENTENCE_CHARS}]," +
                                    $" найден {endCommand.Text[i]}"
                            });
                        break;
                    }

                    if (punctuationCaseWritingType == PunctuationCaseWritingType.СolonLowercaseSemicolon)
                    {
                        if (endCommand.Text[i].ToString() != ";")
                            Errors.Add(new TestError()
                            {
                                ErrorType = ErrorType.Error,
                                ErrorCommand = endText,
                                ErrorInfo = $"Ожидался символ окончания элемента листа: [;]," +
                                            $" найден {endCommand.Text[i]}"
                            });
                        break;
                    }
                }
            }
        }
    }
}