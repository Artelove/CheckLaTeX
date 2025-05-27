using TexLint.Models;

namespace TexLint.Controllers;

public class LintController
{
    private readonly CommandHandler _commandHandler;

    public LintController(string startFile, string startDirectory)
    {
        _commandHandler = new CommandHandler(startFile, startDirectory);
    }

    public List<Command> Analyze()
    {
        return _commandHandler.FindAllCommandsInDocument();
    }
}
