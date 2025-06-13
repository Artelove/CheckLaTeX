using System.Text.Json;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

public class TestQuotationMarks : TestFunction
{
    private readonly char[] _invalidQuotes;
    public TestQuotationMarks()
    {
        var commands = JsonSerializer.Deserialize<List<ParseInfo>>(new StreamReader(TestUtilities.PathToCommandsJson).ReadToEnd());
        var environments = JsonSerializer.Deserialize<List<ParseInfo>>(new StreamReader(TestUtilities.PathToEnvironmentJson).ReadToEnd());
        var rules = JsonSerializer.Deserialize<LintRules>(new StreamReader(TestUtilities.PathToLintRulesJson).ReadToEnd());
        _invalidQuotes = rules?.QuotationMarks.Forbidden.Select(s => s[0]).ToArray() ?? Array.Empty<char>();

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
            foreach (var count in command.Arguments.Select(argument => FindMistakeQuatationMarksInText(argument.Text ?? string.Empty) +
                                                                       FindMistakeQuatationMarksInText(argument.Value ?? string.Empty)))
            {
                for (var i = 0; i < count; i++)
                {
                    Errors.Add(new TestError()
                    {
                        ErrorCommand = command,
                        ErrorType = ErrorType.Warning,
                        ErrorInfo = "$Обнаружено использование иных кавычек. Рекомендуется замена на << >> (елочки) в аргументе команды:\n" +
                                    $"{command}"
                    });
                }
            }
            foreach (var count in command.Parameters.Select(parameter => FindMistakeQuatationMarksInText(parameter.Text ?? string.Empty) +
                                                                         FindMistakeQuatationMarksInText(parameter.Value ?? string.Empty)))
            {
                for (var i = 0; i < count; i++)
                {
                    Errors.Add(new TestError()
                    {
                        ErrorCommand = command,
                        ErrorType = ErrorType.Warning,
                        ErrorInfo = "$Обнаружено использование иных кавычек. Рекомендуется замена на << >> (елочки) в параметре команды:\n" +
                                    $"{command}"
                    });
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
            var number = FindMistakeQuatationMarksInText((textCommand as TextCommand)?.Text ?? string.Empty);
            if(number!=0)
            {
                Errors.Add(new TestError()
                    {
                        ErrorCommand = textCommand,
                        ErrorType = ErrorType.Warning,
                        ErrorInfo =
                            $"Обнаружено использование иных кавычек. Рекомендуется замена на << >> (елочки) в тексте:\n" +
                            $"{TestUtilities.GetContentAreaFromFindSymbol(textCommand, number)}"
                    });
            }
        }
    }

    private int FindMistakeQuatationMarksInText(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            foreach (var ch in _invalidQuotes)
            {
                if (text[i] == ch)
                    return i;
            }
        }

        return 0;
    }
}
