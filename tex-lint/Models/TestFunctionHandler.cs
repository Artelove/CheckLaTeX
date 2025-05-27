using TexLint.TestFunctionClasses;

namespace TexLint.Models;

public class TestFunctionHandler
{
    private List<TestError> Errors = new ();
    
    private string pathToCheckoutFile;
    
    public TestFunctionHandler(List<Command> foundCommands)
    {
        TestUtilities.FoundsCommands = foundCommands;
        
        for (var i = 0; i < foundCommands.Count; i++)
            foundCommands[i].GlobalIndex = i;

        TestUtilities.FoundsCommandsWithLstlisting = new();
        TestUtilities.FoundsCommandsWithLstlisting.AddRange(foundCommands);
        TestUtilities.FoundsCommandsWithLstlisting.AddRange(TestUtilities.GetAllCommandsLikeParametersFromLstlisting());
        TestCiteToBibItems testCiteToBibItems = new ();
        TestEnvironmentLabelToRefs testEnvironmentLabelToRefs = new ();
        TestEnvironmentWithItemsCommand testEnvironmentWithItemsCommand = new();
        TestHyphenInsteadOfDash testHyphenInsteadOfDash = new();
        TestQuotationMarks testQuotationMarks = new();
        TestCaptionNextToRef testCaptionNextToRef = new();
        
        Errors.AddRange(testCiteToBibItems.Errors);
        Errors.AddRange(testEnvironmentLabelToRefs.Errors);
        Errors.AddRange(testEnvironmentWithItemsCommand.Errors);
        Errors.AddRange(testHyphenInsteadOfDash.Errors);
        Errors.AddRange(testQuotationMarks.Errors);
        
        //Errors.AddRange(testCaptionNextToRef.Errors);
        
        pathToCheckoutFile = TestUtilities.StartDirectory +
                             "\\CheckReports\\"
                             + "CheckLatex#" +
                             DateTime.Now.Hour + "-" +
                             DateTime.Now.Minute + string.Empty + " (" +
                             DateTime.Now.Day + "_" +
                             DateTime.Now.Month + "_" +
                             DateTime.Now.Year + ")" + ".txt";
        
        Directory.CreateDirectory(TestUtilities.StartDirectory + "\\CheckReports");
        
        var streamWriter = new StreamWriter(pathToCheckoutFile);
        
        var text = string.Empty;
        foreach (var error in Errors)
        {
            error.ConsolePrint();
            text+=error+"\n";
        }

        text += $"\nОшибки типа cite bib: {testCiteToBibItems.Errors.Count}";
        text += $"\nОшибки label to ref cite bib: {testEnvironmentLabelToRefs.Errors.Count}";
        text += $"\nОшибки items enviroment : {testEnvironmentWithItemsCommand.Errors.Count}";
        text += $"\nОшибки dash : {testHyphenInsteadOfDash.Errors.Count}";
        text += $"\nОшибки quo marks: {testQuotationMarks.Errors.Count}";
        //text += $"\nОшибки caption: {testCaptionNextToRef.Errors.Count}";

        streamWriter.WriteLine(text);
        streamWriter.Close();
        Console.WriteLine($"\n\nРезультат проверки находится по пути: {pathToCheckoutFile}");
        if(Errors.Count == 0)
            Console.WriteLine("Данный документ не имеет искомых ошибок");
        
    }
}