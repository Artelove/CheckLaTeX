# CheckLaTeX Project Architecture & Development Rules

## Общие принципы разработки

Ты - эксперт C# разработчик, специализирующийся на создании инструментов для анализа и проверки LaTeX документов. Следуй этим принципам:

### Архитектурные принципы проекта
- **LaTeX Document Processing**: Всегда учитывай специфику LaTeX синтаксиса при разработке парсеров
- **Rule-Based Validation**: Используй конфигурационный подход через `lint-rules.json` для определения правил проверки
- **Extensible Design**: Проектируй с возможностью легкого добавления новых правил проверки
- **Performance**: Оптимизируй для обработки больших LaTeX документов
- **Посимвольный парсинг**: Используй character-by-character parsing для точного анализа LaTeX

### Технологический стек
- **.NET 6.0** - базовая платформа
- **ASP.NET Core Web API** для HTTP endpoints
- **JSON Configuration** для правил линтинга через `lint-rules.json`
- **LaTeX Parsing** - кастомные парсеры для анализа LaTeX синтаксиса
- **Regex validation** для проверки команд и параметров

## Структура проекта CheckLaTeX

### Слои архитектуры:
```
CheckLaTeX/
├── tex-lint/                    # Основное приложение
│   ├── Controllers/             # Web API контроллеры
│   │   ├── LintController.cs    # POST /api/lint - основной анализ
│   │   └── DiagnosticController.cs # POST /api/diagnostic/test - диагностика
│   ├── Models/                  # Модели данных и парсеры
│   │   ├── CommandHandler.cs    # Центральный LaTeX парсер (624 строки)
│   │   ├── Command.cs           # Модель LaTeX команды
│   │   ├── TextCommand.cs       # Модель текстового блока
│   │   ├── EnvironmentCommand.cs # Модель окружения LaTeX
│   │   ├── TestFunctionHandler.cs # Orchestrator тестовых функций
│   │   └── HandleInfos/         # Информационные модели
│   └── TestFunctionClasses/     # Конкретные правила проверки
│       ├── TestQuotationMarks.cs     # Проверка кавычек
│       ├── TestHyphenInsteadOfDash.cs # Проверка дефисов vs тире
│       ├── TestCiteToBibItems.cs     # Проверка библиографии
│       ├── TestEnvironmentLabelToRefs.cs # Проверка label ↔ ref
│       └── TestEnvironmentWithItemsCommand.cs # Проверка списков
├── lint-rules.json              # Конфигурация правил
├── test-*.ps1                   # PowerShell скрипты тестирования
└── Нирки/                       # 📂 Реальные LaTeX документы для тестирования
    ├── NIR_*/                   # НИР студентов 
    ├── VKR_*/                   # Выпускные квалификационные работы
    └── */                       # Дипломные и курсовые работы
```

## Последовательность обработки .tex файлов

### 1. API Request Processing
```csharp
[HttpPost]
public ActionResult<string> Analyze([FromBody] LintRequest request)
{
    // 1. Создание временной рабочей директории
    var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(workingDirectory);
    
    // 2. Сохранение всех файлов из запроса
    foreach (var kv in request.Files)
    {
        var filePath = Path.Combine(workingDirectory, kv.Key);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, kv.Value);
    }
    
    // 3. Инициализация парсера
    var handler = new CommandHandler(request.StartFile, workingDirectory);
    
    // 4. Парсинг всех команд
    var commands = handler.FindAllCommandsInDocument();
    
    // 5. Очистка
    Directory.Delete(workingDirectory, true);
    return commands.ToString();
}
```

### 2. LaTeX Document Parsing (CommandHandler)

#### Ключевые константы парсера:
```csharp
private const string PATTERN_COMMAND_NAME = @"^[a-zA-Z]+$";
private const string PATTERN_PARAMETER = @"^[а-яА-ЯёЁa-zA-Z0-9!?.:_-]+$";
private const string PATTERN_WHITE_SPACE = @"^\s+$";
private const string PATTERN_END_OF_STRING = @"\s";
```

#### Алгоритм парсинга:
```csharp
public List<Command> FindAllCommandsInDocument()
{
    // 1. Парсинг основного файла
    var foundCommands = FindAllCommandsInFile(StartDirectory + "\\" + StartFile);
    
    // 2. Рекурсивная обработка \input и \include команд
    for (int i = 0; i < foundCommands.Count; i++)
    {
        if (foundCommands[i].Name == "input" || foundCommands[i].Name == "include")
        {
            if (!new Regex(".tex|.bib").Match(foundCommands[i].Arguments[0].Value).Success)
                foundCommands[i].Arguments[0].Value += ".tex";
            
            foundCommands = InsertListAtIndex(
                foundCommands,
                FindAllCommandsInFile(StartDirectory + "\\" + foundCommands[i].Arguments[0].Value),
                i,
                false);
        }
    }
    
    return foundCommands;
}
```

