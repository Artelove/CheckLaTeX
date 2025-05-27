using TexLint.Models; 
using System.Collections.Generic; 
using System.Linq;

namespace TexLint.Models;

public class EnvironmentCommand: Command
{
    public string EnvironmentName { get; set; } = string.Empty;
    public Command? EndCommand { get; set; } 
    public List<Command> InnerCommands { get; set; } = new List<Command>(); 

    public EnvironmentCommand(Command beginCommand)
    {
        Name = beginCommand.Name; 
        FileOwner = beginCommand.FileOwner;
        StringNumber = beginCommand.StringNumber;
        StartSymbolNumber = beginCommand.StartSymbolNumber;
        EndSymbolNumber = beginCommand.EndSymbolNumber; 
        GlobalIndex = beginCommand.GlobalIndex;
        
        if (beginCommand.Arguments != null && beginCommand.Arguments.Any() && beginCommand.Arguments[0] != null)
        {
            EnvironmentName = beginCommand.Arguments[0].Value ?? string.Empty;
        }
        else
        {
            EnvironmentName = "unknown"; 
        }

        Parameters = new List<Parameter>(beginCommand.Parameters?.Where(p => p != null).ToList() ?? Enumerable.Empty<Parameter>()); 
        
        if (Name == "begin" && beginCommand.Arguments != null && beginCommand.Arguments.Count > 1) {
             Arguments = new List<Parameter>(beginCommand.Arguments.Skip(1).Where(arg => arg != null).ToList()); 
        } else if (Name != "begin" && beginCommand.Arguments != null) {
             Arguments = new List<Parameter>(beginCommand.Arguments.Where(arg => arg != null).ToList()); 
        } else {
            Arguments = new List<Parameter>(); 
        }
    }

    public EnvironmentCommand() : base() { 
        EnvironmentName = "unknown"; 
    }
}
