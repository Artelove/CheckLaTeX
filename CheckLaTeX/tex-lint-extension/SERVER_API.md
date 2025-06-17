# API сервера CheckLaTeX для поддержки диагностик VS Code

## Обновленный формат ответа

Для поддержки подсветки ошибок в редакторе VS Code, сервер должен возвращать дополнительную информацию о местоположении ошибок.

### Текущий формат ответа
```json
{
  "commandsFound": 42,
  "testResults": [
    {
      "testName": "TestQuotationMarks",
      "errors": [
        {
          "type": "WrongQuotationMark",
          "info": "Использование неправильных кавычек",
          "command": "\"неправильные\" кавычки"
        }
      ]
    }
  ],
  "text": "Подробный отчет..."
}
```

### Обновленный формат ответа
```json
{
  "commandsFound": 42,
  "testResults": [
    {
      "testName": "TestQuotationMarks",
      "errors": [
        {
          "type": "WrongQuotationMark",
          "info": "Использование неправильных кавычек вместо «елочек»",
          "command": "\"неправильные\" кавычки",
          "fileName": "main.tex",
          "lineNumber": 45,
          "columnNumber": 12,
          "endLineNumber": 45,
          "endColumnNumber": 26
        },
        {
          "type": "WrongQuotationMark", 
          "info": "Одинарные кавычки вместо правильных",
          "command": "'quotes'",
          "fileName": "chapters/intro.tex",
          "lineNumber": 12,
          "columnNumber": 8,
          "endLineNumber": 12,
          "endColumnNumber": 15
        }
      ]
    },
    {
      "testName": "TestHyphenInsteadOfDash",
      "errors": [
        {
          "type": "WrongDash",
          "info": "Использование дефиса вместо тире",
          "command": "текст - продолжение",
          "fileName": "main.tex",
          "lineNumber": 67,
          "columnNumber": 6,
          "endLineNumber": 67,
          "endColumnNumber": 7
        }
      ]
    }
  ],
  "text": "Подробный отчет..."
}
```

## Новые поля для диагностик

### Обязательные поля для подсветки:
- `fileName` (string) - Относительный путь к файлу от корня проекта
- `lineNumber` (number) - Номер строки (1-based, как в редакторах)
- `columnNumber` (number) - Позиция в строке (1-based)

### Опциональные поля:
- `endLineNumber` (number) - Конечная строка для многострочных ошибок
- `endColumnNumber` (number) - Конечная позиция для точного выделения

## Типы серьезности ошибок

Расширение автоматически определяет серьезность на основе поля `type`:

- **Error** (красная волнистая линия): если `type` содержит "error"
- **Warning** (желтая волнистая линия): по умолчанию
- **Information** (синяя волнистая линия): если `type` содержит "info"

### Примеры типов:
```json
{
  "type": "CriticalError",        // → Error (красная)
  "type": "WrongQuotationMark",   // → Warning (желтая)
  "type": "StyleInfo",            // → Information (синяя)
  "type": "FormatWarning"         // → Warning (желтая)
}
```

## Обработка путей к файлам

### Корректные пути:
```json
{
  "fileName": "main.tex",                    // Файл в корне
  "fileName": "chapters/intro.tex",          // Подпапка
  "fileName": "sections/math/formulas.tex"   // Вложенные папки
}
```

### Некорректные пути (будут игнорированы):
```json
{
  "fileName": "/absolute/path/file.tex",     // Абсолютные пути
  "fileName": "../outside/project.tex",     // Выход за пределы проекта
  "fileName": "C:\\Windows\\file.tex"       // Системные пути
}
```

## Примеры реализации

### C# код для создания диагностической информации:

```csharp
public class DiagnosticInfo
{
    public string Type { get; set; }
    public string Info { get; set; }
    public string Command { get; set; }
    public string FileName { get; set; }
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public int? EndLineNumber { get; set; }
    public int? EndColumnNumber { get; set; }
}

// Пример создания диагностики
public DiagnosticInfo CreateQuotationMarkError(string fileName, int line, int startCol, int endCol, string foundQuote)
{
    return new DiagnosticInfo
    {
        Type = "WrongQuotationMark",
        Info = $"Неправильные кавычки '{foundQuote}'. Используйте «елочки»",
        Command = foundQuote,
        FileName = fileName,
        LineNumber = line,
        ColumnNumber = startCol,
        EndLineNumber = line,
        EndColumnNumber = endCol
    };
}
```

### Интеграция с CommandHandler:

```csharp
public class CommandHandler
{
    private string _currentFileName;
    private List<DiagnosticInfo> _diagnostics = new();
    
    public void ProcessFile(string fileName, string content)
    {
        _currentFileName = fileName;
        
        // Анализ содержимого файла...
        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            CheckQuotationMarks(lines[i], i + 1);
            CheckDashes(lines[i], i + 1);
        }
    }
    
    private void CheckQuotationMarks(string line, int lineNumber)
    {
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
            {
                _diagnostics.Add(new DiagnosticInfo
                {
                    Type = "WrongQuotationMark",
                    Info = "Использование прямых кавычек вместо «елочек»",
                    Command = "\"",
                    FileName = _currentFileName,
                    LineNumber = lineNumber,
                    ColumnNumber = i + 1,
                    EndLineNumber = lineNumber,
                    EndColumnNumber = i + 2
                });
            }
        }
    }
}
```

## Рекомендации для сервера

1. **Всегда используйте относительные пути** от корня проекта
2. **Используйте 1-based индексы** для совместимости с редакторами
3. **Указывайте точные позиции** для лучшего пользовательского опыта
4. **Группируйте ошибки по типам** для организованного отображения
5. **Включайте контекст команды** для понимания проблемы

## Совместимость

Расширение VS Code обратно совместимо - если поля диагностик отсутствуют, ошибки просто отображаются в Output Channel без подсветки в коде. 