#### Посимвольный анализ:
```csharp
private List<Command> FindAllCommandsInFile(string fileName)
{
    var text = new StreamReader(fileName).ReadToEnd();
    var foundCommands = new List<Command>();
    
    // Добавляем терминатор для корректного завершения парсинга
    text += "\n\\";
    
    for (int ch = 0; ch < text.Length; ch++)
    {
        // Обработка комментариев (игнорируем до конца строки)
        if (text[ch] == '%' && (ch-1 < 0 || text[ch-1] != '\\'))
            ignoreWhileStringEnd = true;
            
        if (text[ch] == '\n')
        {
            ignoreWhileStringEnd = false;
            _stringNumber++;
        }
        
        if (ignoreWhileStringEnd) continue;
        
        // Обнаружение команд LaTeX
        if (text[ch] == '\\')
        {
            readCommandSymbols = true;
            // Сохранение текущего текстового блока
            // Начало парсинга новой команды
        }
        
        // Парсинг имени команды и её параметров
    }
}
```

### 3. Command Structure

#### Базовая модель команды:
```csharp
public class Command
{
    public string Name { get; set; }                    // Имя команды без \
    public List<Parameter> Arguments { get; set; }     // Обязательные аргументы {...}
    public List<Parameter> Parameters { get; set; }    // Опциональные параметры [...]
    public int StartSymbolNumber { get; set; }         // Позиция начала в файле
    public int EndSymbolNumber { get; set; }           // Позиция конца в файле
    public string FileOwner { get; set; }              // Файл, содержащий команду
    public int StringNumber { get; set; }              // Номер строки
    public int GlobalIndex { get; set; }               // Глобальный индекс
}
```

#### Специальные типы команд:
```csharp
// Текстовые блоки
public class TextCommand : Command
{
    public const string TEXT_COMMAND_NAME = "TEXT_NAME";
    public string Text { get; set; }
}

// Окружения LaTeX
public class EnvironmentCommand : Command
{
    public string EnvironmentName { get; set; }        // Имя окружения
    public List<Command> InnerCommands { get; set; }   // Вложенные команды
    public Command BeginCommand { get; set; }          // \begin{env}
    public Command EndCommand { get; set; }            // \end{env}
}
```

### 4. Test Function System

#### Базовый интерфейс тестовых функций:
```csharp
public abstract class TestFunction
{
    public List<TestError> Errors { get; protected set; } = new List<TestError>();
}

public class TestError
{
    public Command ErrorCommand { get; set; }
    public ErrorType ErrorType { get; set; }           // Warning, Error, Info
    public string ErrorInfo { get; set; }              // Описание ошибки
}
```

#### Orchestrator тестовых функций:
```csharp
public class TestFunctionHandler
{
    public TestFunctionHandler(List<Command> foundCommands)
    {
        TestUtilities.FoundsCommands = foundCommands;
        
        // Инициализация всех тестовых функций
        var testCiteToBibItems = new TestCiteToBibItems();
        var testEnvironmentLabelToRefs = new TestEnvironmentLabelToRefs();
        var testEnvironmentWithItemsCommand = new TestEnvironmentWithItemsCommand();
        var testHyphenInsteadOfDash = new TestHyphenInsteadOfDash();
        var testQuotationMarks = new TestQuotationMarks();
        var testCaptionNextToRef = new TestCaptionNextToRef();
        
        // Сбор всех ошибок
        Errors.AddRange(testCiteToBibItems.Errors);
        Errors.AddRange(testEnvironmentLabelToRefs.Errors);
        Errors.AddRange(testEnvironmentWithItemsCommand.Errors);
        Errors.AddRange(testHyphenInsteadOfDash.Errors);
        Errors.AddRange(testQuotationMarks.Errors);
        
        // Генерация отчета
        GenerateReport();
    }
}
```

## Правила проверки (lint-rules.json)

### Структура конфигурации:
```json
{
  "QuotationMarks": {
    "PreferredOpen": "«",
    "PreferredClose": "»",
    "Forbidden": ["\"", "'"]
  },
  "Hyphen": {
    "WrongSymbol": "-",
    "Replacement": "—"
  }
}
```

