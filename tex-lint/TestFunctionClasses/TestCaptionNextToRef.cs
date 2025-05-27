using TexLint.Models;

namespace TexLint.TestFunctionClasses;

public class TestCaptionNextToRef: TestFunction
{
    // ( Листинг \ref{1} ).
    // листинг \ref{1}.
    // * . листинг \ref{1}.
    // * Листинг \ref{1}.
    // * Листинг \ref{1}.
    // * ( листинг \ref{1} ).
    public TestCaptionNextToRef()
    {
        var refs = TestUtilities.GetAllCommandsByName("ref");
        var textBefore = new Dictionary<Command, string>();
        var textAfter = new Dictionary<Command, string>();
        
        var counter = 1;
        
        foreach (var _ref in refs)
        {
            var first = string.Empty;
            var second = string.Empty;
            
            for (var i = _ref.GlobalIndex - 1; i >= 0; i--)
            {
                if (TestUtilities.FoundsCommands[i] is TextCommand textCommand)
                {
                    if (first != string.Empty)
                    {
                        second = textCommand.Text;
                        textBefore[_ref] = second + first;
                        break;
                    }
                    
                    first = textCommand.Text;
                }
            }
            
            first = string.Empty;
            second = string.Empty;
            
            for (var i =_ref.GlobalIndex; i < TestUtilities.FoundsCommands.Count; i++)
            {
                if (TestUtilities.FoundsCommands[i] is TextCommand textCommand)
                {
                    if (first != string.Empty)
                    {
                        second = textCommand.Text;
                        textAfter[_ref] = first + second;
                        break;
                    }
                    
                    first = textCommand.Text;
                }
            }
            //Console.WriteLine($"{counter}\n{textBefore[_ref]}\\ref{{{_ref.Arguments[0].Value}}}{textAfter[_ref]}");
            counter++;
        }   
    }
}