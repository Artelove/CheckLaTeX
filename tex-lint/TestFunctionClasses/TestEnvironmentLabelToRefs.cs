using TexLint.Models;

namespace TexLint.TestFunctionClasses;

public class TestEnvironmentLabelToRefs : TestFunction
{
    public TestEnvironmentLabelToRefs()
    {
        var refs = TestUtilities.GetAllCommandsByName("ref");
        var eqrefs = TestUtilities.GetAllCommandsByName("eqref");
        var labels = TestUtilities.GetAllCommandsByName("label");
        
        var refsArguments = new Dictionary<string, Command>();
        var labelsArguments = new Dictionary<string, Command>();

        foreach (var _ref in refs)
        {
            foreach (var arg in _ref.Arguments)
            {
                refsArguments.TryAdd(arg.Value, _ref);
            }
        }

        foreach (var _eqref in eqrefs)
        {
            foreach (var arg in _eqref.Arguments)
            {
                refsArguments.TryAdd(arg.Value, _eqref);
            }
        }
        
        foreach (var label in labels)
        {
            foreach (var arg in label.Arguments.Where(arg => labelsArguments.TryAdd(arg.Value, label) == false))
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Error,
                    ErrorCommand = label,
                    ErrorInfo = $"Название окружения \\label[{arg.Value}] уже использовалось ранее "
                });
            }
        }

        bool match;
        foreach (var _refArg in refsArguments)
        {
            match = labelsArguments.Any(label => _refArg.Key == label.Key);

            if (match == false)
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Error,
                    ErrorCommand = _refArg.Value,
                    ErrorInfo = $"Указанная ссылка \\ref[{_refArg.Key}] не ведет ни к одному окружению."
                });
            }
        }
        
        foreach (var label in labelsArguments)
        {
            match = refsArguments.Any(_refArg => _refArg.Key == label.Key);

            if (match == false)
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Error,
                    ErrorCommand = label.Value,
                    ErrorInfo = $"Окружение с названием \\label[{label.Key}] не имеет ссылки в документе."
                });
            }
        }
    }

    private bool IsEnvironmentShouldHaveLabel(Command label)
    {
        var begins = TestUtilities.GetAllEnvironment();
        
        foreach (var begin in begins)
        {
            foreach (var item in begin.InnerCommands)
            {
                if (item.GlobalIndex == label.GlobalIndex)
                {
                    if (begin.Name == "figure" ||
                        begin.Name == "lstlisting" ||
                        begin.Name == "equation" ||
                        begin.Name == "table")
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}