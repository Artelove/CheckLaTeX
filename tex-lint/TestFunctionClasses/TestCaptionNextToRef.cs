using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

public class TestCaptionNextToRef: TestFunction
{
    // ( Листинг \ref{1} ).
    // листинг \ref{1}.
    // * . листинг \ref{1}.
    // * Листинг \ref{1}.
    // * Листинг \ref{1}.
    // * ( листинг \ref{1} ).
    public TestCaptionNextToRef(ILatexConfigurationService configurationService, string requestId)
        : base(configurationService, requestId)
    {
        var refs = GetAllCommandsByName("ref");
        var textBefore = new Dictionary<Command, string>();
        var textAfter = new Dictionary<Command, string>();
        
        var counter = 1;
        
        foreach (var _ref in refs)
        {
            var first = string.Empty;
            var second = string.Empty;
            
            for (var i = _ref.GlobalIndex - 1; i >= 0; i--)
            {
                if (FoundsCommands[i] is TextCommand textCommand)
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
            
            for (var i =_ref.GlobalIndex; i < FoundsCommands.Count; i++)
            {
                if (FoundsCommands[i] is TextCommand textCommand)
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