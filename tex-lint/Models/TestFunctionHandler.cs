using TexLint.TestFunctionClasses;
using System.IO; 
using System; 
using System.Collections.Generic; 
using System.Linq; 

namespace TexLint.Models;

public class TestFunctionHandler
{
    public List<TestError> Errors { get; private set; } = new List<TestError>(); 
    
    private string? pathToCheckoutFile; // Made nullable
    
    public TestFunctionHandler(List<Command> foundCommands)
    {
        // TestUtilities.FoundsCommands is expected to be set by the caller (LintController)
        // before this constructor is called.
        // TestUtilities.FoundsCommands = foundCommands ?? new List<Command>(); 
        
        // Ensure foundCommands itself is not null before iterating
        if (foundCommands == null) foundCommands = new List<Command>();

        for (var i = 0; i < foundCommands.Count; i++)
            if (TestUtilities.FoundsCommands[i] != null) TestUtilities.FoundsCommands[i].GlobalIndex = i;

        TestUtilities.FoundsCommandsWithLstlisting = new List<Command>(); 
        if (TestUtilities.FoundsCommands != null) { // Ensure FoundsCommands is not null before Where
            TestUtilities.FoundsCommandsWithLstlisting.AddRange(TestUtilities.FoundsCommands.Where(c => c != null));
        }
        
        // Ensure FoundsCommands is not null and has items before calling a method that might iterate over it
        if (TestUtilities.FoundsCommands != null && TestUtilities.FoundsCommands.Any()) { 
             TestUtilities.FoundsCommandsWithLstlisting.AddRange(TestUtilities.GetAllCommandsLikeParametersFromLstlisting());
        }
        
        TestCiteToBibItems testCiteToBibItems = new TestCiteToBibItems(); 
        TestEnvironmentLabelToRefs testEnvironmentLabelToRefs = new TestEnvironmentLabelToRefs(); 
        TestEnvironmentWithItemsCommand testEnvironmentWithItemsCommand = new TestEnvironmentWithItemsCommand(); 
        TestHyphenInsteadOfDash testHyphenInsteadOfDash = new TestHyphenInsteadOfDash(); 
        TestQuotationMarks testQuotationMarks = new TestQuotationMarks(); 

        if (testCiteToBibItems.Errors != null) Errors.AddRange(testCiteToBibItems.Errors);
        if (testEnvironmentLabelToRefs.Errors != null) Errors.AddRange(testEnvironmentLabelToRefs.Errors);
        if (testEnvironmentWithItemsCommand.Errors != null) Errors.AddRange(testEnvironmentWithItemsCommand.Errors);
        if (testHyphenInsteadOfDash.Errors != null) Errors.AddRange(testHyphenInsteadOfDash.Errors);
        if (testQuotationMarks.Errors != null) Errors.AddRange(testQuotationMarks.Errors);
        
        if (!string.IsNullOrEmpty(TestUtilities.StartDirectory)) 
        {
            try 
            {
                string reportsDir = Path.Combine(TestUtilities.StartDirectory, "CheckReports");
                Directory.CreateDirectory(reportsDir); 
            
                pathToCheckoutFile = Path.Combine(reportsDir, 
                                     $"CheckLatex#{DateTime.Now.Hour}-{DateTime.Now.Minute} ({DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}).txt");
        
                using (var streamWriter = new StreamWriter(pathToCheckoutFile))
                {
                    var textBuilder = new System.Text.StringBuilder(); 
                    foreach (var error in Errors.Where(e => e != null))
                    {
                        textBuilder.AppendLine(error.ToString()); 
                    }

                    textBuilder.AppendLine($"\\nОшибки типа cite bib: {testCiteToBibItems.Errors?.Count ?? 0}"); 
                    textBuilder.AppendLine($"\\nОшибки label to ref cite bib: {testEnvironmentLabelToRefs.Errors?.Count ?? 0}"); 
                    textBuilder.AppendLine($"\\nОшибки items enviroment : {testEnvironmentWithItemsCommand.Errors?.Count ?? 0}"); 
                    textBuilder.AppendLine($"\\nОшибки dash : {testHyphenInsteadOfDash.Errors?.Count ?? 0}"); 
                    textBuilder.AppendLine($"\\nОшибки quo marks: {testQuotationMarks.Errors?.Count ?? 0}"); 

                    streamWriter.Write(textBuilder.ToString());
                }
            }
            catch (Exception ex) 
            {
                Errors.Add(new TestError { ErrorInfo = "Failed to write report: " + ex.Message, ErrorType = ErrorType.Warning });
            }
        }
    }
}
