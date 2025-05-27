using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

public class TestHyphenInsteadOfDash : TestFunction
{
  public TestHyphenInsteadOfDash()
    {
        var commands = JsonSerializer.Deserialize<List<ParseInfo>>(new StreamReader(TestUtilities.PathToCommandsJson).ReadToEnd());
        var environments = JsonSerializer.Deserialize<List<ParseInfo>>(new StreamReader(TestUtilities.PathToEnvironmentJson).ReadToEnd());

        List<string> commandsNamesWherePhraseArgOrParam = new();
        foreach (var command in commands)
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
            foreach (var count in command.Arguments.Select(argument => FindMistakeHyphenInText(argument.Text ?? string.Empty) +
                                                                       FindMistakeHyphenInText(argument.Value ?? string.Empty)))
            {
                for (var i = 0; i < count; i++)
                {
                    Errors.Add(new TestError()
                    {
                        ErrorCommand = command,
                        ErrorType = ErrorType.Warning,
                        ErrorInfo = $"Обнаружено использования дефиса вместо тиреобразного символа в аргументе команды:\n" +
                                    $"{command}"
                    });
                }
            }

            foreach (var count in command.Parameters.Select(parameter => FindMistakeHyphenInText(parameter?.Text ?? string.Empty) +
                                                                         FindMistakeHyphenInText(parameter?.Value ?? string.Empty)))
            {
                for (var i = 0; i < count; i++)
                {
                    Errors.Add(new TestError()
                    {
                        ErrorCommand = command,
                        ErrorType = ErrorType.Warning,
                        ErrorInfo = $"Обнаружено использования дефиса вместо тиреобразного символа в параметре команды:\n" +
                                    $"{command}"
                    });
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
            var number = FindMistakeHyphenInText(textCommand?.Text ?? string.Empty);
            if (number != 0)
            {
                Errors.Add(new TestError()
                    {
                        ErrorCommand = textCommand,
                        ErrorType = ErrorType.Warning,
                        ErrorInfo = $"Обнаружено использования дефиса вместо тиреобразного символа в тексте:\n" +
                                    $"{TestUtilities.GetContentAreaFromFindSymbol(textCommand, number)}"
                    });
            }
        }
    }

    private int FindMistakeHyphenInText(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '-')
                continue;
            
            bool left = true;
            bool right = true;
            
            if(i - 1 >= 0)
                left = Char.IsWhiteSpace(text[i - 1]);
            
            if (i + 1 < text.Length)
                right = Char.IsWhiteSpace(text[i + 1]);
            
            if(left && right)
                return i;
        }

        return 0;
    }
}