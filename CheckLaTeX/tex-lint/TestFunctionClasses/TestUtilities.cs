using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TexLint.Models;

namespace TexLint.TestFunctionClasses;

/// <summary>
/// Thread-safe singleton для хранения данных тестирования с изоляцией по идентификатору запроса
/// </summary>
public sealed class TestUtilities
{
    private static readonly Lazy<TestUtilities> _instance = new Lazy<TestUtilities>(() => new TestUtilities());
    
    // Thread-safe словарь для хранения данных по запросам
    private readonly ConcurrentDictionary<string, RequestData> _requestData = new();
    
    // Кэшированные пути к конфигурационным файлам (общие для всех запросов)
    private static string? _pathToCommandsJson;
    private static string? _pathToEnvironmentJson;
    private static string? _pathToLintRulesJson;
    
    public static TestUtilities Instance => _instance.Value;
    
    private TestUtilities() { }
    
    /// <summary>
    /// Данные, изолированные по запросу
    /// </summary>
    private class RequestData
    {
        public List<Command> FoundsCommands { get; set; } = new();
        public List<Command> FoundsCommandsWithLstlisting { get; set; } = new();
        public string StartDirectory { get; set; } = string.Empty;
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Получает или создает данные для указанного запроса
    /// </summary>
    private RequestData GetRequestData(string requestId)
    {
        return _requestData.GetOrAdd(requestId, _ => new RequestData());
    }
    
    /// <summary>
    /// Очищает данные для указанного запроса
    /// </summary>
    public void ClearRequestData(string requestId)
    {
        _requestData.TryRemove(requestId, out _);
        Console.WriteLine($"[TestUtilities] Очищены данные для запроса: {requestId}");
    }
    
    /// <summary>
    /// Очищает устаревшие данные (старше указанного времени)
    /// </summary>
    public void CleanupOldData(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var keysToRemove = _requestData
            .Where(kvp => kvp.Value.LastAccessed < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var key in keysToRemove)
        {
            _requestData.TryRemove(key, out _);
        }
        
        if (keysToRemove.Count > 0)
        {
            Console.WriteLine($"[TestUtilities] Очищено {keysToRemove.Count} устаревших запросов");
        }
    }
    
    /// <summary>
    /// Получает количество активных запросов
    /// </summary>
    public int GetActiveRequestsCount() => _requestData.Count;
    
    // Свойства для работы с конфигурационными файлами (общие)
    public static string PathToCommandsJson => _pathToCommandsJson ??= FindConfigFile("commands.json");
    public static string PathToEnvironmentJson => _pathToEnvironmentJson ??= FindConfigFile("environments.json");
    public static string PathToLintRulesJson => _pathToLintRulesJson ??= FindConfigFile("lint-rules.json");
    
    // Свойства для работы с данными конкретного запроса
    public List<Command> GetFoundsCommands(string requestId)
    {
        var data = GetRequestData(requestId);
        data.LastAccessed = DateTime.UtcNow;
        return data.FoundsCommands;
    }
    
    public void SetFoundsCommands(string requestId, List<Command> commands)
    {
        var data = GetRequestData(requestId);
        data.FoundsCommands = commands;
        data.LastAccessed = DateTime.UtcNow;
        Console.WriteLine($"[TestUtilities] Установлено {commands.Count} команд для запроса: {requestId}");
    }
    
    public List<Command> GetFoundsCommandsWithLstlisting(string requestId)
    {
        var data = GetRequestData(requestId);
        data.LastAccessed = DateTime.UtcNow;
        return data.FoundsCommandsWithLstlisting;
    }
    
    public void SetFoundsCommandsWithLstlisting(string requestId, List<Command> commands)
    {
        var data = GetRequestData(requestId);
        data.FoundsCommandsWithLstlisting = commands;
        data.LastAccessed = DateTime.UtcNow;
        Console.WriteLine($"[TestUtilities] Установлено {commands.Count} команд с lstlisting для запроса: {requestId}");
    }
    
