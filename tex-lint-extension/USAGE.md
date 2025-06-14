# Инструкция по использованию CheckLaTeX Extension

## Быстрый старт

### 1. Установка и настройка

1. **Запустите скрипт установки:**
   ```powershell
   .\scripts\setup.ps1
   ```

2. **Или выполните установку вручную:**
   ```bash
   npm install
   npm run compile
   vsce package
   ```

### 2. Установка расширения в VS Code

**Способ 1: Из файла .vsix**
1. Откройте VS Code
2. Нажмите `Ctrl+Shift+P`
3. Введите "Extensions: Install from VSIX..."
4. Выберите созданный `.vsix` файл

**Способ 2: Режим разработки**
1. Откройте папку `tex-lint-extension` в VS Code
2. Нажмите `F5` для запуска Extension Development Host
3. В новом окне откройте LaTeX проект

### 3. Настройка сервера

1. Убедитесь, что CheckLaTeX сервер запущен (по умолчанию на порту 5000)
2. Откройте настройки VS Code (`Ctrl+,`)
3. Найдите секцию "CheckLaTeX"
4. Установите правильный URL сервера

## Основные функции

### Анализ всего проекта

1. **Через контекстное меню:**
   - ПКМ на папке проекта в Explorer
   - Выберите "Analyze LaTeX Project"

2. **Через Command Palette:**
   - `Ctrl+Shift+P`
   - Введите "CheckLaTeX: Analyze LaTeX Project"

3. **Через статус-бар:**
   - Нажмите на "CheckLaTeX" в правом нижнем углу

### Анализ текущего файла

1. **Откройте .tex файл**
2. **Способы запуска:**
   - Кнопка в заголовке редактора (справа от вкладок)
   - ПКМ в редакторе → "Analyze Current LaTeX File"
   - `Ctrl+Shift+P` → "CheckLaTeX: Analyze Current LaTeX File"

### Просмотр результатов

Результаты отображаются в трех местах:

1. **Output Channel** - подробный лог:
   - View → Output → выберите "CheckLaTeX"

2. **Explorer Panel** - дерево результатов:
   - Появляется панель "CheckLaTeX Results"
   - Показывает статистику и ошибки по категориям

3. **Notifications** - быстрые уведомления о статусе

## Конфигурация

### Основные настройки

```json
{
  "checklatex.serverUrl": "http://localhost:5000",
  "checklatex.startFile": "main.tex",
  "checklatex.timeout": 30000,
  "checklatex.autoAnalyze": false,
  "checklatex.excludePatterns": [
    "**/node_modules/**",
    "**/build/**",
    "**/dist/**",
    "**/.git/**"
  ]
}
```

### Настройка исключений

Для исключения файлов и папок из анализа используйте паттерны в `excludePatterns`:

```json
{
  "checklatex.excludePatterns": [
    "**/build/**",           // Исключить папку build
    "**/temp/**",            // Исключить папку temp
    "**/*.aux",              // Исключить файлы .aux
    "**/old-versions/**"     // Исключить папку old-versions
  ]
}
```

### Автоматический анализ

Включите автоматический анализ при сохранении:

```json
{
  "checklatex.autoAnalyze": true
}
```

## Тестирование

### Тестовый проект

В папке `test-project/` находится тестовый LaTeX документ с намеренными ошибками:

1. Откройте папку `test-project` в VS Code
2. Запустите анализ проекта
3. Изучите результаты в Output Channel

### Типичные проблемы и решения

**Проблема:** "Сервер недоступен"
- **Решение:** Убедитесь, что CheckLaTeX сервер запущен
- Проверьте URL в настройках

**Проблема:** "Файлы не найдены"
- **Решение:** Убедитесь, что проект содержит .tex файлы
- Проверьте настройку `startFile`

**Проблема:** "Таймаут запроса"
- **Решение:** Увеличьте значение `timeout` в настройках
- Проверьте размер проекта (большие проекты требуют больше времени)

## Разработка и отладка

### Структура проекта

```
tex-lint-extension/
├── src/
│   └── extension.ts          # Основная логика
├── .vscode/
│   ├── launch.json           # Конфигурация отладки
│   └── tasks.json            # Задачи сборки
├── media/
│   ├── icon.svg              # Иконка (SVG)
│   └── icon.png              # Иконка (PNG)
├── scripts/
│   └── setup.ps1             # Скрипт установки
├── test-project/
│   └── main.tex              # Тестовый LaTeX файл
├── package.json              # Конфигурация расширения
├── tsconfig.json             # Конфигурация TypeScript
└── README.md                 # Документация
```

### Отладка

1. **Откройте папку расширения в VS Code**
2. **Установите точки останова в `src/extension.ts`**
3. **Нажмите F5 для запуска отладки**
4. **В новом окне VS Code тестируйте функции**

### Сборка

```bash
# Компиляция TypeScript
npm run compile

# Режим наблюдения (автоматическая компиляция)
npm run watch

# Создание пакета
npm run package
```

## API CheckLaTeX сервера

Расширение использует следующие эндпоинты:

### POST /api/lint/analyze-zip

Отправляет ZIP архив проекта на анализ.

**Параметры:**
- `zipFile` (multipart/form-data) - ZIP файл с LaTeX проектом
- `startFile` (опционально) - главный .tex файл

**Ответ:**
```json
{
  "commandsFound": 42,
  "testResults": [
    {
      "testName": "TestQuotationMarks",
      "errors": [
        {
          "type": "WrongQuotationMark",
          "info": "Неправильные кавычки",
          "command": "..."
        }
      ]
    }
  ],
  "text": "Подробный отчет..."
}
```

## Лицензия

MIT License - см. файл LICENSE в корне проекта.

## Поддержка

Для сообщения об ошибках и предложений создайте issue в репозитории проекта. 