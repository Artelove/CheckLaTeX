using System.Globalization;
using System.Text.RegularExpressions;
using TexLint.Models;
using System.Collections.Generic; 
using System.IO; 
using System; 
using System.Linq;

namespace TexLint.TestFunctionClasses;

public class TestCiteToBibItems : TestFunction
{
    private const string PATTERN_ITEM = @"^[a-zA-Z_-]+$";
    private const string PATTERN_KEY = @"^[\da-zA-Zа-яА-ЯёЁ!?#+=:._-]+$";
    private const string PATTERN_END_OF_STRING = @"[\s]";
    
    private readonly Regex _regexItemName = new Regex(PATTERN_ITEM); 
    private readonly Regex _regexKey = new Regex(PATTERN_KEY); 
    private readonly Regex _regexSpace = new Regex(PATTERN_END_OF_STRING); 

    public TestCiteToBibItems()
    {
        var citeNumberCommand = new Dictionary<string,Command>();
        var cites = TestUtilities.GetAllCommandsByName("cite") ?? new List<Command>();
        var bibItems = ParseBibItems();
        
        foreach (var command in cites)
        {
            if (command == null || command.Arguments == null) continue;
            foreach (var argument in command.Arguments)
            {
                if (argument != null && argument.Text != null) { 
                    citeNumberCommand.TryAdd(argument.Text,command);
                }
            }
        }

        bool matchFound; 
        foreach (var commandEntry in citeNumberCommand) 
        {
            matchFound = false;
            for (int i = 0; i < bibItems.Count; i++)
            {
                if (commandEntry.Key == bibItems[i].Key) 
                {
                    matchFound = true;
                    break;
                }
            }

            if (matchFound == false)
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Error,
                    ErrorCommand = commandEntry.Value,
                    ErrorInfo = $"Команда \\cite[{commandEntry.Key}] не нашла ссылаемого источника."
                });
            }
        }
        
        foreach (var bibItemEntry in bibItems) 
        {
            matchFound = false;
            foreach (var commandEntry in citeNumberCommand)
            {
                if (commandEntry.Key == bibItemEntry.Key) 
                {
                    matchFound = true;
                    break;
                }
            }

            if (matchFound == false)
            {
                Errors.Add(new TestError()
                {
                    ErrorType = ErrorType.Warning, 
                    ErrorCommand = null, 
                    ErrorInfo = $"Библиографический источник Name:{bibItemEntry.Name} Key:{bibItemEntry.Key} не используется в документе командой \\cite."
                });
            }
        }
    }
    
    private List<BibItem> FilterNonGrowthItems()
    {
        var bibItems = ParseBibItems();
        var indexesToDelete = new List<int>(); 
        
        for (var i = 0; i < bibItems.Count; i++)
        {
            if (Int32.TryParse(bibItems[i].Key, out int keyNumber)) 
            {
                 if (keyNumber != i + 1)
                 {
                    indexesToDelete.Add(i);
                 }
            } else {
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
        var bibItems = new List<BibItem>();
        var bibFilePath = Path.Combine(TestUtilities.StartDirectory ?? string.Empty, "Bib.bib");

        if (!File.Exists(bibFilePath)) {
            // ErrorType.Critical does not exist based on previous build errors. Using ErrorType.Error.
            Errors.Add(new TestError { 
                ErrorInfo = "Bib.bib file not found at: " + bibFilePath, 
                ErrorType = ErrorType.Error 
            });
            return bibItems;
        }
        var text = File.ReadAllText(bibFilePath);
        
        var readBibItemName = false;
        var readBibItemKey = false;
        
        BibItem currentItem = new BibItem();
        
        for(int i = 0; i < text.Length; i++)
        {
            char ch = text[i];

            if (ch == '@')
            {
                if (!string.IsNullOrEmpty(currentItem.Name) && !string.IsNullOrEmpty(currentItem.Key))
                {
                    if (!bibItems.Any(b => b.Key == currentItem.Key)) bibItems.Add(currentItem);
                }
                currentItem = new BibItem();
                readBibItemName = true;
                readBibItemKey = false;
                continue;
            }

            if (readBibItemName)
            {
                if (ch == '{')
                {
                    readBibItemName = false;
                    readBibItemKey = true;
                    continue;
                }
                if (!char.IsWhiteSpace(ch)) 
                {
                     currentItem.Name += ch;
                }
            }
            else if (readBibItemKey)
            {
                if (ch == ',')
                {
                    readBibItemKey = false; 
                    if (!string.IsNullOrEmpty(currentItem.Name) && !string.IsNullOrEmpty(currentItem.Key))
                    {
                         if (!bibItems.Any(b => b.Key == currentItem.Key)) bibItems.Add(currentItem);
                    }
                    readBibItemName = false; 
                    continue;
                }
                 if (!char.IsWhiteSpace(ch)) 
                 {
                    currentItem.Key += ch;
                 }
            }
        }
        if (!string.IsNullOrEmpty(currentItem.Name) && !string.IsNullOrEmpty(currentItem.Key) && !bibItems.Any(b => b.Key == currentItem.Key))
        {
            bibItems.Add(currentItem);
        }
        return bibItems;
    }
}

class BibItem 
{
    public string Name {get; set;} = string.Empty;
    public string Key {get; set;} = string.Empty;
}
