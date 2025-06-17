using System.Net.Security;
using System.Text.RegularExpressions;
using TexLint.Models;
using TexLint.Models.HandleInfos;

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

    public TestEnvironmentWithItemsCommand(ILatexConfigurationService configurationService, string requestId)
        : base(configurationService, requestId)
    {
        var itemCommandEnvironment =
            GetAllEnvironment().Where(command =>
                command.EnvironmentName == "enumerate" ||
                command.EnvironmentName == "itemize" ||
                command.EnvironmentName == "description" ||
                command.EnvironmentName == "list");
       
        foreach (var environmentCommand in itemCommandEnvironment)
        {
            var textBeforeEnvironment = GetCommandByIndexInCollection(
                    environmentCommand.GlobalIndex - 1, 
                    FoundsCommands.
                        Where(command => command.FileOwner==environmentCommand.FileOwner)
                    );
            
            if (textBeforeEnvironment is TextCommand textCommand)
            {
                if (_regexSpace.Match(textCommand.Text).Success)
                {
                    AddError(ErrorType.Warning,
                        $"Отсутствует пунктуационный переход перед окружением \"{environmentCommand.EnvironmentName}\"",
                        textBeforeEnvironment,
                        "Добавьте двоеточие (:) или точку (.) перед списком");
                    
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
                    
                    AddError(ErrorType.Error,
                        $"Ожидался символ перехода на окружение \"{environmentCommand.EnvironmentName}\": [{END_SENTENCE_CHARS + ":"}], найден символ '{textCommand.Text[i]}'",
                        textBeforeEnvironment,
                        $"Замените '{textCommand.Text[i]}' на ':' или '.'");
                    
                    break;
                }

                if (chBeforeEnvironment == '\0')
                    continue;

                var items = GetAllCommandsByNameFromList("item", environmentCommand.InnerCommands);
                
                for (int i = 1; i < items.Count; i++)
                {
                    CheckTextWritingType(
                        GetCommandByIndexInCollection(
                            items[i-1].GlobalIndex + 1, 
                            environmentCommand.InnerCommands),
                        GetCommandByIndexInCollection(
                            items[i].GlobalIndex - 1, 
                            environmentCommand.InnerCommands), 
                        _writingType);
                }

                CheckLastItemWritingType(
                    GetCommandByIndexInCollection(
                        items[^1].GlobalIndex + 1,
                        environmentCommand.InnerCommands),
                    GetCommandByIndexInCollection(
                        environmentCommand.InnerCommands[^1].GlobalIndex - 1, 
                        environmentCommand.InnerCommands),
                    _writingType);
            }
            else
            {
                AddError(ErrorType.Warning,
                    $"Отсутствует текст перед окружением \"{environmentCommand.EnvironmentName}\"",
                    textBeforeEnvironment ?? environmentCommand,
                    "Добавьте вводный текст с двоеточием (:) или точкой (.) перед списком");
            }
        }
    }

    private void CheckLastItemWritingType(Command startText, Command endText, PunctuationCaseWritingType punctuationCaseWritingType)
    {
        if (startText is TextCommand startCommand)
        {
            if (_regexSpace.Match(startCommand.Text).Success)
            {
                AddError(ErrorType.Warning,
                    "Ожидался текст после команды \\item",
                    startText,
                    "Добавьте содержимое элемента списка");
                
                return;
            }

            for (var i = 0; i < startCommand.Text.Length; i++)
            {
                var t = startCommand.Text[i].ToString();
                
                if (_regexSpace.Match(t).Success == false &&
                    _regexUpperCase.Match(t).Success == false &&
                    _regexLowerCase.Match(t).Success == false)
                {
                    AddError(ErrorType.Warning,
                        $"Ожидался алфавитный символ в начале текста, найден '{startCommand.Text[i]}'",
                        startText,
                        "Начните текст с буквы");
                    break;
                }

                if (punctuationCaseWritingType == PunctuationCaseWritingType.DotUppercaseDot)
                {
                    if (_regexUpperCase.Match(t).Success == false)
                    {
                        AddError(ErrorType.Error,
                            $"Ожидался символ в верхнем регистре в начале текста списка, найден \"{startCommand.Text[i]}\"",
                            startCommand,
                            $"Измените \"{startCommand.Text[i]}\" на \"{startCommand.Text[i].ToString().ToUpper()}\"");
                    }
                    
                    break;
                }

                if (punctuationCaseWritingType == PunctuationCaseWritingType.СolonLowercaseSemicolon)
                {
                    if (_regexLowerCase.Match(t).Success == false)
                    {
                        AddError(ErrorType.Error,
                            $"Ожидался символ в нижнем регистре в начале текста списка, найден \"{startCommand.Text[i]}\"",
                            startCommand,
                            $"Измените \"{startCommand.Text[i]}\" на \"{startCommand.Text[i].ToString().ToLower()}\"");
                    }
                    
                    break;
                }
            }
        }
        else
        {
            AddError(ErrorType.Warning,
                "Ожидался текст после команды \\item",
                startText,
                "Добавьте содержимое элемента списка");
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
                        AddError(ErrorType.Warning,
                            $"Ожидался символ окончания последнего элемента списка: [{END_SENTENCE_CHARS}], найден '{endCommand.Text[i]}'",
                            endText,
                            $"Замените '{endCommand.Text[i]}' на '.'");
                        break;
                    }

                    if (punctuationCaseWritingType == PunctuationCaseWritingType.DotUppercaseDot
                        || punctuationCaseWritingType == PunctuationCaseWritingType.СolonLowercaseSemicolon)
                    {
                        if (_regexEndSentence.Match(endCommand.Text[i].ToString()).Success == false)
                        {
                            AddError(ErrorType.Error,
                                $"Ожидался символ окончания последнего элемента списка: [{END_SENTENCE_CHARS}], найден '{endCommand.Text[i]}'",
                                endText,
                                $"Замените '{endCommand.Text[i]}' на '.'");
                        }
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
                AddError(ErrorType.Warning,
                    "Ожидался текст после команды \\item",
                    startText,
                    "Добавьте содержимое элемента списка");
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
                            AddError(ErrorType.Warning,
                                $"Ожидался алфавитный символ в начале текста, найден '{startCommand.Text[i]}'",
                                startText,
                                "Начните текст с буквы");
                            break;
                        }
                    }

                    if (punctuationCaseWritingType == PunctuationCaseWritingType.DotUppercaseDot)
                    {
                        if (_regexUpperCase.Match(t).Success == false)
                        {
                            AddError(ErrorType.Error,
                                $"Ожидался символ в верхнем регистре в начале текста списка, найден \"{startCommand.Text[i]}\"",
                                startCommand,
                                $"Измените \"{startCommand.Text[i]}\" на \"{startCommand.Text[i].ToString().ToUpper()}\"");
                        }
                        break;
                    }

                    if (punctuationCaseWritingType == PunctuationCaseWritingType.СolonLowercaseSemicolon)
                    {
                        if (_regexLowerCase.Match(t).Success == false)
                        {
                            AddError(ErrorType.Error,
                                $"Ожидался символ в нижнем регистре в начале текста списка, найден \"{startCommand.Text[i]}\"",
                                startCommand,
                                $"Измените \"{startCommand.Text[i]}\" на \"{startCommand.Text[i].ToString().ToLower()}\"");
                        }
                        break;
                    }
                }
            }
        }
        else
        {
            AddError(ErrorType.Warning,
                "Ожидался текст после команды \\item",
                startText,
                "Добавьте содержимое элемента списка");
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
                            AddError(ErrorType.Warning,
                                $"Ожидался символ окончания элемента списка: [{END_SENTENCE_CHARS};], найден '{endCommand.Text[i]}'",
                                endText,
                                $"Замените '{endCommand.Text[i]}' на ';' или '.'");
                            break;
                        }
                    }

                    if (punctuationCaseWritingType == PunctuationCaseWritingType.DotUppercaseDot)
                    {
                        if (_regexEndSentence.Match(endCommand.Text[i].ToString()).Success == false)
                        {
                            AddError(ErrorType.Error,
                                $"Ожидался символ окончания элемента списка: [{END_SENTENCE_CHARS}], найден '{endCommand.Text[i]}'",
                                endText,
                                $"Замените '{endCommand.Text[i]}' на '.'");
                        }
                        break;
                    }

                    if (punctuationCaseWritingType == PunctuationCaseWritingType.СolonLowercaseSemicolon)
                    {
                        if (endCommand.Text[i].ToString() != ";")
                        {
                            AddError(ErrorType.Error,
                                $"Ожидался символ окончания элемента списка: [;], найден '{endCommand.Text[i]}'",
                                endText,
                                $"Замените '{endCommand.Text[i]}' на ';'");
                        }
                        break;
                    }
                }
            }
        }
    }
}