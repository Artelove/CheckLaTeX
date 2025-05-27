using System.Net.Mime;
using System.Text.RegularExpressions;
using TexLint.Models.HandleInfos;
using TexLint.TestFunctionClasses;
using System.IO; // Added for StreamReader, Path
using System.Collections.Generic; // Added for List, Dictionary

namespace TexLint.Models
{
    class CommandHandler
    {
        private const string PATTERN_COMMAND_NAME = @"^[a-zA-Z]+$";
        private const string PATTERN_PARAMETER = @"^[а-яА-ЯёЁa-zA-Z0-9!?.:_-]+$";
        private const string PATTERN_WHITE_SPACE = @"^\s+$";
        private const string PATTERN_END_OF_STRING = @"\s";
    
        private readonly Regex _regexCommandName;
        private readonly Regex _regexParam;
        private readonly Regex _regexSpace;
        private readonly Regex _regexEndString;
        private readonly HandleInfo _handleInfo;

        private int _stringNumber;
        private Dictionary<int,int> nextStrChar = new Dictionary<int,int>(); // Corrected

        public string StartFile;
        public string StartDirectory;

        public CommandHandler(string startFile, string startDirectory)
        {
            _regexCommandName = new Regex(PATTERN_COMMAND_NAME);
            _regexParam = new Regex(PATTERN_PARAMETER);
            _regexSpace = new Regex(PATTERN_WHITE_SPACE);
            _regexEndString = new Regex(PATTERN_END_OF_STRING);
            StartFile = startFile;
            StartDirectory = startDirectory;
            _handleInfo = new HandleInfo();
        }

        public List<Command> FindAllCommandsInDocument()
        {
            // Use Path.Combine for cross-platform compatibility
            string fullPath = Path.Combine(StartDirectory, StartFile);
            var foundCommands = FindAllCommandsInFile(fullPath);
        
            for (int i = 0; i < foundCommands.Count; i++)
            {
                if (foundCommands[i].Name == "input" || foundCommands[i].Name == "include")
                {
                    // Ensure Arguments is not null and has elements
                    if (foundCommands[i].Arguments != null && foundCommands[i].Arguments.Count > 0)
                    {
                        string argumentValue = foundCommands[i].Arguments[0].Value;
                        if (argumentValue != null && !new Regex(".tex|.bib", RegexOptions.IgnoreCase).Match(argumentValue).Success)
                        {
                            foundCommands[i].Arguments[0].Value += ".tex";
                        }
                        // Use Path.Combine here as well
                        string includedFilePath = Path.Combine(StartDirectory, foundCommands[i].Arguments[0].Value);
                        foundCommands = InsertListAtIndex(
                            foundCommands,
                            FindAllCommandsInFile(includedFilePath),
                            i,
                            false);
                    }
                }
            }
            return foundCommands;
        }

        private List<Command> InsertListAtIndex(
            List<Command> firstList, 
            List<Command> secondList, 
            int index, 
            bool includeCommandByIndex = true)
        {
            var resultList = new List<Command>();
            resultList.AddRange(firstList.GetRange(0, index));
            resultList.AddRange(secondList);

            int remainingCount = firstList.Count - index;
            if (includeCommandByIndex)
            {
                if (remainingCount > 0)
                    resultList.AddRange(firstList.GetRange(index, remainingCount));
            }
            else
            {
                remainingCount--; 
                if (remainingCount > 0 && (index + 1) < firstList.Count && (index + 1 + remainingCount) <= firstList.Count) // Added boundary checks
                    resultList.AddRange(firstList.GetRange(index + 1, remainingCount));
            }
            return resultList;
        }

