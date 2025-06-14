# Обновления CheckLaTeX Server для поддержки диагностик VS Code

## Обзор изменений

Данные изменения добавляют полную поддержку диагностик Visual Studio Code в серверный проект CheckLaTeX. Теперь сервер возвращает точные позиции ошибок с детальной информацией для подсветки проблем прямо в редакторе.

## 🔧 Измененные файлы

### 1. `tex-lint/Models/TestError.cs`

**Добавленные свойства для диагностик:**
```csharp
/// <summary>
/// Имя файла относительно корня проекта (например, "chapter1/main.tex")  
/// </summary>
public string? FileName { get; set; }

/// <summary>
/// Номер строки (1-based) где найдена ошибка
/// </summary>
public int? LineNumber { get; set; }

/// <summary>
/// Номер колонки (1-based) где начинается ошибка
/// </summary>
public int? ColumnNumber { get; set; }

/// <summary>
/// Номер строки (1-based) где заканчивается ошибка (опционально)
/// </summary>
public int? EndLineNumber { get; set; }

/// <summary>
/// Номер колонки (1-based) где заканчивается ошибка (опционально)
/// </summary>
public int? EndColumnNumber { get; set; }

/// <summary>
/// Исходный текст с ошибкой для контекста
/// </summary>
public string? OriginalText { get; set; }

/// <summary>
/// Предлагаемое исправление (опионально)
/// </summary>
public string? SuggestedFix { get; set; }
```

**Новый статический метод:**
```csharp
public static TestError CreateWithDiagnostics(
    ErrorType errorType, 
    string errorInfo, 
    string fileName, 
    int lineNumber, 
    int columnNumber, 
    string originalText,
    int? endLineNumber = null, 
    int? endColumnNumber = null, 
    string? suggestedFix = null,
    Command? errorCommand = null)
```

### 2. `tex-lint/TestFunctionClasses/TestQuotationMarks.cs`

**Ключевые изменения:**
- ✅ Заменен алгоритм подсчета ошибок на точное определение позиций
- ✅ Добавлен метод `FindQuotationMarkErrors()` с построчным анализом
- ✅ Генерация предлагаемых исправлений (замена на елочки `<<>>`)
- ✅ Контекстная информация вокруг ошибки
- ✅ Поддержка многострочных текстов

**Новый метод анализа:**
```csharp
private List<TestError> FindQuotationMarkErrors(string text, string fileName, int baseLineNumber)
{
    // Построчный анализ с точными позициями
    // Определение контекста ошибки 
    // Генерация предлагаемых исправлений
}
```

### 3. `tex-lint/TestFunctionClasses/TestHyphenInsteadOfDash.cs`

**Аналогичные изменения:**
- ✅ Замена `FindMistakeHyphenInText()` на `FindHyphenErrors()`
- ✅ Точное определение позиций дефисов, которые должны быть тире
- ✅ Генерация предлагаемых исправлений (замена на `---`)
- ✅ Проверка контекста (дефис должен быть окружен пробелами)

### 4. `tex-lint/Controllers/LintController.cs`

**Обновленный JSON ответ:**
Методы `CheckLatex` и `AnalyzeZip` теперь возвращают расширенную информацию:

```csharp
errors = r.Value.Select(e => new
{
    type = e.ErrorType.ToString(),
    info = e.ErrorInfo,
    command = e.ErrorCommand?.ToString(),
    // НОВЫЕ поля для диагностик VS Code
    fileName = e.FileName,
    lineNumber = e.LineNumber,
    columnNumber = e.ColumnNumber,
    endLineNumber = e.EndLineNumber,
    endColumnNumber = e.EndColumnNumber,
    originalText = e.OriginalText,
    suggestedFix = e.SuggestedFix
})
```

## 📊 Структура API ответа

### Пример ответа с диагностическими данными:

