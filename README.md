# CheckLaTeX - Инструмент для комплексного анализа LaTeX документов

CheckLaTeX - это веб-сервис и инструмент командной строки для статического анализа LaTeX документов. Он помогает выявлять типичные ошибки оформления, нарушения стилистических правил и проблемы со структурой, предоставляя **точную диагностическую информацию** для быстрой интеграции с IDE и системами непрерывной интеграции.

## 🚀 Быстрая установка (Автоматическая)

### Полная система одной командой

```bash
# 1. Установка backend сервера (Linux/Ubuntu)
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install.sh | sudo bash

# 2. Установка VS Code расширения (Linux/macOS)
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install-extension.sh | bash
```

### Windows (PowerShell)

```powershell
# Установка VS Code расширения
& ([scriptblock]::Create((Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install-extension.ps1').Content))
```

### Проверка установки

```bash
# Проверка backend сервера
curl -I http://localhost:5000/swagger

# Проверка расширения VS Code
code --list-extensions | grep checklatex
```

## 📋 Компоненты системы

| Компонент | Описание | Доступ |
|-----------|----------|--------|
| **Backend API** | .NET Core сервер для анализа LaTeX | `http://localhost:5000` |
| **VS Code Extension** | Расширение для интеграции с редактором | Встроено в VS Code |
| **Swagger UI** | Интерактивная документация API | `http://localhost:5000/swagger` |

## 🔧 Ключевые возможности

### 📍 **Точная диагностика ошибок**
- **Локализация**: Определение файла, строки и колонки для каждой ошибки.
- **Контекст**: Предоставление исходного текста, вызвавшего ошибку.
- **Предложения**: Автоматические предложения по исправлению.
- **Интеграция с IDE**: Полная поддержка `Problems Panel` в Visual Studio Code.

### ⚙️ **Гибкая настройка правил**
- **Централизованная конфигурация**: Все правила настраиваются в `lint-rules.json`.
- **Включение/выключение**: Возможность отключить ненужные проверки.
- **Пользовательские стили**: Настройка под специфические требования к оформлению.

### 📦 **Поддержка комплексных проектов**
- **Анализ ZIP-архивов**: Проверка проектов, упакованных в ZIP, с сохранением структуры.
- **Автоопределение главного файла**: Автоматический поиск основного `.tex` файла в проекте.
- **Рекурсивный анализ**: Обработка вложенных файлов (`\input`, `\include`).

### 🔌 **Современное API и интеграция**
- **REST API**: Понятное API для интеграции с любыми системами.
- **Swagger UI**: Интерактивная документация для тестирования API прямо в браузере.
- **VS Code Extension**: Готовое расширение для полной интеграции в редактор.

## 🏃 Быстрый старт

### Автоматический способ (рекомендуется)

1. **Установите весь стек одной командой:**
   ```bash
   # Для Linux/Ubuntu
   curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install.sh | sudo bash
   curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install-extension.sh | bash
   ```

2. **Откройте LaTeX проект в VS Code:**
   - Нажмите `Ctrl+Alt+Shift+S` для полного анализа проекта
   - Или используйте Command Palette (`Ctrl+Shift+P`) → "CheckLaTeX"

3. **Проверьте результаты:**
   - Ошибки отображаются в панели **Problems**
   - Подсветка ошибок прямо в коде
   - Предложения по исправлению

### Ручной способ (для разработки)

1. **Запуск backend сервера:**
   ```bash
   cd tex-lint
   dotnet run
   ```
   Сервер будет доступен по адресу `http://localhost:5000`.

2. **Тестирование через Swagger UI:**
   - Откройте `http://localhost:5000/swagger`
   - Используйте эндпоинт `POST /api/lint/analyze-zip`
   - Загрузите ZIP-архив с LaTeX проектом

3. **Установка VS Code расширения:**
   ```bash
   cd tex-lint-extension
   npm install
   npm run compile
   vsce package
   code --install-extension checklatex-extension-1.0.0.vsix
   ```

## ⚡ Быстрые команды

### Управление backend сервером

```bash
# Статус сервиса
sudo systemctl status checklatex

# Просмотр логов
sudo journalctl -u checklatex -f

# Перезапуск
sudo systemctl restart checklatex

# Обновление
sudo /opt/checklatex/update.sh
```

### VS Code расширение

- `Ctrl+Alt+Shift+S` - Полный анализ проекта
- `Ctrl+Alt+Shift+T` - Настройка интервала проверки
- `Ctrl+Shift+P` → "CheckLaTeX" - Все команды расширения

## ⚙️ Использование и API

CheckLaTeX предоставляет несколько способов для анализа документов.

### 1. Анализ ZIP-архивов (рекомендуемый способ)
Эндпоинт: `POST /api/lint/analyze-zip`
Формат: `multipart/form-data`

Идеально подходит для анализа целых проектов.

**Пример с помощью cURL:**
```bash
curl -X POST http://localhost:5000/api/lint/analyze-zip \
  -F "zipFile=@/path/to/your/project.zip" \
  -F "startFile=main.tex" # Опционально, если не указать, определится автоматически
```

### 2. Анализ одиночного файла
Эндпоинт: `POST /api/lint/check`
Формат: `application/json`

Подходит для быстрой проверки отдельных файлов.

**Пример с помощью cURL:**
```bash
curl -X POST http://localhost:5000/api/lint/check \
  -H "Content-Type: application/json" \
  -d '{ "content": "\\documentclass{article}..." }'
```

## 📋 Реализованные проверки

Сервис поддерживает 8 основных групп проверок:

