using TexLint.Models;

namespace TexLint.Models;

public class EnvironmentCommand: Command
{
    public string EnvironmentName = string.Empty;

    public Command EndCommand { get; set; }

    public List<Command> InnerCommands { get; set; } = new();

    public EnvironmentCommand(Command current)
    {
        Name = current.Name;
        FileOwner = current.FileOwner;
        StartSymbolNumber = current.StartSymbolNumber;
        StringNumber = current.StringNumber;
        EndSymbolNumber = current.EndSymbolNumber;
        GlobalIndex = current.GlobalIndex;
        Parameters = current.Parameters;
        Arguments = current.Arguments;
    }
}