        private List<Command> FindAllCommandsInFile(string fileName)
        {
            string text = "";
            try
            {
                text = File.ReadAllText(fileName); 
            }
            catch (FileNotFoundException)
            {
                return new List<Command>(); 
            }
            
            var foundCommands = new List<Command>();
            var currentCommand = new Command();
            TextCommand? textCommand = null; // Initialize to null
        
            var readCommandSymbols = false;
            var ignoreWhileStringEnd = false;
        
            nextStrChar = new Dictionary<int,int>(); // Corrected
            _stringNumber = 1;
        
            text += "\n\\"; 
        
            for (int ch = 0; ch < text.Length; ch++)
            {
                if (text[ch] == '%')
                {
                    if(ch > 0 && text[ch - 1] != '\\') ignoreWhileStringEnd = true;
                }
            
                if (text[ch] == '\n')
                {
                    ignoreWhileStringEnd = false;
                    if(!nextStrChar.ContainsKey(ch)) 
                        nextStrChar.TryAdd(ch, 1); 
                    _stringNumber++;
                }
            
                if(ignoreWhileStringEnd)
                    continue;

                if (readCommandSymbols)
                {
                    if (_regexCommandName.Match(currentCommand.Name + text[ch]).Success
                        && !_regexEndString.Match(text[ch].ToString()).Success)
                    {
                        currentCommand.Name += text[ch];
                        continue;
                    }
                
                    currentCommand.GlobalIndex = foundCommands.Count;
                    if (currentCommand.Name == "begin")
                    {
                        var environmentCommand = new EnvironmentCommand(currentCommand);
                        ch = FillEnvironment(environmentCommand, currentCommand, text, ch, foundCommands);
                    }
                    else
                    {
                        // FillCommand returns count of processed chars for params/args
                        // ch is current position *before* params/args.
                        // ch should be advanced by the count of characters processed by FillCommand.
                        // The -1 was likely to counteract the loop's ch++.
                        int processedChars = FillCommand(currentCommand, text, ch + 1); // Start after current char
                        ch += processedChars; // Advance ch by the number of chars processed by FillCommand
                        // No -1 needed if FillCommand returns count of chars *after* command name

                        currentCommand.EndSymbolNumber = ch; // EndSymbolNumber is now end of params/args
                        foundCommands.Add(currentCommand);
                    }
                    currentCommand = new Command();
                    readCommandSymbols = false;
                }
                else
                {
                    textCommand ??= new TextCommand() // Corrected
                    {
                        StartSymbolNumber = ch,
                        FileOwner = fileName,
                        StringNumber = _stringNumber
                    };

                    if (text[ch] == '\\') 
                    {
                        readCommandSymbols = true;
                        currentCommand.StartSymbolNumber = ch;
                        currentCommand.FileOwner = fileName;
                        currentCommand.StringNumber = _stringNumber;
                        if (textCommand != null) { 
                           textCommand.EndSymbolNumber = ch - 1;
                           textCommand.GlobalIndex = foundCommands.Count;
                           if (textCommand.Text != null && textCommand.Text.Length > 0)
                           {
                               foundCommands.Add(textCommand);
                           }
                        }
                        textCommand = null; 
                        continue;
                    }
                    if (textCommand != null) { 
                        textCommand.Text += text[ch];
                    }
                }
            }
            return foundCommands;
        }

        private int FillEnvironment(
            EnvironmentCommand environmentCommand, 
            Command current, 
            string text, 
            int ch, 
            List<Command> founded)
        {
            current.Name = "ref"; 
            // ch is position of last char of "begin". Params start after.
            int processedForBeginArgs = FillCommand(current, text, ch + 1); 
            ch += processedForBeginArgs;
            
            if (current.Arguments.Count > 0 && current.Arguments[0].Value != null)
            {
                environmentCommand.EnvironmentName = current.Arguments[0].Value;
            }
            else
            {
                environmentCommand.EnvironmentName = "unknown"; 
            }
            environmentCommand.Name = "begin";
        
            founded.Add(environmentCommand);
            // Params for the environment itself (e.g. \begin{figure}[H])
            int processedForEnvParams = FillCommand(environmentCommand, text, ch +1); // ch is now end of \begin{...}<env_name_arg>
            ch += processedForEnvParams;
            
            return FindEnvironmentCommands(environmentCommand, text, ch, founded); // Pass current ch
        }

