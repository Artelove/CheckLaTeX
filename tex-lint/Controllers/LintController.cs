using Microsoft.AspNetCore.Mvc;
using TexLint.Models;
using System.Text;

namespace TexLint.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LintController : ControllerBase
{
    [HttpPost]
    public ActionResult<string> Analyze([FromBody] LintRequest request)
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(workingDirectory);

        foreach (var kv in request.Files)
        {
            var filePath = Path.Combine(workingDirectory, kv.Key);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(filePath, kv.Value);
        }

        var handler = new CommandHandler(request.StartFile, workingDirectory);
        var commands = handler.FindAllCommandsInDocument();
        var builder = new StringBuilder();
        foreach (var command in commands)
        {
            builder.Append(command.ToString());
        }

        Directory.Delete(workingDirectory, true);
        return builder.ToString();
    }
}
