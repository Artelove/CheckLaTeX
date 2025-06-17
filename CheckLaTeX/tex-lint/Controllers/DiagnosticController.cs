using Microsoft.AspNetCore.Mvc;
using TexLint.Models;
using System.Text;

namespace TexLint.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticController : ControllerBase
{
    /// <summary>
    /// Диагностический тест для проверки работы парсера
    /// </summary>
    /// <param name="request">Запрос с файлами LaTeX</param>
    /// <returns>Диагностическая информация</returns>
    [HttpPost("test")]
    public ActionResult<string> Test([FromBody] LintRequest request)
    {
        try
        {
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(workingDirectory);

            var result = new StringBuilder();
            result.AppendLine($"Working Directory: {workingDirectory}");
            result.AppendLine($"Start File: {request.StartFile}");
            result.AppendLine($"Files Count: {request.Files.Count}");

            foreach (var kv in request.Files)
            {
                var filePath = Path.Combine(workingDirectory, kv.Key);
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(filePath, kv.Value);
                
                result.AppendLine($"Created file: {filePath}");
                result.AppendLine($"File exists: {System.IO.File.Exists(filePath)}");
                result.AppendLine($"File size: {new FileInfo(filePath).Length} bytes");
            }

            // Test file reading
            var startFilePath = Path.Combine(workingDirectory, request.StartFile);
            if (System.IO.File.Exists(startFilePath))
            {
                var content = System.IO.File.ReadAllText(startFilePath);
                result.AppendLine($"File content length: {content.Length}");
                result.AppendLine($"First 100 chars: {content.Substring(0, Math.Min(100, content.Length))}");
            }
            else
            {
                result.AppendLine("Start file does not exist!");
            }

            Directory.Delete(workingDirectory, true);
            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}\nStack: {ex.StackTrace}";
        }
    }
} 