| Название теста | Описание проверки |
| :--- | :--- |
| **TestQuotationMarks** | Поиск и замена неправильных кавычек (`"`, `'`) на типографские (`«»`). |
| **TestHyphenInsteadOfDash** | Обнаружение дефиса, используемого вместо тире. |
| **TestMarginsAndSpacing** | Комплексная проверка полей, межстрочного интервала и шрифтов. |
| **TestLineBreaks** | Выявление одиночных переносов строк, которые не работают в LaTeX. |
| **TestEnvironmentWithItems**| Анализ правильности оформления списков (`itemize`, `enumerate`). |
| **TestEnvironmentLabelToRefs**| Проверка соответствия меток (`\label`) и ссылок (`\ref`). |
| **TestCiteToBibItems** | Валидация библиографических ссылок (`\cite`) и источников (`\bibitem`). |
| **TestCaptionLength** | Контроль длины подписей к рисункам и таблицам. |

## 📊 Структура ответа API

Ответ API содержит полную диагностическую информацию для каждой найденной проблемы.

### Формат ошибки с диагностикой
```json
{
  "testName": "TestQuotationMarks",
  "errors": [
    {
      "type": "Warning",
      "info": "Обнаружено использование неправильных кавычек. Рекомендуется замена на « » (елочки).",
      "command": null,
      "fileName": "chapter1/main.tex",
      "lineNumber": 42,
      "columnNumber": 15,
      "endLineNumber": 42,
      "endColumnNumber": 28,
      "originalText": "Это пример \"неправильных\" кавычек",
      "suggestedFix": "Это пример «неправильных» кавычек"
    }
  ]
}
```

## 🔧 Настройка и конфигурация

### VS Code настройки

```json
{
    "checklatex.serverUrl": "http://localhost:5000",
    "checklatex.autoAnalyze": true,
    "checklatex.autoAnalyzeScope": "project",
    "checklatex.periodicCheck": false,
    "checklatex.periodicCheckInterval": 300000
}
```

### Настройка правил

Все правила проверки настраиваются в файле `lint-rules.json`:

```json
{
  "quotationMarks": {
    "enabled": true,
    "severity": "Warning",
    "allowedQuotes": ["«", "»"]
  },
  "hyphen": {
    "enabled": true,
    "severity": "Warning",
    "checkDashes": true
  }
}
```

## 🔍 Устранение неполадок

### Backend не запускается

```bash
# Проверьте логи
sudo journalctl -u checklatex --no-pager -l

# Проверьте порт
sudo netstat -tlnp | grep :5000

# Переустановка
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/uninstall.sh | sudo bash
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install.sh | sudo bash
```

### Расширение не работает

1. Проверьте установку: `code --list-extensions | grep checklatex`
2. Проверьте настройки: `File > Preferences > Settings > CheckLaTeX`
3. Проверьте логи: `View > Output > CheckLaTeX Extension`
4. Убедитесь, что backend запущен: `curl -I http://localhost:5000/swagger`

## 🏗️ Архитектура и разработка

Подробное описание архитектуры, принципов разработки и инструкции по добавлению новых правил находятся в файле [**CHECKLATEX_ARCHITECTURE.md**](CHECKLATEX_ARCHITECTURE.md).

### Ключевые принципы:
- **Diagnostic-First**: Приоритет на точности и полноте диагностической информации.
- **Configuration-Driven**: Управление правилами через `lint-rules.json`.
- **Dependency Injection**: Гибкая архитектура на основе сервисов.

## 🧪 Тестирование

Руководство по тестированию, включая описание тестовых документов и сценариев, доступно в файле [**TEST_DOCUMENTATION.md**](TEST_DOCUMENTATION.md).

## 📚 Документация по развертыванию

### Основные руководства

- [**QUICK_DEPLOYMENT.md**](./QUICK_DEPLOYMENT.md) - Быстрые команды для развертывания всей системы
- [**DEPLOYMENT_GUIDE.md**](./DEPLOYMENT_GUIDE.md) - Подробная установка backend сервера  
- [**EXTENSION_DEPLOYMENT_GUIDE.md**](./EXTENSION_DEPLOYMENT_GUIDE.md) - Установка и компиляция VS Code расширения

### Скрипты автоматической установки

#### Linux/macOS
- `install.sh` - Установка backend сервера
- `uninstall.sh` - Удаление backend сервера
- `install-extension.sh` - Установка VS Code расширения
- `uninstall-extension.sh` - Удаление VS Code расширения

#### Windows PowerShell
- `install-extension.ps1` - Установка VS Code расширения
- `uninstall-extension.ps1` - Удаление VS Code расширения

### Дополнительная документация

- [**CHECKLATEX_ARCHITECTURE.md**](./CHECKLATEX_ARCHITECTURE.md): Техническая архитектура проекта.
- [**TEST_DOCUMENTATION.md**](./TEST_DOCUMENTATION.md): Руководство по тестированию.
- [**tex-lint-extension/README.md**](./tex-lint-extension/README.md): Документация по расширению для VS Code.

## 🆘 Поддержка

При возникновении проблем:

1. Проверьте соответствующий deployment guide
2. Используйте команды для диагностики из [QUICK_DEPLOYMENT.md](./QUICK_DEPLOYMENT.md)
3. Создайте issue в GitHub репозитории
4. Приложите логи: `sudo journalctl -u checklatex` и логи VS Code

---

## 🚀 Быстрый старт для нетерпеливых

```bash
# Полная установка одной командой (Linux)
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install.sh | sudo bash && \
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install-extension.sh | bash

# Проверка установки
curl -I http://localhost:5000/swagger && code --list-extensions | grep checklatex

# Теперь откройте LaTeX проект в VS Code и нажмите Ctrl+Alt+Shift+S
```

При использовании CheckLaTeX в ваших проектах, пожалуйста, укажите это в документации для поддержки проекта и улучшения качества LaTeX-анализа в сообществе.
