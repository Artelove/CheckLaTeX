using Microsoft.AspNetCore.Mvc;
using TexLint.Models;
using TexLint.TestFunctionClasses; // Required for TestUtilities
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO; 
using System; 
using System.Linq;

namespace TexLint.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LintController : ControllerBase
    {
        [HttpPost]
        [Route("analyze")]
        public async Task<IActionResult> Lint([FromBody] LintRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body cannot be null.");
            }
            if (string.IsNullOrEmpty(request.DocumentContent))
            {
                return BadRequest("DocumentContent cannot be empty.");
            }

            string tempDirectory = Path.Combine(Path.GetTempPath(), "texlint_" + Guid.NewGuid().ToString());
            
            string tempFileNameWithExt = "document.tex"; // Default filename
            if (!string.IsNullOrWhiteSpace(request.FilePath))
            {
                string originalFileName = Path.GetFileName(request.FilePath);
                if (!string.IsNullOrWhiteSpace(originalFileName)) 
                {
                    // Sanitize to prevent path traversal issues and invalid characters
                    foreach (char invalidChar in Path.GetInvalidFileNameChars())
                    {
                        originalFileName = originalFileName.Replace(invalidChar.ToString(), "_");
                    }
                    // Ensure it has .tex extension if it's not just a directory path
                    if (!string.IsNullOrWhiteSpace(Path.GetExtension(originalFileName)))
                    {
                        tempFileNameWithExt = originalFileName;
                    }
                    else
                    {
                         tempFileNameWithExt = originalFileName + ".tex";
                    }
                }
            }
            // If request.FilePath was a directory, tempFileNameWithExt might be just ".tex". Handle this.
            if (tempFileNameWithExt == ".tex")
            {
                tempFileNameWithExt = "document.tex";
            }
            
            string tempFilePath = Path.Combine(tempDirectory, tempFileNameWithExt);
            List<TestError> errors = new List<TestError>(); 

            try
            {
                Directory.CreateDirectory(tempDirectory);
                await System.IO.File.WriteAllTextAsync(tempFilePath, request.DocumentContent);

                // Create a dummy Bib.bib file if it's expected by TestCiteToBibItems, 
                // to prevent errors if it's not part of the input.
                // This is a common pattern for tools that might look for auxiliary files.
                string dummyBibPath = Path.Combine(tempDirectory, "Bib.bib");
                if (!File.Exists(dummyBibPath)) // Only create if not already there (e.g. if included in a zip in future)
                {
                    await System.IO.File.WriteAllTextAsync(dummyBibPath, "% Empty Bib.bib file for tex-lint\n");
                }


                TestUtilities.StartDirectory = tempDirectory; 
                
                CommandHandler commandHandler = new CommandHandler(tempFileNameWithExt, tempDirectory);
                List<Command> commands = commandHandler.FindAllCommandsInDocument();
                
                // TestFunctionHandler constructor sets TestUtilities.FoundsCommands.
                TestFunctionHandler testFunctionHandler = new TestFunctionHandler(commands);
                errors = testFunctionHandler.Errors;

                return Ok(errors); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during linting: {ex.ToString()}");
                // Return a more structured error
                return StatusCode(500, new List<TestError> { new TestError { ErrorInfo = $"Internal server error: {ex.Message}", ErrorType = ErrorType.Error }});
            }
            finally
            {
                // Clean up static fields to prevent state leakage between requests as much as possible
                // This is a workaround for the static design. A proper fix would involve DI and instance-based services.
                TestUtilities.StartDirectory = string.Empty; 
                TestUtilities.FoundsCommands = new List<Command>();
                TestUtilities.FoundsCommandsWithLstlisting = new List<Command>();

                if (Directory.Exists(tempDirectory))
                {
                    try
                    {
                        Directory.Delete(tempDirectory, true); 
                    }
                    catch (IOException ioEx)
                    {
                        Console.WriteLine($"Error deleting temporary directory {tempDirectory}: {ioEx.Message}");
                        // Optionally, log this to a more persistent logging system in a real application
                    }
                }
            }
        }
    }
}
