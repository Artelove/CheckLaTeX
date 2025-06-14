# Документация по тестированию CheckLaTeX

## Обзор тестовых материалов

Для комплексного тестирования системы CheckLaTeX созданы следующие материалы:

### 📄 Файлы
- `test-document.tex` - Полный LaTeX документ с различными тестовыми сценариями
- `test-cases.json` - Структурированные тестовые случаи для автоматизированного тестирования
- `lint-rules.json` - Конфигурация правил валидации

## 🎯 Цели тестирования

### Основные правила для проверки:
1. **QuotationMarks** - Проверка использования правильных кавычек
   - ✅ Разрешённые: `«»`, `„"`
   - ❌ Запрещённые: `"`, `'`

2. **Hyphen** - Проверка использования правильных тире
   - ✅ Правильно: `—` (длинное тире)
   - ❌ Неправильно: `-` (дефис)

## 📋 Тестовые сценарии

### 1. Правильное использование (Valid Cases)
```latex
«Это правильный пример кавычек»
Это предложение — с правильным тире
```

### 2. Неправильное использование (Invalid Cases)
```latex
"Это неправильные кавычки"
Это предложение - с неправильным дефисом
```

### 3. Граничные случаи (Edge Cases)
```latex
"неправильно с самого начала.
правильно, но закрываем неправильно"
""
-начало строки
конец строки-
```

### 4. Смешанные сценарии (Combined Cases)
```latex
"Неправильные кавычки - и неправильный дефис"
«Правильные кавычки - но неправильный дефис»
```

### 5. LaTeX-специфичные конструкции
```latex
% Комментарии с "ошибками" - не должны проверяться
$x - y = 5$ % Математические формулы игнорируются
\texttt{command-line} % Команды могут обрабатываться особо
```

## 🔧 Использование тестовых материалов

### Для ручного тестирования:
1. Запустите вашу систему CheckLaTeX
2. Загрузите файл `test-document.tex`
3. Проанализируйте результаты валидации
4. Сравните с ожидаемыми результатами

### Для автоматизированного тестирования:
1. Загрузите тестовые случаи из `test-cases.json`
2. Выполните каждый тест через соответствующую функцию
3. Сравните фактические результаты с ожидаемыми
4. Сгенерируйте отчёт о результатах

## 📊 Ожидаемые результаты

### Общая статистика ошибок в test-document.tex:
- **Ошибки кавычек**: 15-25 случаев
- **Ошибки дефисов**: 10-20 случаев
- **Общее количество проблем**: 25-45

### Детальная разбивка по разделам:

#### Раздел "Неправильное использование кавычек":
```
Строка: "Это неправильный пример"
Ошибки: 2 (открывающая и закрывающая кавычки)

Строка: 'Ещё один неправильный пример'
Ошибки: 2 (открывающая и закрывающая кавычки)
```

#### Раздел "Неправильное использование дефисов":
```
Строка: Это предложение - с неправильным дефисом
Ошибки: 1 (дефис должен быть тире)

Строка: Период 1990-2000 годов
Ошибки: 1 (дефис в диапазоне)
```

## 🧪 Создание дополнительных тестов

### Шаблон для новых тестовых случаев:
```json
{
  "id": "unique_test_id",
  "description": "Описание тестового случая",
  "input": "Тестируемый LaTeX код",
  "expectedErrors": 2,
  "expectedValid": false,
  "expectedIssues": [
    {
      "lineNumber": 1,
      "columnNumber": 5,
      "message": "Описание ошибки"
    }
  ]
}
```

### Рекомендации по добавлению тестов:
1. **Покрытие edge cases** - тестируйте граничные условия
2. **Производительность** - добавляйте тесты с большими объёмами текста
3. **Unicode поддержка** - проверяйте различные символы
4. **LaTeX специфика** - тестируйте сложные конструкции LaTeX

## 🔍 Анализ результатов

### Критерии успешного прохождения:
- ✅ Все валидные случаи должны проходить без ошибок
- ✅ Все невалидные случаи должны обнаруживать ошибки
- ✅ Количество найденных ошибок должно соответствовать ожиданиям
- ✅ LaTeX конструкции должны обрабатываться корректно

### Возможные проблемы и решения:

#### Ложные срабатывания:
```latex
% Если система находит ошибки в:
$x - y = 5$ % Математических формулах
% Комментариях с "кавычками"
\texttt{code-sample} % Коде программ
```
**Решение**: Улучшить парсер для игнорирования этих конструкций

#### Пропущенные ошибки:
```latex
% Если система не находит ошибки в:
"явно неправильных кавычках"
Предложение - с неправильным дефисом
```
**Решение**: Проверить логику правил валидации

## 📈 Метрики качества

### Основные показатели:
- **Precision** (Точность): Доля правильно найденных ошибок
- **Recall** (Полнота): Доля обнаруженных от всех существующих ошибок
- **F1-Score**: Гармоническое среднее точности и полноты

### Расчёт метрик:
```
Precision = True Positives / (True Positives + False Positives)
Recall = True Positives / (True Positives + False Negatives)
F1 = 2 * (Precision * Recall) / (Precision + Recall)
```

## 🚀 Автоматизация тестирования

### Пример псевдокода для автоматического тестирования:
```csharp
public class TestRunner
{
    public TestResults RunAllTests(string testCasesPath)
    {
        var testCases = LoadTestCases(testCasesPath);
        var results = new TestResults();
        
        foreach (var testCase in testCases)
        {
            var actualResult = _lintEngine.Validate(testCase.Input);
            var testResult = CompareResults(actualResult, testCase.Expected);
            results.Add(testResult);
        }
        
        return results;
    }
}
```

## 📝 Отчётность

### Формат отчёта о тестировании:
```
=== ОТЧЁТ О ТЕСТИРОВАНИИ CHECKLATEX ===
Дата: 2024-01-15 10:30:00
Версия: 1.0.0

ОБЩАЯ СТАТИСТИКА:
- Всего тестов: 61
- Пройдено: 58
- Провалено: 3
- Успешность: 95.1%

ДЕТАЛИЗАЦИЯ ПО КАТЕГОРИЯМ:
- Правильные случаи: 12/12 (100%)
- Неправильные случаи: 23/25 (92%)
- Граничные случаи: 7/8 (87.5%)
- LaTeX конструкции: 7/7 (100%)

НАЙДЕННЫЕ ПРОБЛЕМЫ:
1. Тест invalid_quotes_3: Ожидалось 3 ошибки, найдено 2
2. Тест edge_hyphen_3: Ложное срабатывание в математической формуле
3. Тест unicode_1: Неправильная обработка немецких кавычек
```

## 🔄 Непрерывное улучшение

### Рекомендации по развитию тестового набора:
1. **Регулярное обновление** - добавлять новые сценарии по мере развития системы
2. **Анализ реальных документов** - создавать тесты на основе проблем из практики
3. **Обратная связь пользователей** - включать случаи, вызывающие вопросы
4. **Производительность** - следить за временем выполнения тестов

### Версионирование тестов:
- Документируйте изменения в тестовых случаях
- Сохраняйте совместимость с предыдущими версиями
- Ведите changelog для тестовых данных

---

**Примечание**: Данная документация должна обновляться при изменении правил валидации или добавлении новых тестовых сценариев. 