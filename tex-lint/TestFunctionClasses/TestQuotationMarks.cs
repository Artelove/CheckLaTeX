using System.Text.Json;
using TexLint.Models;
using TexLint.Models.HandleInfos;
using System.IO; 
using System.Collections.Generic; 
using System.Linq; 

namespace TexLint.TestFunctionClasses;

public class TestQuotationMarks : TestFunction
{
    public TestQuotationMarks()
    {
        var commands = new List<ParseInfo>();
        // var environments = new List<ParseInfo>(); // Unused
        try {
            if(File.Exists(TestUtilities.PathToCommandsJson)){
                commands = JsonSerializer.Deserialize<List<ParseInfo>>(File.ReadAllText(TestUtilities.PathToCommandsJson)) ?? new List<ParseInfo>();
            }
            // if(File.Exists(TestUtilities.PathToEnvironmentJson)){ // Unused
            //     environments = JsonSerializer.Deserialize<List<ParseInfo>>(File.ReadAllText(TestUtilities.PathToEnvironmentJson)) ?? new List<ParseInfo>();
            // }
        } catch (JsonException ex) {
            // ErrorType.Critical does not exist, using ErrorType.Error
            Errors.Add(new TestError{ ErrorInfo = "JSON parsing error in TestQuotationMarks: " + ex.Message, ErrorType = ErrorType.Error});
            return; 
        }
        catch (IOException ex) {
            Errors.Add(new TestError{ ErrorInfo = "File IO error in TestQuotationMarks: " + ex.Message, ErrorType = ErrorType.Error});
            return;
        }

        List<string> commandsNamesWherePhraseArgOrParam = new List<string>(); 
        foreach (var commandInfo in commands) 
        {
            if (commandInfo != null && 
                (commandInfo.Arg.ParseType == ParameterParseType.Phrase || commandInfo.Param.ParseType == ParameterParseType.Phrase))
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
                    var (isMistakeText, idxText) = FindMistakeQuatationMarksInTextWithIndex(arg.Text ?? string.Empty);
                    if(isMistakeText) Errors.Add(CreateQuotationError(command, "аргументе (имя)"));
                    var (isMistakeValue, idxValue) = FindMistakeQuatationMarksInTextWithIndex(arg.Value ?? string.Empty);
                    if(isMistakeValue) Errors.Add(CreateQuotationError(command, "аргументе (значение)"));
                }
            }
            if (command.Parameters != null) {
                 foreach(var param in command.Parameters) {
                    if (param == null) continue;
                    var (isMistakeText, idxText) = FindMistakeQuatationMarksInTextWithIndex(param.Text ?? string.Empty);
                    if(isMistakeText) Errors.Add(CreateQuotationError(command, "параметре (имя)"));
                    var (isMistakeValue, idxValue) = FindMistakeQuatationMarksInTextWithIndex(param.Value ?? string.Empty);
                    if(isMistakeValue) Errors.Add(CreateQuotationError(command, "параметре (значение)"));
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

        foreach (var textCmd in textCommandsToProcess) 
        {
            if (textCmd == null || textCmd.Text == null) continue; 
            var (mistakeFound, mistakeIndex) = FindMistakeQuatationMarksInTextWithIndex(textCmd.Text);
            if(mistakeFound)
            {
                Errors.Add(new TestError()
                    {
                        ErrorCommand = textCmd,
                        ErrorType = ErrorType.Warning,
                        ErrorInfo =
                            $"Обнаружено использование иных кавычек. Рекомендуется замена на << >> (елочки) в тексте:\\n" + 
                            $"{TestUtilities.GetContentAreaFromFindSymbol(textCmd, mistakeIndex)}"
                    });
            }
        }
    }
    
    private TestError CreateQuotationError(Command command, string context) {
        return new TestError() {
            ErrorCommand = command,
            ErrorType = ErrorType.Warning,
            ErrorInfo = $"Обнаружено использование иных кавычек. Рекомендуется замена на << >> (елочки) в {context} команды:\\n{command}"
        };
    }

    private (bool, int) FindMistakeQuatationMarksInTextWithIndex(string text)
    {
        if (string.IsNullOrEmpty(text)) return (false, -1);
        for (var i = 0; i < text.Length; i++)
        {
            // Corrected char literal for single quote
            if ((text[i] == '\"' || text[i] == '\'')) 
            {
                // Basic LaTeX-aware skipping for `` and '' pairs
                if (text[i] == '\'') { // If current is single quote
                    if (i > 0 && text[i-1] == '\'') continue; // Part of '' (LaTeX right quote)
                    if (i + 1 < text.Length && text[i+1] == '\'') continue; // Part of '' (LaTeX right quote)
                    if (i > 0 && text[i-1] == '`') continue; // Part of `' (LaTeX apostrophe)
                }
                if (text[i] == '`') { // If current is backtick (often part of LaTeX left quote)
                     if (i > 0 && text[i-1] == '`') continue; // Part of `` (LaTeX left quote)
                     if (i + 1 < text.Length && text[i+1] == '`') continue; // Part of `` (LaTeX left quote)
                }
                return (true, i);
            }
        }
        return (false, -1);
    }
}
