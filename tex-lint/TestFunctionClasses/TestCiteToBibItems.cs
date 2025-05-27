using System.Globalization;
using System.Text.RegularExpressions;
using TexLint.Models;

namespace TexLint.TestFunctionClasses;

public class TestCiteToBibItems : TestFunction
{
    private const string PATTERN_ITEM = @"^[a-zA-Z_-]+$";
    private const string PATTERN_KEY = @"^[\da-zA-Zа-яА-ЯёЁ!?#+=:._-]+$";
    private const string PATTERN_END_OF_STRING = @"[\s]";
    
    private readonly Regex _regexItemName = new(PATTERN_ITEM);
    private readonly Regex _regexKey = new(PATTERN_KEY);
    private readonly Regex _regexSpace = new(PATTERN_END_OF_STRING);

    public TestCiteToBibItems()
    {
        //List<Command> cites = new();
        var citeNumberCommand = new Dictionary<string,Command>();
        var cites = TestUtilities.GetAllCommandsByName("cite");
        var bibItems = ParseBibItems();
        
        foreach (var command in cites)
        {
            foreach (var argument in command.Arguments)
            {
                citeNumberCommand.TryAdd(argument.Text,command);
            }
        }

        bool match;
        foreach (var command in citeNumberCommand)
        {
            match = false;
            for (int i = 0; i < bibItems.Count; i++)
            {
                if (command.Key == bibItems[i].Key) 
                {
                    match = true;
                    break;
                }
            }

            if (match == false)
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Error,
                    ErrorCommand = command.Value,
                    ErrorInfo = $"Команда \\cite[{command.Key}] не нашла ссылаемого источника."
                });
            }
        }
        
        foreach (var t in bibItems)
        {
            match = false;
            foreach (var command in citeNumberCommand)
            {
                if (command.Key == t.Key) 
                {
                    match = true;
                    break;
                }
            }

            if (match == false)
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Error,
                    ErrorInfo = $"Библиографический источник  Name:{t.Name} Key:{ t.Key} не нашел ожидаемую команду \\cite с ключем {t.Key}."
                });
            }
        }
        
    }
    
    private List<BibItem> FilterNonGrowthItems()
    {
        var bibItems = ParseBibItems();
        var indexesToDelete = new List<int> ();
        
        for (var i = 0; i < bibItems.Count; i++)
        {
            if (Int32.Parse(bibItems[i].Key) != i + 1)
            {
                indexesToDelete.Add(i);
            }
        }

        for (var i = indexesToDelete.Count - 1; i >= 0; i--)
        {
            bibItems.RemoveAt(indexesToDelete[i]);
        }
        
        return bibItems;
    }
    
    private List<BibItem> ParseBibItems()
    {
        //Указать в файле конфигурации .bib файл
        var bibItems = new List<BibItem>();
        var text = new StreamReader(TestUtilities.StartDirectory+"\\"+"Bib.bib").ReadToEnd();
        
        var readBibItem = false;
        var readKey = false;
        
        var item = new BibItem();
        
        foreach (var t in text)
        {
            if (_regexSpace.Match(t.ToString()).Success)
                continue;
            if (readKey)
            {
                if (_regexKey.Match(item.Key + t).Success)
                {
                    item.Key += t;
                    continue;
                }
                else
                {
                    bibItems.Add(item);
                    item = new BibItem();
                    readBibItem = false;
                    readKey = false;
                    continue;
                }
            }

            if (readBibItem)
            {
                if (_regexItemName.Match(item.Name + t).Success)
                    item.Name += t;
                else
                {
                    readKey = true;
                    readBibItem = false;
                }
            }
            else
            {
                if (t == '@')
                {
                    readBibItem = true;
                }
            }
        }

        return bibItems;
    }
}

class BibItem
{
    public string Name = string.Empty;
    public string Key = string.Empty;
}