### Модель правил:
```csharp
public class LintRules
{
    public QuotationMarksConfig QuotationMarks { get; set; }
    public HyphenConfig Hyphen { get; set; }
}

public class QuotationMarksConfig
{
    public string PreferredOpen { get; set; }
    public string PreferredClose { get; set; }
    public List<string> Forbidden { get; set; }
}

public class HyphenConfig
{
    public string WrongSymbol { get; set; }
    public string Replacement { get; set; }
}
```

## Реализованные проверки

### 1. TestQuotationMarks - Проверка кавычек
```csharp
public class TestQuotationMarks : TestFunction
{
    private readonly char[] _invalidQuotes;
    
    public TestQuotationMarks()
    {
        var rules = JsonSerializer.Deserialize<LintRules>(
            new StreamReader(TestUtilities.PathToLintRulesJson).ReadToEnd());
        _invalidQuotes = rules?.QuotationMarks.Forbidden
            .Select(s => s[0]).ToArray() ?? Array.Empty<char>();
        
        // Проверка в аргументах команд
        // Проверка в параметрах команд  
        // Проверка в текстовых блоках (исключая lstlisting)
    }
    
    private int FindMistakeQuatationMarksInText(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            foreach (var ch in _invalidQuotes)
            {
                if (text[i] == ch)
                    return i; // Позиция ошибки
            }
        }
        return 0; // Ошибок нет
    }
}
```

### 2. TestHyphenInsteadOfDash - Проверка дефисов vs тире
```csharp
public class TestHyphenInsteadOfDash : TestFunction
{
    private readonly char _wrongHyphen;
    
    private int FindMistakeHyphenInText(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != _wrongHyphen) continue;
            
            // Проверяем, что дефис окружен пробелами (контекст тире)
            bool left = (i - 1 >= 0) ? Char.IsWhiteSpace(text[i - 1]) : true;
            bool right = (i + 1 < text.Length) ? Char.IsWhiteSpace(text[i + 1]) : true;
            
            if (left && right)
                return i; // Найден дефис в контексте тире
        }
        return 0;
    }
}
```

### 3. TestCiteToBibItems - Проверка библиографии
- Находит все команды `\cite{...}`
- Проверяет наличие соответствующих `\bibitem{...}`
- Сообщает о несуществующих ссылках

### 4. TestEnvironmentLabelToRefs - Проверка label ↔ ref
- Находит все команды `\label{...}` в окружениях
- Проверяет наличие соответствующих `\ref{...}`
- Обнаруживает дублирующиеся labels
- Находит ссылки на несуществующие labels

### 5. TestEnvironmentWithItemsCommand - Проверка списков
- Анализирует окружения `enumerate`, `itemize`
- Проверяет корректность команд `\item`
- Валидирует регистры и завершающие символы

## Принципы добавления новых правил проверки

### 1. Создание новой тестовой функции:
```csharp
public class TestNewRule : TestFunction
{
    public TestNewRule()
    {
        // 1. Загрузка конфигурации из lint-rules.json
        var rules = JsonSerializer.Deserialize<LintRules>(
            new StreamReader(TestUtilities.PathToLintRulesJson).ReadToEnd());
        
        // 2. Получение команд для анализа
        var commandsToCheck = TestUtilities.GetAllCommandsByName("commandName");
        
        // 3. Анализ каждой команды
        foreach (var command in commandsToCheck)
        {
            if (HasError(command))
            {
                Errors.Add(new TestError()
                {
                    ErrorCommand = command,
                    ErrorType = ErrorType.Warning,
                    ErrorInfo = "Описание ошибки"
                });
            }
        }
    }
    
    private bool HasError(Command command) 
    {
        // Логика проверки
        return false;
    }
}
```

### 2. Регистрация в TestFunctionHandler:
```csharp
public TestFunctionHandler(List<Command> foundCommands)
{
    // ... существующие тесты ...
    var testNewRule = new TestNewRule();
    Errors.AddRange(testNewRule.Errors);
}
```

### 3. Обновление lint-rules.json:
```json
{
  "ExistingRules": { /* ... */ },
  "NewRule": {
    "Parameter1": "value1",
    "Parameter2": ["array", "values"]
  }
}
```

## Утилиты для работы с командами

