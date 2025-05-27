using System.Text.Json;
using TexLint.Models.HandleInfos;
using TexLint.TestFunctionClasses;

namespace TexLint.Models;

class HandleInfo
{
    private List<ParseInfo> Commands { get; set; }
    private List<ParseInfo> Environments { get; set; }

    public enum ParamsOrder
    {
        Optional,
        Param
    }

    private bool CommandExist = false;

    /// <summary>
    /// Коструктор, заполняющий объекты содержащие конфигурацию команд и окружений из соответствующих Json-файлов
    /// </summary>
    /// <param name="jsonCommands">Json в виде строки из файла конфигурации команд</param>
    /// <param name="jsonEnvironments">Json в виде строки из файла конфигурации окружений</param>
    public HandleInfo()
    {
        Commands = JsonSerializer.Deserialize<List<ParseInfo?>>(new StreamReader(TestUtilities.PathToCommandsJson).ReadToEnd());
        Environments = JsonSerializer.Deserialize<List<ParseInfo?>>(new StreamReader(TestUtilities.PathToEnvironmentJson).ReadToEnd());
    }

    public ParseInfo GetParseInfoByCommand(Command command)
    {
        foreach (var info in Commands)
        {
            if (info.Name == command.Name)
                return info;
        }

        return new ParseInfo { IsCommandExist = false };
    }

    public ParseInfo GetParseInfoByEnvironments(Command command)
    {
        foreach (var info in Environments)
        {
            if (info.Name == command.Arguments[0].Value)
                return info;
        }

        return new ParseInfo
        {
            IsCommandExist = false
        };
    }
}