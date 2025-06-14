using TexLint.Models;

namespace TexLint.Models;

public class EnvironmentCommand: Command
{
    public string EnvironmentName = string.Empty;

    public Command EndCommand { get; set; }

    public List<Command> InnerCommands { get; set; } = new();

    public EnvironmentCommand(Command current)
        :base(current.FileOwner)
    {
        Name = current.Name;
        StartSymbolNumber = current.StartSymbolNumber;
        StringNumber = current.StringNumber;
        EndSymbolNumber = current.EndSymbolNumber;
        GlobalIndex = current.GlobalIndex;
        Parameters = current.Parameters;
        Arguments = current.Arguments;
        
        // Копируем новые поля позиций
        SourceStartPosition = current.SourceStartPosition;
        SourceEndPosition = current.SourceEndPosition;
        SourceStartLine = current.SourceStartLine;
        SourceStartColumn = current.SourceStartColumn;
        SourceEndLine = current.SourceEndLine;
        SourceEndColumn = current.SourceEndColumn;
    }
}