        private int FindEnvironmentCommands(
            EnvironmentCommand environmentCommand, 
            string text, 
            int ch, // ch is position *before* inner commands start
            List<Command> foundCommands)
        {
            if (environmentCommand.EnvironmentName == "lstlisting")
            {
                var lstCommands = TestUtilities.GetAllCommandsLikeParametersFromEnvironment(environmentCommand);
                environmentCommand.InnerCommands.AddRange(lstCommands);
                foundCommands.AddRange(lstCommands);
            }
        
            var currentCommand = new Command();
            TextCommand? textCommand = null; 
        
            bool readCommandSymbols = false;
            bool ignoreWhileStringEnd = false; 
            
            // ch is now at the start of the content within the environment
            for (; ch < text.Length; ch++)
            {
                if (text[ch] == '%')
                {
                    if(ch > 0 && text[ch - 1] != '\\') ignoreWhileStringEnd = true;
                }
            
                if (text[ch] == '\n')
                {
                    ignoreWhileStringEnd = false;
                     if(!nextStrChar.ContainsKey(ch))
                        nextStrChar.TryAdd(ch, 1);
                    _stringNumber++;
                }
            
                if(ignoreWhileStringEnd)
                    continue;
            
                if (readCommandSymbols)
                {
                    if (_regexCommandName.Match(currentCommand.Name + text[ch]).Success
                        && !_regexEndString.Match(text[ch].ToString()).Success)
                    {
                        currentCommand.Name += text[ch];
                        continue;
                    }
                
                    if (environmentCommand.EnvironmentName == "comment" && currentCommand.Name != "end")
                    {
                        currentCommand = new Command();
                        readCommandSymbols = false;
                        continue;
                    }
                
                    currentCommand.GlobalIndex = foundCommands.Count;
                
                    if (currentCommand.Name == "begin")
                    {
                        var _environmentCommand = new EnvironmentCommand(environmentCommand);
                        ch = FillEnvironment(_environmentCommand, currentCommand, text, ch, foundCommands);
                        environmentCommand.InnerCommands.Add(_environmentCommand);
                    }
                    else
                    {
                        int processedChars = FillCommand(currentCommand, text, ch + 1);
                        ch += processedChars;
                        currentCommand.EndSymbolNumber = ch;
                    
                        environmentCommand.InnerCommands.Add(currentCommand);
                        foundCommands.Add(currentCommand);
                    
                        if (currentCommand.Name == "end" &&
                            currentCommand.Arguments.Count > 0 && 
                            currentCommand.Arguments[0].Value == environmentCommand.EnvironmentName)
                        {
                            environmentCommand.EndCommand = currentCommand;
                            return ch; // Return current position
                        }
                    }
                    currentCommand = new Command();
                    readCommandSymbols = false;
                }
                else
                {
                    textCommand ??= new TextCommand() // Corrected
                    {
                        StartSymbolNumber = ch,
                        FileOwner = environmentCommand.FileOwner,
                        StringNumber = _stringNumber
                    };
                    
                    if (text[ch] == '\\') 
                    {
                        readCommandSymbols = true;
                        currentCommand.StartSymbolNumber = ch;
                        currentCommand.StringNumber = _stringNumber;
                        currentCommand.FileOwner = environmentCommand.FileOwner;
                        if(textCommand != null) { 
                           textCommand.EndSymbolNumber = ch - 1;
                           textCommand.GlobalIndex = foundCommands.Count;
                           if (textCommand.Text != null && textCommand.Text.Length > 0) {
                               environmentCommand.InnerCommands.Add(textCommand); // Add to inner commands of environment
                               foundCommands.Add(textCommand); // Also add to global list
                           }
                        }
                        textCommand = null; 
                        continue;
                    }
                    if (textCommand != null) {
                        textCommand.Text += text[ch];
                    }
                }
            }
            return ch; // Return current position at end of text
        }

