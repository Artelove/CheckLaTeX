using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TexLint.Models;

namespace TexLint.TestFunctionClasses;

public static class TestUtilities
{
    private static string? _pathToCommandsJson;
    private static string? _pathToEnvironmentJson;
    private static string? _pathToLintRulesJson;
    
    public static string PathToCommandsJson => _pathToCommandsJson ??= FindConfigFile("commands.json");
    public static string PathToEnvironmentJson => _pathToEnvironmentJson ??= FindConfigFile("environments.json");
    public static string PathToLintRulesJson => _pathToLintRulesJson ??= FindConfigFile("lint-rules.json");
    
    public static List<Command> FoundsCommands { get; set; }
    public static List<Command> FoundsCommandsWithLstlisting { get; set; }
    public static string StartDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Ищет конфигурационный файл в различных возможных местах
    /// </summary>
    public static string FindConfigFile(string fileName)
    {
        // Поиск в различных локациях
        var searchPaths = new[]
        {
            fileName, // Текущая директория
            Path.Combine("..", fileName), // Родительская директория
            Path.Combine("..", "..", fileName), // На два уровня выше
            Path.Combine("..", "..", "..", fileName), // На три уровня выше (для bin/Debug/net6.0)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName), // Директория приложения
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", fileName), // Корень проекта от bin
        };
        
        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }
        
        // Если не найден, возвращаем путь по умолчанию
        return Path.Combine("..", "..", "..", fileName);
    }
    public static List<Command> GetAllCommandsByName(string name)
    {
        var founds = new List<Command>();
        foreach (var command in FoundsCommands)
            if(command.Name == name)
                founds.Add(command);
        return founds;
    }
    public static List<Command> GetAllCommandsByNameFromList(string name, IEnumerable<Command> list)
    {
        var founds = new List<Command>();
        foreach (var command in list)
            if(command.Name == name)
                founds.Add(command);
        return founds;
    }
    public static List<EnvironmentCommand> GetAllEnvironment()
    {
        List<EnvironmentCommand> founds = new();
        foreach (var command in FoundsCommands)
            if (command is EnvironmentCommand environments)
                founds.Add(environments);
        return founds;
    }
    public static List<Command> GetAllCommandsLikeParametersFromLstlisting()
    {
        List<EnvironmentCommand> founds = GetAllEnvironment();
        var foundsCommand = new List<Command>();
        foreach (var found in founds)
        {
            if (found.EnvironmentName == "lstlisting")
            {
                foundsCommand.AddRange(GetAllCommandsLikeParametersFromEnvironment(found));
            }
        }
        return foundsCommand;
    }

    public static List<Command> GetAllCommandsLikeParametersFromEnvironment(EnvironmentCommand environmentCommand)
    {
        var foundsCommand = new List<Command>();
        foreach (var argument in environmentCommand.Parameters)
        {
            foundsCommand.Add(new Command()
            {
                StringNumber = environmentCommand.StringNumber,
                FileOwner = environmentCommand.FileOwner,
                Name = argument.Text,
                Arguments = new()
                {
                    new Parameter{Value = argument.Value}
                }
            });
        }
        return foundsCommand;
    }

    public static Command GetCommandByIndexInCollection(int index, IEnumerable<Command> collection)
    {
        return collection.First(command =>
            command.GlobalIndex == index);
    }

    
    public static string GetContentAreaFromFindSymbol(TextCommand textCommand, int centerAreaSymbolNumber, int areaLenght = 50)
    {
        var text = textCommand.Text;
        var areaText = string.Empty;
        int count = areaLenght%2 == 0 ? areaLenght/2 : (int)(areaLenght / 2) + 1;
        int commandCount = 1;
        while(text.Length-centerAreaSymbolNumber < count)
        {
            text += GetCommandByIndexInCollection(textCommand.GlobalIndex + commandCount, FoundsCommands);
            commandCount++;
        }

        commandCount = 1;
        while(centerAreaSymbolNumber-count < 0)
        {
            text = GetCommandByIndexInCollection(textCommand.GlobalIndex - commandCount, FoundsCommands) + text;
            centerAreaSymbolNumber +=
                GetCommandByIndexInCollection(textCommand.GlobalIndex - commandCount, FoundsCommands).ToString()
                    .Length;
            commandCount++;
        }
        for (int i = centerAreaSymbolNumber; i < count + centerAreaSymbolNumber; i++)
        {
            areaText += text[i];
        }

        for (int i = centerAreaSymbolNumber - 1; i >= centerAreaSymbolNumber-count; i--)
        {
            areaText = text[i] + areaText;
        }
        return areaText;
    }
}
