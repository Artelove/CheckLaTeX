using System.Net.Mime;
using System.Text.RegularExpressions;
using TexLint.Models.HandleInfos;
using TexLint.TestFunctionClasses;

namespace TexLint.Models;

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
    private Dictionary<int,int> nextStrChar = new();
    public string StartFile;
    public string StartDirectory;

    public CommandHandler(string startFile, string startDirectory, ILatexConfigurationService configurationService)
    {
        _regexCommandName = new(PATTERN_COMMAND_NAME);
        _regexParam = new(PATTERN_PARAMETER);
        _regexSpace = new(PATTERN_WHITE_SPACE);
        _regexEndString = new(PATTERN_END_OF_STRING);
        StartFile = startFile;
        StartDirectory = startDirectory;
        _handleInfo = new HandleInfo(configurationService);
    }

    /// <summary>
    /// Вычисляет номер строки и столбца для заданной позиции в тексте
    /// </summary>
    /// <param name="text">Исходный текст</param>
    /// <param name="position">Позиция в тексте</param>
    /// <returns>Кортеж (номер строки, номер столбца) - оба 1-based</returns>
    private (int line, int column) GetLineAndColumn(string text, int position)
    {
        if (position < 0 || position >= text.Length)
            return (1, 1);

        int line = 1;
        int column = 1;

        for (int i = 0; i < position; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }

    /// <summary>
    /// Заполняет позиционную информацию для команды
    /// </summary>
    /// <param name="command">Команда для заполнения</param>
    /// <param name="text">Исходный текст</param>
    /// <param name="startPos">Начальная позиция</param>
    /// <param name="endPos">Конечная позиция</param>
    private void FillSourcePositions(Command command, string text, int startPos, int endPos)
    {
        command.SourceStartPosition = startPos;
        command.SourceEndPosition = endPos;
        
        var (startLine, startColumn) = GetLineAndColumn(text, startPos);
        var (endLine, endColumn) = GetLineAndColumn(text, endPos);
        
        command.SourceStartLine = startLine;
        command.SourceStartColumn = startColumn;
        command.SourceEndLine = endLine;
        command.SourceEndColumn = endColumn;
    }

    /// <summary>
    /// Находит команды в исходном файле, после чего проходится по всем командам для поиске команд \input и \include
    /// <br/>
    /// Происходит поиск команд в файле обозначенном в аргументе команды,
    /// затем найденные команды замещают команду вставкой,
    /// далее происходит поиск новых команды в обновленном списке команд 
    /// </summary>
    /// <returns>
    /// Распарсенные команды (включая текстовые и команды окружения с аргументами и параметрами),
    /// содержащиеся в подключенных файлах относительно исходного
    /// </returns>
    public List<Command> FindAllCommandsInDocument()
    {
        var foundCommands = FindAllCommandsInFile(StartDirectory+"\\"+StartFile);
        
        for (int i = 0; i < foundCommands.Count; i++)
        {
            if (foundCommands[i].Name == "input" || foundCommands[i].Name == "include")
            {
                if (new Regex(".tex|.bib").Match(foundCommands[i].Arguments[0].Value).Success == false)
                    foundCommands[i].Arguments[0].Value += ".tex";
                
                foundCommands = InsertListAtIndex(
                    foundCommands,
                    FindAllCommandsInFile(StartDirectory+"\\"+foundCommands[i].Arguments[0].Value),
                    i,
                    false);
            }
        }

        return foundCommands;
    }

    /// <summary>
    /// Метод создает новый лист, вставляя элементы первого листа до индекса, вставляя элементы второго листа,
    /// вставляя элементы первого листа начиная с индекса и до конца листа 
    /// </summary>
    /// <param name="firstList">Лист, чьи элементы раздвигаются на месте индекса</param>
    /// <param name="secondList">Лист, чьи элементы вставляются на место индекса</param>
    /// <param name="index">Индекс, по которому происходит разделение исходного листа </param>
    /// <param name="includeCommandByIndex">Условие включения команды по индексу разделения в возращаемый лист</param>
    /// <returns>Лист, содержащий элементы двух листов</returns>
    private List<Command> InsertListAtIndex(
        List<Command> firstList, 
        List<Command> secondList, 
        int index, 
        bool includeCommandByIndex = true)
    {
        var resultList = new List<Command>();
        
        resultList.AddRange(firstList.GetRange(0, index));
        
        resultList.AddRange(secondList);

        if (includeCommandByIndex)
        {
            resultList.AddRange(firstList.GetRange(index, firstList.Count - index));
        }
        else
        {
            resultList.AddRange(firstList.GetRange(index + 1, firstList.Count - index - 1));
        }
        
        return resultList;
    }

    /// <summary>
    /// Находит все команды (также окружения и текст) и заполняет их параметры и аргументы в тексте файла.
    /// </summary>
    /// <remarks>
    /// Используется посимвольный поиск.
    /// Для проверки необходимости следующего этапа, используются регулярные выражения.
    /// </remarks>
    /// <param name="fileName">Название файла, где происходит поиск команд</param>
    /// <returns>
    /// Распарсенные команды (включая текстовые и команды окружения с аргументами и параметрами),
    /// содержащиеся в файле
    /// </returns>
    private List<Command> FindAllCommandsInFile(string fileName)
    {
        var text = new StreamReader(fileName).ReadToEnd();
        
        var foundCommands = new List<Command>();
        var currentCommand = new Command(fileName);
        var textCommand = new TextCommand(fileName);
        
        var readCommandSymbols = false;
        var ignoreWhileStringEnd = false;
        
        nextStrChar = new();
        _stringNumber = 1;
        
        text += "\n\\";
        
        for (int ch = 0; ch < text.Length; ch++)
        {
            if (text[ch] == '%')
            {
                if(ch-1>=0 && text[ch - 1] != '\\') ignoreWhileStringEnd = true;
            }
            
            if (text[ch] == '\n')
            {
                ignoreWhileStringEnd = false;
                
                if(nextStrChar.TryAdd(ch, 1))
                    _stringNumber++;
            }
            
            if(ignoreWhileStringEnd)
                continue;

            if (readCommandSymbols)
            {
                if (_regexCommandName.Match(currentCommand.Name + text[ch]).Success
                    && _regexEndString.Match(text[ch].ToString()).Success == false)
                {
                    currentCommand.Name += text[ch];
                    continue;
                }
                
                currentCommand.GlobalIndex = foundCommands.Count;
                if (currentCommand.Name == "begin")
                {
                    var environmentCommand = new EnvironmentCommand(currentCommand);
                    ch = FillEnvironment(environmentCommand, currentCommand, text, ch, foundCommands);
                    
                    // Позиционная информация уже заполнена в FillEnvironment
                }
                else
                {
                    ch += FillCommand(currentCommand, text, ch) - 1;
                    currentCommand.EndSymbolNumber = ch;
                    
                    // Заполняем позиционную информацию для команды
                    FillSourcePositions(currentCommand, text, currentCommand.StartSymbolNumber, ch);
                    
                    foundCommands.Add(currentCommand);
                }

                currentCommand = new Command(fileName);
                readCommandSymbols = false;
            }
            else
            {
                textCommand ??= new(fileName)
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
                    textCommand.EndSymbolNumber = ch - 1;
                    textCommand.GlobalIndex = foundCommands.Count;
                    if (textCommand.Text.Length > 0)
                    {
                        // Заполняем позиционную информацию для текстовой команды
                        FillSourcePositions(textCommand, text, textCommand.StartSymbolNumber, ch - 1);
                        
                        foundCommands.Add(textCommand);
                    }

                    textCommand = null;
                    continue;
                }

                textCommand.Text += text[ch];
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
        current.Name = "ref"; // КОСТЫЛЬ. Изменения имени команды, для заполнения только одного аргумента
        
        ch += FillCommand(current, text, ch); //Нахождение названия окружения в аргументе
        
        environmentCommand.EnvironmentName = current.Arguments[0].Value;
        environmentCommand.Name = "begin";
        
        // Заполняем позиционную информацию для команды окружения
        FillSourcePositions(environmentCommand, text, environmentCommand.StartSymbolNumber, ch);
        
        founded.Add(environmentCommand);
        ch += FillCommand(environmentCommand, text, ch);
        
        return FindEnvironmentCommands(environmentCommand, text, ch, founded);
    }

    /// <summary>
    /// Метод имеющий аналогичную функцию "FindAllCommandsInFile", с отличем в том,
    /// что заполнение и поиск происходит внутри окружения, до команды \end 
    /// </summary>
    /// <param name="environmentCommand">Команда окружения</param>
    /// <param name="text">Поисковый текст</param>
    /// <param name="ch">Номер символа, где начинается окружение в text</param>
    /// <param name="foundCommands">Лист найденных команд до окружения</param>
    /// <remarks>Плохая практика дублирования кода, но на данный момент переписывание кода не целесообразно</remarks>
    /// <returns>Номер символа, где заканчивается окружение в text</returns>
    private int FindEnvironmentCommands(
        EnvironmentCommand environmentCommand, 
        string text, 
        int ch,
        List<Command> foundCommands)
    {
        if (environmentCommand.EnvironmentName == "lstlisting")
        {
            environmentCommand.InnerCommands.AddRange(
                TestUtilities.GetAllCommandsLikeParametersFromEnvironmentStatic(environmentCommand));
            foundCommands.AddRange(TestUtilities.GetAllCommandsLikeParametersFromEnvironmentStatic(environmentCommand));
        }
        
        var currentCommand = new Command(environmentCommand.FileOwner);
        TextCommand textCommand = null;
        
        bool readCommandSymbols = false;
        bool ignoreWhileStringEnd = false; 
        
        for (; ch < text.Length; ch++)
        {
            if (text[ch] == '%')
            {
                if(ch-1>=0 && text[ch - 1] != '\\') ignoreWhileStringEnd = true;
            }
            
            if (text[ch] == '\n')
            {
                ignoreWhileStringEnd = false;
                if(nextStrChar.TryAdd(ch, 1))
                    _stringNumber++;
            }
            
            if(ignoreWhileStringEnd)
                continue;
            
            if (readCommandSymbols)
            {
                if (_regexCommandName.Match(currentCommand.Name + text[ch]).Success
                    && _regexEndString.Match(text[ch].ToString()).Success == false)
                {
                    currentCommand.Name += text[ch];
                    continue;
                }
                
                if (environmentCommand.EnvironmentName == "comment" && currentCommand.Name != "end")
                {
                    currentCommand = new Command(environmentCommand.FileOwner);
                    readCommandSymbols = false;
                    continue;
                }
                
                currentCommand.GlobalIndex = foundCommands.Count;
                
                if (currentCommand.Name == "begin")
                {
                    var _environmentCommand = new EnvironmentCommand(environmentCommand);
                    ch = FillEnvironment(_environmentCommand, currentCommand, text, ch, foundCommands);
                    
                    // Заполняем позиционную информацию для вложенного окружения
                    FillSourcePositions(_environmentCommand, text, _environmentCommand.StartSymbolNumber, ch);
                    
                    environmentCommand.InnerCommands.Add(_environmentCommand);
                }
                else
                {
                    ch += FillCommand(currentCommand, text, ch) - 1;
                    currentCommand.EndSymbolNumber = ch;
                    
                    // Заполняем позиционную информацию для команды в окружении
                    FillSourcePositions(currentCommand, text, currentCommand.StartSymbolNumber, ch);
                    
                    environmentCommand.InnerCommands.Add(currentCommand);
                    foundCommands.Add(currentCommand);
                    
                    //Поиск команды завершения окружения и проверка на соответствие названию begin и end 
                    if (currentCommand.Name == "end" &&
                        currentCommand?.Arguments != null &&
                        currentCommand.Arguments.Count > 0 &&
                        currentCommand.Arguments[0].Value == environmentCommand.EnvironmentName)
                    {
                        environmentCommand.EndCommand = currentCommand;
                        return ch;
                    }
                }

                currentCommand = new Command(environmentCommand.FileOwner);
                readCommandSymbols = false;
            }
            else
            {
                textCommand ??= new (environmentCommand.FileOwner)
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
                    textCommand.EndSymbolNumber = ch - 1;
                    textCommand.GlobalIndex = foundCommands.Count;
                    environmentCommand.InnerCommands.Add(textCommand);
                    if (textCommand.Text.Length > 0)
                    {
                        // Заполняем позиционную информацию для текстовой команды в окружении
                        FillSourcePositions(textCommand, text, textCommand.StartSymbolNumber, ch - 1);
                        
                        foundCommands.Add(textCommand);
                        //Console.WriteLine("TEXT:" + textCommand.Text);
                    }

                    textCommand = null;
                    continue;
                }

                textCommand.Text += text[ch];
            }
        }

        return ch;
    }

    /// <summary>
    /// Заполение полей параметров и аругментов команды по соответствующей ей конфигурации
    /// </summary>
    /// <param name="command">Заполняемая команда</param>
    /// <param name="text">Поисковый текст</param>
    /// <param name="startSymbol">Символ, откуда начинается заполнение</param>
    /// <returns>Количество символов прошедших методом в ходе поиска</returns>
    private int FillCommand(Command command, string text, int startSymbol)
    {
        ParseInfo parseInfo;

        if (command.Name == "begin") 
            parseInfo = _handleInfo.GetParseInfoByEnvironments(command);
        else 
            parseInfo = _handleInfo.GetParseInfoByCommand(command);

        var startCh = startSymbol;
        var endSymbol = startSymbol;

        //Console.Write("\\"+command.Name);
        if (parseInfo is { IsCommandExist: true, Order: not null })
            foreach (var order in parseInfo.Order)
            {
                //Перенос начального символа, для перехода к следующему поиску параметров
                startSymbol += endSymbol - startSymbol;
                //Иное значение
                if (order == "any")
                {
                    char anySymbol = '|';
                    //Поиск первого встречающегося непустого символа для использования границ аргумента 
                    for (int i = startSymbol; i < text.Length; i++)
                    {
                        if (_regexSpace.Match(text[i].ToString()).Success)
                            continue;

                        anySymbol = text[i];
                        break;
                    }

                    command.Arguments.AddRange(
                        FillParameters(
                            text,
                            startSymbol,
                            out endSymbol,
                            parseInfo.Arg.ParseType,
                            new[] { anySymbol, anySymbol }));
                    //Console.Write(command.ArgumentsToString());
                }

                //Параметр
                if (order == "p")
                {
                    command.Parameters.AddRange(
                        FillParameters(
                            text,
                            startSymbol,
                            out endSymbol,
                            parseInfo.Param.ParseType,
                            new[] { '[', ']' }));
                    // Console.Write(command.ParametersToString());
                }

                //Аргумент
                if (order == "a")
                {
                    command.Arguments.AddRange(
                        FillParameters(
                            text,
                            startSymbol,
                            out endSymbol,
                            parseInfo.Arg.ParseType,
                            new[] { '{', '}' }));
                    //Console.Write(command.ArgumentsToString());
                }
            }

        //Console.WriteLine();
        return endSymbol - startCh;
    }

    //Значение определяющее количество баланса открытых скобок к открытым для предотвращения прерывания поиска из-за 
    //нахождения в аргументе или параметре пару скобок
    private int _balanceBrackets;

    /// <summary>
    /// Поиск входных символов и заполение параметров относительно ParameterParseType
    /// </summary>
    /// <param name="text">Поисковый текст</param>
    /// <param name="startSymbol">Номер начального поискового символа</param>
    /// <param name="endSymbol">Выходной параметр конечного символа поиска</param>
    /// <param name="parseParseType">Тип парсинга</param>
    /// <param name="parseSymbols">Массив из 2 элементов содержащий открывающий и закрывающий символы</param>
    /// <returns></returns>
    private List<Parameter> FillParameters(
        string text, 
        int startSymbol, 
        out int endSymbol, 
        ParameterParseType parseParseType,
        char[] parseSymbols)
    {
        var ignoreWhileStringEnd = false;
        var currentStep = FillParametersParseOrder.ReadStartSymbol;
        var parameters = new List<Parameter>();
        
        var parameter = new Parameter()
        {
            Text = string.Empty,
            Value = null
        };
        endSymbol = startSymbol;
        
        for (int ch = startSymbol; ch < text.Length; ch++)
        {
            if (text[ch] == parseSymbols[0])
                _balanceBrackets++;
            
            if (text[ch] == parseSymbols[1])
                _balanceBrackets--;
            
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
                if (text[ch] == '\n' && nextStrChar.TryAdd(ch, 1)) 
                    _stringNumber++;
                
                if (_regexSpace.Match(text[ch].ToString()).Success) 
                    continue;

                return parameters;
            }

            if (text[ch] == '%' &&
                ch-1>=0 &&
                text[ch - 1] != '\\')
            {
                    ignoreWhileStringEnd = true;
            }
            
            if (text[ch] == '\n')
            {
                ignoreWhileStringEnd = false;
                
                if(nextStrChar.TryAdd(ch, 1))
                    _stringNumber++;
            }
            
            if(ignoreWhileStringEnd)
                continue;
            
            if (currentStep == FillParametersParseOrder.ReadParamName)
            {
                if (_regexParam.Match(parameter.Text + text[ch]).Success)
                {
                    parameter.Text += text[ch];
                    continue;
                }

                if (text[ch] == ',')
                {
                    currentStep = FillParametersParseOrder.ReadParamName;
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

                if (text[ch] == parseSymbols[1])
                {
                    endSymbol = ch + 1;
                    parameters.Add(parameter);
                    
                    return parameters;
                }
            }

            if (currentStep == FillParametersParseOrder.ReadValue)
            {
                if (text[ch] == '{')
                {
                    currentStep = FillParametersParseOrder.ReadPhraseValue;
                    
                    continue;
                }

                if (text[ch] == ',')
                {
                    currentStep = FillParametersParseOrder.ReadParamName;
                    parameters.Add(parameter);
                    parameter = new Parameter();
                    
                    continue;
                }

                if (_regexParam.Match(parameter.Text + text[ch]).Success
                    && _regexEndString.Match(text[ch].ToString()).Success == false)
                {
                    parameter.Value += text[ch];
                    
                    continue;
                }
                
                if (text[ch] == parseSymbols[1] && _balanceBrackets == 0)
                {
                    endSymbol = ch + 1;
                    parameters.Add(parameter);
                    _balanceBrackets = 0;
                    
                    return parameters;
                }
            }

            if (currentStep == FillParametersParseOrder.ReadPhraseValue)
            {
                if (text[ch] == '}')
                {
                    currentStep = FillParametersParseOrder.ReadParamName;
                    
                    continue;
                }

                parameter.Value += text[ch];
            }

            if (currentStep == FillParametersParseOrder.ReadPhrase)
            {
                if (text[ch] == parseSymbols[1] && _balanceBrackets == 0)
                {
                    endSymbol = ch + 1;
                    parameters.Add(parameter);
                    _balanceBrackets = 0;
                    
                    return parameters;
                }

                parameter.Value += text[ch];
            }
        }

        return parameters;
    }
    
    /// <summary>
    /// Перечисление определяющее этапы парсинга команды
    /// ReadStartSymbol. Этап, описывающий поиск начального символа означающего границы параметра.
    /// ReadParamName. Этап, описывающий чтение имени значимого параметра.
    /// ReadValueName. Этап, описывающий чтение значения значимого параметра.
    /// ReadPhrase. Этап, описывающий чтение всего значения внутри граничных символов как значение параметра.
    /// ReadPhraseValue. Этап, описывающий чтение всего значения внутри граничных символов при значащем параметре.
    /// </summary>
    private enum FillParametersParseOrder
    {
        ReadStartSymbol = 0,
        ReadParamName = 1,
        ReadValue = 2,
        ReadPhrase = 3,
        ReadPhraseValue = 4
    }
    
    /// <summary>
    /// Получает контекст LaTeX текста вокруг указанной позиции для отладки
    /// </summary>
    /// <param name="text">Полный текст</param>
    /// <param name="position">Позиция ошибки</param>
    /// <param name="contextLength">Длина контекста в символах с каждой стороны</param>
    /// <returns>Контекстная строка с выделением позиции ошибки</returns>
    private string GetLatexContext(string text, int position, int contextLength = 50)
    {
        if (string.IsNullOrEmpty(text) || position < 0 || position >= text.Length)
            return "Контекст недоступен";
            
        var start = Math.Max(0, position - contextLength);
        var end = Math.Min(text.Length, position + contextLength);
        
        var before = text.Substring(start, position - start);
        var current = position < text.Length ? text[position].ToString() : "";
        var after = text.Substring(position + current.Length, end - position - current.Length);
        
        return $"{before}【{current}】{after}";
    }
}