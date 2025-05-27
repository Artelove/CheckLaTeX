using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TexLint.Models;
using TexLint.Models.HandleInfos;
using System.IO; 
using System.Collections.Generic; 
using System.Linq; 

namespace TexLint.TestFunctionClasses;

public class TestHyphenInsteadOfDash : TestFunction
{
  public TestHyphenInsteadOfDash()
    {
        var commands = new List<ParseInfo>();
        try {
            if(File.Exists(TestUtilities.PathToCommandsJson)) {
                 commands = JsonSerializer.Deserialize<List<ParseInfo>>(File.ReadAllText(TestUtilities.PathToCommandsJson)) ?? new List<ParseInfo>();
            }
        } catch (JsonException ex) {
            Errors.Add(new TestError{ ErrorInfo = "JSON parsing error in TestHyphen: " + ex.Message, ErrorType = ErrorType.Error});
            return; 
        }
        catch (IOException ex) {
            Errors.Add(new TestError{ ErrorInfo = "File IO error in TestHyphen: " + ex.Message, ErrorType = ErrorType.Error});
            return;
        }

        List<string> commandsNamesWherePhraseArgOrParam = new List<string>(); 
        foreach (var commandInfo in commands) 
        {
            if (commandInfo != null && 
                (commandInfo.Arg.ParseType == ParameterParseType.Phrase || 
                 commandInfo.Param.ParseType == ParameterParseType.Phrase))
            {
                if (commandInfo.Name != null) commandsNamesWherePhraseArgOrParam.Add(commandInfo.Name);
            }
        }

        var commandsWherePhraseArgOrParam = new List<Command>();
        
        if (TestUtilities.FoundsCommandsWithLstlisting != null) { 
            foreach (var command in TestUtilities.FoundsCommandsWithLstlisting)
            {
                if (command != null && command.Name != null && commandsNamesWherePhraseArgOrParam.Contains(command.Name)) 
                    commandsWherePhraseArgOrParam.Add(command);
            }
        }

        foreach (var command in commandsWherePhraseArgOrParam)
        {
            if (command == null) continue;
            if (command.Arguments != null) {
                foreach(var arg in command.Arguments) {
                    if (arg == null) continue;
                    var (isMistakeText, idxText) = FindMistakeHyphenInTextWithIndex(arg.Text ?? string.Empty);
                    if(isMistakeText) Errors.Add(CreateHyphenError(command, "аргументе (имя)"));
                    var (isMistakeValue, idxValue) = FindMistakeHyphenInTextWithIndex(arg.Value ?? string.Empty);
                    if(isMistakeValue) Errors.Add(CreateHyphenError(command, "аргументе (значение)"));
                }
            }
            if (command.Parameters != null) {
                 foreach(var param in command.Parameters) {
                    if (param == null) continue;
                    var (isMistakeText, idxText) = FindMistakeHyphenInTextWithIndex(param.Text ?? string.Empty);
                    if(isMistakeText) Errors.Add(CreateHyphenError(command, "параметре (имя)"));
                    var (isMistakeValue, idxValue) = FindMistakeHyphenInTextWithIndex(param.Value ?? string.Empty);
                    if(isMistakeValue) Errors.Add(CreateHyphenError(command, "параметре (значение)"));
                }
            }
        }
        
        var allTextCommands = TestUtilities.GetAllCommandsByName(TextCommand.TEXT_COMMAND_NAME) ?? new List<Command>();
        var textCommandsToProcess = new List<TextCommand>(allTextCommands.OfType<TextCommand>());
        
        var lstlistingEnvs = TestUtilities.GetAllEnvironment()?.Where(e => e != null && e.EnvironmentName == "lstlisting") ?? Enumerable.Empty<EnvironmentCommand>();
        var textInLstlisting = new HashSet<TextCommand>();
        foreach (var env in lstlistingEnvs) {
            if (env.InnerCommands == null) continue;
            foreach (var innerCmd in env.InnerCommands.OfType<TextCommand>()) {
                if (innerCmd != null) textInLstlisting.Add(innerCmd);
            }
        }
        textCommandsToProcess.RemoveAll(tc => tc != null && textInLstlisting.Contains(tc));
        
        var ignoreEnvironmentNames = new HashSet<string> { "equation", "comment" };
        var textInIgnoredEnvs = new HashSet<TextCommand>();
        var ignoredEnvs = TestUtilities.GetAllEnvironment()?.Where(e => e != null && ignoreEnvironmentNames.Contains(e.EnvironmentName)) ?? Enumerable.Empty<EnvironmentCommand>();
        foreach (var env in ignoredEnvs) {
            CollectTextCommandsRecursive(env, textInIgnoredEnvs);
        }
        textCommandsToProcess.RemoveAll(tc => tc != null && textInIgnoredEnvs.Contains(tc));

        foreach (var textCmd in textCommandsToProcess) 
        {
            if (textCmd == null || textCmd.Text == null) continue; 
            var (isMistake, mistakeIndex) = FindMistakeHyphenInTextWithIndex(textCmd.Text); 
            if (isMistake)
            {
                Errors.Add(new TestError()
                    {
                        ErrorCommand = textCmd,
                        ErrorType = ErrorType.Warning,
                        ErrorInfo = $"Обнаружено использования дефиса вместо тиреобразного символа в тексте:\n" + 
                                    $"{TestUtilities.GetContentAreaFromFindSymbol(textCmd, mistakeIndex)}" 
                    });
            }
        }
    }

    private void CollectTextCommandsRecursive(EnvironmentCommand env, HashSet<TextCommand> collection)
    {
        if (env == null || env.InnerCommands == null) return;
        foreach(var innerCmd in env.InnerCommands)
        {
            if (innerCmd is TextCommand tc && tc != null) collection.Add(tc);
            else if (innerCmd is EnvironmentCommand innerEnv && innerEnv != null) CollectTextCommandsRecursive(innerEnv, collection);
        }
    }

    private TestError CreateHyphenError(Command command, string context) {
        return new TestError() {
            ErrorCommand = command,
            ErrorType = ErrorType.Warning,
            ErrorInfo = $"Обнаружено использования дефиса вместо тиреобразного символа в {context} команды:\n{command}"
        };
    }

    private (bool, int) FindMistakeHyphenInTextWithIndex(string text)
    {
        if (string.IsNullOrEmpty(text)) return (false, -1);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '-')
                continue;
            
            bool leftIsSpaceOrStart = (i == 0) || Char.IsWhiteSpace(text[i - 1]);
            bool rightIsSpaceOrEnd = (i == text.Length - 1) || Char.IsWhiteSpace(text[i + 1]);
            
            if(leftIsSpaceOrStart && rightIsSpaceOrEnd && !(text.Length == 1 && i == 0)) 
                return (true, i); 
        }
        return (false, -1);
    }
}