### TestUtilities - хелперы для анализа:
```csharp
public static class TestUtilities
{
    public static List<Command> FoundsCommands { get; set; }
    public static List<Command> FoundsCommandsWithLstlisting { get; set; }
    
    // Получение команд по имени
    public static List<Command> GetAllCommandsByName(string name)
    
    // Получение всех окружений
    public static List<EnvironmentCommand> GetAllEnvironment()
    
    // Извлечение команд из lstlisting как параметров
    public static List<Command> GetAllCommandsLikeParametersFromLstlisting()
    
    // Получение контекста вокруг найденной ошибки
    public static string GetContentAreaFromFindSymbol(TextCommand textCommand, int symbolNumber)
}
```

## Критически важные принципы

### 1. Сохранение логики парсинга LaTeX
- **НЕ ИЗМЕНЯЙ** существующие алгоритмы парсинга без крайней необходимости
- **ДОПОЛНЯЙ**, а не заменяй существующую логику CommandHandler
- Всегда тестируй изменения на реальных LaTeX документах
- Сохраняй обратную совместимость с существующими правилами

### 2. Работа с TestFunctionClasses
- Каждая тестовая функция должна быть независимой
- Используй единообразный интерфейс наследования от TestFunction
- Обеспечивай детальное логирование результатов в TestError
- Поддерживай возможность отключения отдельных проверок

### 3. Конфигурация через lint-rules.json
- Всегда валидируй структуру JSON при загрузке
- Добавляй новые правила без нарушения существующей структуры
- Используй JsonSerializer.Deserialize для загрузки конфигурации
- Документируй все новые параметры конфигурации

### 4. LaTeX-специфичные требования
```csharp
// Правильная обработка комментариев
if (text[ch] == '%' && (ch-1 < 0 || text[ch-1] != '\\'))
    ignoreWhileStringEnd = true;

// Обработка команд с автодополнением расширения
if (!new Regex(".tex|.bib").Match(filename).Success)
    filename += ".tex";

// Исключение специальных окружений из проверки
if (environmentCommand.EnvironmentName == "lstlisting" || 
    environmentCommand.EnvironmentName == "comment")
    // Пропускаем проверку текста
```

### 5. Performance Guidelines
- Используй посимвольный парсинг только когда необходимо
- Кэшируй результаты загрузки конфигурации
- Освобождай временные директории после обработки
- Используй StringBuilder для построения больших строк

### 6. Error Handling
```csharp
// Всегда проверяй существование файлов
if (!File.Exists(filePath))
    throw new FileNotFoundException($"LaTeX file not found: {filePath}");

// Graceful handling при ошибках парсинга
try
{
    var commands = handler.FindAllCommandsInDocument();
}
catch (Exception ex)
{
    return $"Parsing error: {ex.Message}";
}
```

## API Contract

### Request Model:
```csharp
public record LintRequest(string StartFile, Dictionary<string, string> Files);
```

### Response: 
- Объединенный текст всех найденных команд
- В случае ошибки - текстовое описание проблемы

### Endpoints:
- `POST /api/lint` - основной анализ LaTeX документов
- `POST /api/diagnostic/test` - диагностический endpoint для отладки

## Папка с тестовыми документами (Нирки/)

### ⚠️ **ВАЖНО для разработчиков:**
- Папка `Нирки/` содержит **реальные LaTeX документы** студентов (НИР, ВКР, дипломы)
- **НЕ ИЗМЕНЯЙ** содержимое этой папки - это эталонные документы для тестирования
- **НЕ ОБРАЩАЙ** активного внимания на код и содержимое этих документов при разработке
- Используй только для **финального тестирования** запущенной программы CheckLaTeX
- Документы содержат реальные ошибки оформления, что идеально для валидации системы

### Структура тестовых документов:
```
Нирки/
├── NIR_*                        # НИР студентов разных курсов
├── VKR_Mamontov_2023/          # Выпускные квалификационные работы  
├── IST_-_4_kurs/               # Документы студентов ИСТ
├── Dasha/, Ивина_*/            # Практики и курсовые работы
└── MyDocTest/                  # Специальные тестовые документы
```

### Использование в тестировании:
1. **Запускай API** на `http://localhost:5000`
2. **Отправляй документы** из папки `Нирки/` через PowerShell скрипты
3. **Анализируй результаты** работы системы проверок
4. **Валидируй качество** обнаружения ошибок в реальных документах

**ВСЕГДА ПОМНИ**: Главная цель - корректная проверка LaTeX документов. Любые изменения должны улучшать качество анализа, не нарушая существующую функциональность посимвольного парсера! 