        private int FillCommand(Command command, string text, int startSymbol) // startSymbol is after command name
        {
            ParseInfo parseInfo;

            if (command.Name == "begin") parseInfo = _handleInfo.GetParseInfoByEnvironments(command);
            else parseInfo = _handleInfo.GetParseInfoByCommand(command);

            int initialStartSymbol = startSymbol;
            int currentPos = startSymbol;

            if (parseInfo is { IsCommandExist: true, Order: not null })
            {
                foreach (var order in parseInfo.Order)
                {
                    int processedCharsForParam; // How many chars this parameter took
                    if (order == "any")
                    {
                        char anySymbol = '|';
                        int firstContentChar = currentPos;
                        while(firstContentChar < text.Length && _regexSpace.Match(text[firstContentChar].ToString()).Success) {
                            firstContentChar++;
                        }
                        if (firstContentChar < text.Length) anySymbol = text[firstContentChar];
                        else { /* No non-space char found, maybe return or error */ }


                        command.Arguments.AddRange(
                            FillParameters(
                                text, currentPos, out processedCharsForParam,
                                parseInfo.Arg.ParseType, new[] { anySymbol, anySymbol }));
                        currentPos += processedCharsForParam;
                    }
                    else if (order == "p")
                    {
                        command.Parameters.AddRange(
                            FillParameters(
                                text, currentPos, out processedCharsForParam,
                                parseInfo.Param.ParseType, new[] { '[', ']' }));
                        currentPos += processedCharsForParam;
                    }
                    else if (order == "a")
                    {
                        command.Arguments.AddRange(
                            FillParameters(
                                text, currentPos, out processedCharsForParam,
                                parseInfo.Arg.ParseType, new[] { '{', '}' }));
                        currentPos += processedCharsForParam;
                    }
                }
            }
            return currentPos - initialStartSymbol; 
        }

        private int _balanceBrackets;