```json
{
  "commandsFound": 25,
  "testResults": [
    {
      "testName": "TestQuotationMarks",
      "errors": [
        {
          "type": "Warning",
          "info": "Обнаружено использование неправильных кавычек. Рекомендуется замена на << >> (елочки) в тексте",
          "command": null,
          "fileName": "main.tex",
          "lineNumber": 15,
          "columnNumber": 12,
          "endLineNumber": 15,
          "endColumnNumber": 13,
          "originalText": "слово \"неправильные\" кавычки",
          "suggestedFix": "слово <<неправильные>> кавычки"
        }
      ]
    },
    {
      "testName": "TestHyphenInsteadOfDash", 
      "errors": [
        {
          "type": "Warning",
          "info": "Дефис '-' должен быть заменен на тире в позиции 8",
          "fileName": "main.tex",
          "lineNumber": 23,
          "columnNumber": 8,
          "endLineNumber": 23,
          "endColumnNumber": 9,
          "originalText": "слово - тире",
          "suggestedFix": "слово --- тире"
        }
      ]
    }
  ]
}
```

## 🔄 Обратная совместимость

- ✅ Все существующие API эндпоинты работают без изменений
- ✅ Старые клиенты получают расширенные данные (игнорируют новые поля)
- ✅ Устаревшие методы помечены `[Obsolete]` но продолжают работать

## 🚀 Преимущества для VS Code расширения

1. **Точная подсветка ошибок**: Красные/желтые волнистые линии точно под проблемными символами
2. **Интеллектуальные исправления**: VS Code может предложить автофиксы
3. **Навигация по ошибкам**: Problems Panel с кликабельными ссылками на файлы
4. **Контекстная информация**: Подробности об ошибке при наведении мыши
5. **Многофайловая поддержка**: Диагностики для всех файлов в проекте

## 🧪 Тестирование изменений

### Команды для проверки:

```powershell
# Компиляция проекта
cd tex-lint
dotnet build

# Запуск сервера (если нужно)
dotnet run

# Тестирование API
curl -X POST http://localhost:5000/api/lint/check \
  -H "Content-Type: application/json" \
  -d '{"content": "\\documentclass{article}\n\\begin{document}\nТекст с \"неправильными\" кавычками и дефисом - тире.\n\\end{document}"}'
```

### Ожидаемый результат:
- ✅ Компиляция без ошибок
- ✅ JSON ответ содержит новые поля `fileName`, `lineNumber`, `columnNumber` и др.
- ✅ Точные позиции ошибок для кавычек и дефисов

## 📝 Статус реализации

| Компонент | Статус | Описание |
|-----------|---------|----------|
| Модель TestError | ✅ Готово | Добавлены все поля диагностик |
| TestQuotationMarks | ✅ Готово | Точные позиции кавычек |
| TestHyphenInsteadOfDash | ✅ Готово | Точные позиции дефисов |
| LintController | ✅ Готово | Обновлен JSON ответ |
| Компиляция | ✅ Готово | Проект собирается успешно |
| VS Code Extension | ✅ Готово | Поддерживает новые поля |

## ⚡ Производительность

- Новый алгоритм анализа имеет сложность O(n*m) где n - количество строк, m - количество символов
- Для типичных LaTeX документов (< 10K строк) производительность остается высокой
- Memory overhead минимальный благодаря lazy evaluation

## 🐛 Известные ограничения

1. **Многострочные конструкции**: Анализ ведется по строкам, сложные многострочные LaTeX команды могут требовать доработки
2. **Encoding**: Предполагается UTF-8 кодировка файлов
3. **LaTeX Comments**: Ошибки внутри комментариев (после %) могут анализироваться, требует фильтрации

## 📈 Планы развития

- [ ] Добавить поддержку диагностик в другие тестовые функции
- [ ] Реализовать QuickFix actions для автоматических исправлений  
- [ ] Добавить severity levels для разных типов ошибок
- [ ] Кэширование результатов анализа для больших проектов 