    public string GetStartDirectory(string requestId)
    {
        var data = GetRequestData(requestId);
        data.LastAccessed = DateTime.UtcNow;
        return data.StartDirectory;
    }
    
    public void SetStartDirectory(string requestId, string directory)
    {
        var data = GetRequestData(requestId);
        data.StartDirectory = directory;
        data.LastAccessed = DateTime.UtcNow;
        Console.WriteLine($"[TestUtilities] Установлена директория для запроса {requestId}: {directory}");
    }
    
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
    
    public List<Command> GetAllCommandsByName(string requestId, string name)
    {
        var commands = GetFoundsCommands(requestId);
        var founds = new List<Command>();
        foreach (var command in commands)
            if(command.Name == name)
                founds.Add(command);
        return founds;
    }
    
    public List<Command> GetAllCommandsByNameFromList(string name, IEnumerable<Command> list)
    {
        var founds = new List<Command>();
        foreach (var command in list)
            if(command.Name == name)
                founds.Add(command);
        return founds;
    }
    
    public List<EnvironmentCommand> GetAllEnvironment(string requestId)
    {
        var commands = GetFoundsCommands(requestId);
        List<EnvironmentCommand> founds = new();
        foreach (var command in commands)
            if (command is EnvironmentCommand environments)
                founds.Add(environments);
        return founds;
    }
    
    public List<Command> GetAllCommandsLikeParametersFromLstlisting(string requestId)
    {
        List<EnvironmentCommand> founds = GetAllEnvironment(requestId);
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

    public List<Command> GetAllCommandsLikeParametersFromEnvironment(EnvironmentCommand environmentCommand)
    {
        var foundsCommand = new List<Command>();
        foreach (var argument in environmentCommand.Parameters)
        {
            foundsCommand.Add(new Command(environmentCommand.FileOwner)
            {
                StringNumber = environmentCommand.StringNumber,
                Name = argument.Text,
                Arguments = new()
                {
                    new Parameter{Value = argument.Value}
                }
            });
        }
        return foundsCommand;
    }
    
    /// <summary>
    /// Статическая версия для использования в CommandHandler и других классах, не имеющих доступа к requestId
    /// </summary>
    public static List<Command> GetAllCommandsLikeParametersFromEnvironmentStatic(EnvironmentCommand environmentCommand)
    {
        var foundsCommand = new List<Command>();
        foreach (var argument in environmentCommand.Parameters)
        {
            foundsCommand.Add(new Command(environmentCommand.FileOwner)
            {
                StringNumber = environmentCommand.StringNumber,
                Name = argument.Text,
                Arguments = new()
                {
                    new Parameter{Value = argument.Value}
                }
            });
        }
        return foundsCommand;
    }

    public Command GetCommandByIndexInCollection(int index, IEnumerable<Command> collection)
    {
        return collection.First(command =>
            command.GlobalIndex == index);
    }

    public string GetContentAreaFromFindSymbol(string requestId, TextCommand textCommand, int centerAreaSymbolNumber, int areaLenght = 50)
    {
        var commands = GetFoundsCommands(requestId);
        var text = textCommand.Text;
        var areaText = string.Empty;
        int count = areaLenght%2 == 0 ? areaLenght/2 : (int)(areaLenght / 2) + 1;
        int commandCount = 1;
        while(text.Length-centerAreaSymbolNumber < count)
        {
            text += GetCommandByIndexInCollection(textCommand.GlobalIndex + commandCount, commands);
            commandCount++;
        }

        commandCount = 1;
        while(centerAreaSymbolNumber-count < 0)
        {
            text = GetCommandByIndexInCollection(textCommand.GlobalIndex - commandCount, commands) + text;
            centerAreaSymbolNumber +=
                GetCommandByIndexInCollection(textCommand.GlobalIndex - commandCount, commands).ToString()
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