        private List<Parameter> FillParameters(
            string text, 
            int startSymbol, 
            out int processedChars, 
            ParameterParseType parseParseType,
            char[] parseSymbols)
        {
            var ignoreWhileStringEnd = false;
            var currentStep = FillParametersParseOrder.ReadStartSymbol;
            var parameters = new List<Parameter>();
            var parameter = new Parameter() { Text = string.Empty, Value = null };
            
            int ch = startSymbol;
            for (; ch < text.Length; ch++)
            {
                if (text[ch] == parseSymbols[0]) _balanceBrackets++;
                if (text[ch] == parseSymbols[1]) _balanceBrackets--;
            
                if (currentStep == FillParametersParseOrder.ReadStartSymbol)
                {
                    if (text[ch] == parseSymbols[0])
                    {
                        switch (parseParseType)
                        {
                            case ParameterParseType.Value: currentStep = FillParametersParseOrder.ReadParamName; break;
                            case ParameterParseType.Phrase: currentStep = FillParametersParseOrder.ReadPhrase; break;
                        }
                        continue;
                    }
                    if (text[ch] == '\n') {
                        if(!nextStrChar.ContainsKey(ch)) nextStrChar.TryAdd(ch, 1);
                        _stringNumber++;
                    }
                    if (_regexSpace.Match(text[ch].ToString()).Success) continue;
                    
                    processedChars = ch - startSymbol; 
                    return parameters;
                }

                if (text[ch] == '%' && ch > 0 && text[ch - 1] != '\\') ignoreWhileStringEnd = true;
                if (text[ch] == '\n')
                {
                    ignoreWhileStringEnd = false;
                    if(!nextStrChar.ContainsKey(ch)) nextStrChar.TryAdd(ch, 1);
                    _stringNumber++;
                }
                if(ignoreWhileStringEnd) continue;
            
                if (currentStep == FillParametersParseOrder.ReadParamName)
                {
                    if (_regexParam.Match(parameter.Text + text[ch]).Success)
                    {
                        parameter.Text += text[ch];
                        continue;
                    }
                    if (text[ch] == ',')
                    {
                        parameters.Add(parameter);
                        parameter = new Parameter(); 
                        continue;
                    }
                    if (text[ch] == '=')
                    {
                        currentStep = FillParametersParseOrder.ReadValue;
                        parameter.Value = string.Empty; 
                        continue;
                    }
                    // Corrected: check _balanceBrackets before returning
                    if (text[ch] == parseSymbols[1] && _balanceBrackets == 0) 
                    {
                        parameters.Add(parameter);
                        processedChars = ch - startSymbol + 1;
                        return parameters;
                    }
                     // If it's the closing symbol but balance is not zero, continue accumulating (part of nested structure)
                    else if (text[ch] == parseSymbols[1] && _balanceBrackets != 0) {
                        // This case implies the closing bracket is part of the parameter text itself
                        // This might be an issue if not handled carefully, for now, let it pass to accumulate
                        parameter.Text += text[ch]; // Or parameter.Value if in that state
                        continue;
                    }
                }
                else if (currentStep == FillParametersParseOrder.ReadValue)
                {
                    if (text[ch] == '{')
                    {
                        currentStep = FillParametersParseOrder.ReadPhraseValue;
                        continue;
                    }
                    if (text[ch] == ',')
                    {
                        parameters.Add(parameter);
                        parameter = new Parameter(); 
                        currentStep = FillParametersParseOrder.ReadParamName; 
                        continue;
                    }
                    if (text[ch] == parseSymbols[1] && _balanceBrackets == 0) 
                    {
                        parameters.Add(parameter);
                        processedChars = ch - startSymbol + 1;
                        return parameters;
                    }
                    // If it's the closing symbol but balance is not zero, part of value
                    parameter.Value += text[ch]; 
                }
                else if (currentStep == FillParametersParseOrder.ReadPhraseValue)
                {
                    if (text[ch] == '}')
                    {
                        // Phrase value ends, what's next? Usually back to reading param name for key=value
                        currentStep = FillParametersParseOrder.ReadParamName; 
                        continue;
                    }
                    parameter.Value += text[ch];
                }
                else if (currentStep == FillParametersParseOrder.ReadPhrase)
                {
                    if (text[ch] == parseSymbols[1] && _balanceBrackets == 0)
                    {
                        // The content of a "phrase" is typically its value.
                        // Original code assigned to parameter.Value.
                        // If parameter.Text was used to accumulate, it should be moved to Value.
                        if (string.IsNullOrEmpty(parameter.Value)) parameter.Value = parameter.Text;
                        else parameter.Value += parameter.Text; // Or decide how to merge if both have data.
                        parameter.Text = string.Empty; // Usually, phrase doesn't have a "name" part like key=value.

                        parameters.Add(parameter);
                        processedChars = ch - startSymbol + 1;
                        return parameters;
                    }
                    // For Phrase, accumulate everything as its value (or text, depending on interpretation)
                    // Original code used parameter.Value, so we stick to it.
                    parameter.Value += text[ch];
                }
            }
            processedChars = ch - startSymbol; 
            if (!string.IsNullOrEmpty(parameter.Text) || !string.IsNullOrEmpty(parameter.Value)) {
                 if (currentStep == FillParametersParseOrder.ReadPhrase && string.IsNullOrEmpty(parameter.Value)) {
                    parameter.Value = parameter.Text;
                 }
                 parameters.Add(parameter);
            }
            return parameters;
        }
    
        private enum FillParametersParseOrder
        {
            ReadStartSymbol = 0,
            ReadParamName = 1,
            ReadValue = 2,
            ReadPhrase = 3,
            ReadPhraseValue = 4
        }
    }
}
