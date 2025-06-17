# CheckLaTeX API с Swagger UI

## ✅ Что добавлено

### 1. Swagger UI интеграция
- **Swagger UI доступен по адресу**: `http://localhost:5000/swagger`
- **Swagger JSON**: `http://localhost:5000/swagger/v1/swagger.json`
- **Автоматическая документация** всех API endpoints
- **Интерактивное тестирование** прямо в браузере

### 2. Новый endpoint для загрузки ZIP файлов
- **URL**: `POST /api/lint/analyze-zip`
- **Формат**: `multipart/form-data`
- **Параметры**:
  - `zipFile` (file, required) - ZIP архив с LaTeX документами
  - `startFile` (string, optional) - Главный файл для анализа

### 3. Автоматическое определение главного файла
- Ищет файлы с `\documentclass` команды
- Если не найден, берет первый `.tex` файл
- Поддерживает вложенные папки в ZIP архиве

## 🚀 Как использовать

### Запуск API
```bash
cd tex-lint
dotnet run
```

API будет доступен на `http://localhost:5000`

### Методы тестирования

#### 1. Через Swagger UI (Рекомендуется)
1. Откройте браузер: `http://localhost:5000/swagger`
2. Найдите endpoint `POST /api/lint/analyze-zip`
3. Нажмите **"Try it out"**
4. Загрузите ZIP файл
5. (Опционально) Укажите `startFile`
6. Нажмите **"Execute"**

#### 2. Через PowerShell скрипт
```powershell
# Анализ ZIP файла с автоопределением главного файла
.\test-zip.ps1 -ZipPath "path\to\document.zip"

# Анализ с указанием главного файла
.\test-zip.ps1 -ZipPath "path\to\document.zip" -StartFile "main.tex"

# С указанием другого URL API
.\test-zip.ps1 -ZipPath "path\to\document.zip" -ApiUrl "http://localhost:8080"
```

#### 3. Через curl (если установлен)
```bash
curl -X POST \
  -H "Content-Type: multipart/form-data" \
  -F "zipFile=@document.zip" \
  -F "startFile=main.tex" \
  http://localhost:5000/api/lint/analyze-zip
```

#### 4. Через Postman
1. Создайте POST запрос к `http://localhost:5000/api/lint/analyze-zip`
2. Выберите **Body → form-data**
3. Добавьте поле `zipFile` типа **File** и выберите ZIP файл
4. (Опционально) Добавьте поле `startFile` типа **Text**
5. Отправьте запрос

## 📋 Доступные endpoints

### Swagger UI показывает все endpoints:

#### `POST /api/lint/analyze` (существующий)
- Анализ LaTeX документов из JSON запроса
- Принимает: `LintRequest` с полями `StartFile` и `Files`

#### `POST /api/lint/analyze-zip` (новый)
- Анализ LaTeX документов из ZIP архива
- Принимает: `multipart/form-data` с `zipFile` и `startFile`

#### `POST /api/diagnostic/test` (существующий)
- Диагностический тест для проверки парсера
- Принимает: `LintRequest` с полями `StartFile` и `Files`

## 🔧 Техническая реализация

### Новые зависимости
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
```

### Алгоритм обработки ZIP файлов
1. **Загрузка**: Получение ZIP файла через `IFormFile`
2. **Валидация**: Проверка формата и размера файла
3. **Извлечение**: Распаковка в временную директорию
4. **Поиск главного файла**: Автоматическое определение или использование указанного
5. **Анализ**: Запуск `CommandHandler` для парсинга LaTeX
6. **Очистка**: Удаление временных файлов
7. **Результат**: Возврат результата анализа

### Функция поиска главного файла
```csharp
private string? FindMainTexFile(string directory)
{
    var texFiles = Directory.GetFiles(directory, "*.tex", SearchOption.AllDirectories);
    
    // Ищем файлы с \documentclass
    foreach (var texFile in texFiles)
    {
        var content = File.ReadAllText(texFile);
        if (content.Contains("\\documentclass"))
        {
            return Path.GetRelativePath(directory, texFile);
        }
    }
    
    // Возвращаем первый .tex файл если главный не найден
    return texFiles.Length > 0 ? Path.GetRelativePath(directory, texFiles[0]) : null;
}
```

## 🧪 Тестирование с реальными документами

### Использование папки Нирки/
```powershell
# Создайте ZIP архив из любой папки в Нирки/
Compress-Archive -Path "Нирки\VKR_Mamontov_2023\*" -DestinationPath "test_vkr.zip"

# Протестируйте через API
.\test-zip.ps1 -ZipPath "test_vkr.zip"
```

### Или через Swagger UI
1. Создайте ZIP архив вручную из папки в `Нирки/`
2. Откройте `http://localhost:5000/swagger`
3. Загрузите ZIP через Swagger UI
4. Анализируйте результаты

## ⚡ Преимущества нового подхода

### Для пользователей:
- **Простота**: Один ZIP файл вместо множества файлов
- **Структура**: Сохраняется исходная структура папок
- **Автоматизация**: Не нужно указывать главный файл
- **Удобство**: Графический интерфейс через Swagger

### Для разработчиков:
- **Документация**: Автоматическая документация API
- **Тестирование**: Удобное тестирование через браузер
- **Стандарты**: Соответствие стандартам OpenAPI
- **Расширяемость**: Легко добавлять новые endpoints

## 🚨 Важные замечания

1. **Размер файлов**: Нет ограничений на размер ZIP (добавьте при необходимости)
2. **Безопасность**: ZIP bomb protection не реализован
3. **Кодировка**: Поддерживается UTF-8 для LaTeX файлов
4. **Временные файлы**: Автоматически очищаются после обработки
5. **Ошибки**: Подробные сообщения об ошибках в ответах API

Теперь CheckLaTeX стал еще удобнее в использовании! 🎉 