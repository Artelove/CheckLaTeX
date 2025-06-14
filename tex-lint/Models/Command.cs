using System.Text.Json;

namespace TexLint.Models;

public class Command
{
    /// <summary>
    /// Имя команды
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Имя файла, в котором была найдена команда
    /// </summary>
    public string FileOwner = string.Empty;

    /// <summary>
    /// Номер строки, в которой была найдена команда
    /// </summary>
    
    public int StringNumber;
    /// <summary>
    /// Номер символа, с которого начинается команда
    /// /// </summary>
    public int StartSymbolNumber;

    /// <summary>
    /// Номер символа, с которого заканчивается команда
    /// /// </summary>
    public int EndSymbolNumber;

    /// <summary>
    /// Позиция начала команды в исходном тексте файла (абсолютная позиция)
    /// </summary>
    public int SourceStartPosition;

    /// <summary>
    /// Позиция конца команды в исходном тексте файла (абсолютная позиция)
    /// </summary>
    public int SourceEndPosition;

    /// <summary>
    /// Номер строки начала команды в исходном файле (1-based)
    /// </summary>
    public int SourceStartLine;

    /// <summary>
    /// Номер столбца начала команды в исходном файле (1-based)
    /// </summary>
    public int SourceStartColumn;

    /// <summary>
    /// Номер строки конца команды в исходном файле (1-based)
    /// </summary>
    public int SourceEndLine;

    /// <summary>
    /// Номер столбца конца команды в исходном файле (1-based)
    /// </summary>
    public int SourceEndColumn;

    /// <summary>
    /// Номер файла, в котором была найдена команда
    /// /// </summary>
    public int FileIndex;

    /// <summary>
    /// Номер команды в файле
    /// </summary>
    public int GlobalIndex;

    /// <summary>
    /// Параметры команды
    /// </summary>
    public List<Parameter> Parameters = new();

    /// <summary>
    /// Аргументы команды
    /// /// </summary>
    public List<Parameter> Arguments = new();

    public Command(string fileOwner)
    {
        FileOwner = fileOwner;
    }

    /// <summary>
    /// Текстовый конструктор команды
    /// </summary>
    public override string ToString()
    {
        return $"\\{Name}{ParamsToString(Parameters, '[', ']', "=", ',')}{ParamsToString(Arguments, '{', '}', ":", ',')}";
    }

    private string ParamsToString(List<Parameter> list, char open, char close, string valueSeparator, char itemSeparator)
    {
        if (list.Count == 0)
            return string.Empty;

        var str = open.ToString();
        for (int i = 0; i < list.Count; i++)
        {
            str += list[i].Text;
            if (list[i].Value is not null)
            {
                str += valueSeparator;
                str += list[i].Value;
            }
            if (i < list.Count - 1)
                str += itemSeparator;
        }
        str += close;
        return str;
    }
}

