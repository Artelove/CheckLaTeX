using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using TexLint.Models;
using TexLint.Models.HandleInfos;

namespace TexLint.TestFunctionClasses;

public class TestCiteToBibItems : TestFunction
{
    private const string PATTERN_ITEM = @"^[a-zA-Z_-]+$";
    private const string PATTERN_KEY = @"^[\da-zA-Zа-яА-ЯёЁ!?#+=:._-]+$";
    private const string PATTERN_END_OF_STRING = @"[\s]";
    
    private readonly Regex _regexItemName = new(PATTERN_ITEM);
    private readonly Regex _regexKey = new(PATTERN_KEY);
    private readonly Regex _regexSpace = new(PATTERN_END_OF_STRING);
    private readonly BibliographyRule _bibliographyConfig;

    public TestCiteToBibItems(ILatexConfigurationService configurationService, string requestId) 
        : base(configurationService, requestId)
    {
        // Загружаем конфигурацию из lint-rules.json
        var lintRulesJson = File.ReadAllText(PathToLintRulesJson);
        var lintRules = JsonSerializer.Deserialize<LintRules>(lintRulesJson);
        _bibliographyConfig = lintRules?.Bibliography ?? new BibliographyRule();
        
        RunCheck();
    }
    
    private void RunCheck()
    {
        var citeNumberCommand = new Dictionary<string, Command>();
        var cites = GetAllCommandsByName("cite");
        var bibItems = ParseBibItems();
        
        // Собираем все цитирования
        foreach (var command in cites)
        {
            foreach (var argument in command.Arguments)
            {
                if (!string.IsNullOrEmpty(argument.Text))
                {
                    citeNumberCommand.TryAdd(argument.Text, command);
                }
            }
        }

        // Проверяем, что каждое цитирование имеет соответствующий библиографический источник
        if (_bibliographyConfig.CheckMissingCitations)
        {
            foreach (var citePair in citeNumberCommand)
            {
                var citationKey = citePair.Key;
                var citeCommand = citePair.Value;
                var match = bibItems.Any(bibItem => bibItem.Key == citationKey);

                if (!match)
                {
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Error,
                        $"Команда \\cite[{citationKey}] не нашла ссылаемого источника",
                        citeCommand.FileOwner ?? "unknown.tex",
                        citeCommand.StringNumber,
                        citeCommand.SourceStartColumn,
                        citeCommand.ToString(),
                        suggestedFix: $"Добавьте библиографический источник с ключом '{citationKey}' в .bib файл",
                        errorCommand: citeCommand
                    ));
                }
            }
        }
        
        // Проверяем, что каждый библиографический источник используется
        if (_bibliographyConfig.CheckUnusedSources)
        {
            foreach (var bibItem in bibItems)
            {
                var match = citeNumberCommand.ContainsKey(bibItem.Key);

                if (!match)
                {
                    // Создаем ошибку без привязки к конкретной команде, т.к. проблема в отсутствии использования
                    Errors.Add(TestError.CreateWithDiagnostics(
                        ErrorType.Warning,
                        $"Библиографический источник '{bibItem.Name}' с ключом '{bibItem.Key}' не используется в документе",
                        _bibliographyConfig.BibFilePath,
                        bibItem.LineNumber,
                        1,
                        $"@{bibItem.Name}{{{bibItem.Key}",
                        suggestedFix: $"Удалите неиспользуемый источник или добавьте \\cite{{{bibItem.Key}}} в текст"
                    ));
                }
            }
        }
    }

    
    private List<BibItem> ParseBibItems()
    {
        var bibItems = new List<BibItem>();
        
        try
        {
            var bibFilePath = Path.Combine(StartDirectory, _bibliographyConfig.BibFilePath);
            if (!File.Exists(bibFilePath))
            {
                //Попробовать найти другой файл в проекте с .bib
                var bibFiles = Directory.GetFiles(StartDirectory, "*.bib");
                if (bibFiles.Length > 0)
                {
                    bibFilePath = bibFiles[0];
                }
                else
                
                return bibItems;
            }
            
            var lines = File.ReadAllLines(bibFilePath);
            var readBibItem = false;
            var readKey = false;
            var item = new BibItem();
            
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                
                foreach (var t in line)
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
                            // Устанавливаем номер строки (1-based)
                            item.LineNumber = lineIndex + 1;
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
                            // Запоминаем начальную строку для текущего элемента
                            item.LineNumber = lineIndex + 1;
                        }
                    }
                }
            }
            
            // Добавляем последний элемент, если он остался необработанным
            if (!string.IsNullOrEmpty(item.Name) || !string.IsNullOrEmpty(item.Key))
            {
                bibItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            // Если произошла ошибка при чтении файла, добавляем ошибку в список
            Errors.Add(TestError.CreateWithDiagnostics(
                ErrorType.Error,
                $"Не удалось прочитать файл библиографии: {ex.Message}",
                _bibliographyConfig.BibFilePath,
                1,
                1,
                "",
                suggestedFix: "Проверьте существование и доступность .bib файла"
            ));
        }

        return bibItems;
    }
}

class BibItem
{
    public string Name = string.Empty;
    public string Key = string.Empty;
    public int LineNumber = 1;
}