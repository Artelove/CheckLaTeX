# CheckLaTeX Extension - Быстрый старт

## 🚀 Установка за 3 шага

### 1. Подготовка
```powershell
# Запустите PowerShell в папке tex-lint-extension
.\scripts\setup.ps1
```

### 2. Запуск в режиме разработки
1. Откройте папку `tex-lint-extension` в VS Code
2. Нажмите `F5` → откроется новое окно VS Code
3. В новом окне откройте LaTeX проект

### 3. Тестирование
1. Откройте тестовый проект `test-project/main.tex`
2. Нажмите `Ctrl+Shift+P` → введите "CheckLaTeX"
3. Выберите "Analyze LaTeX Project"

## ⚙️ Настройка сервера

В VS Code настройках (`Ctrl+,`) найдите "CheckLaTeX":
- `checklatex.serverUrl`: `http://localhost:5000` (адрес вашего сервера)

## 🎯 Основные команды

| Команда | Описание | Способ запуска |
|---------|----------|----------------|
| **Analyze Project** | Анализирует весь LaTeX проект | ПКМ на папке → "Analyze LaTeX Project" |
| **Analyze File** | Анализирует текущий .tex файл | Кнопка в редакторе или ПКМ в файле |
| **Configure Server** | Настройка URL сервера | Command Palette → "Configure Server" |

## 📊 Просмотр результатов

- **Output Channel**: View → Output → "CheckLaTeX" (подробный лог)
- **Explorer Panel**: "CheckLaTeX Results" (древовидная структура)
- **Notifications**: Уведомления о статусе анализа

## 🔧 Проблемы?

- **Сервер недоступен**: Проверьте, что CheckLaTeX сервер запущен на порту 5000
- **Файлы не найдены**: Убедитесь, что проект содержит .tex файлы
- **Долгий анализ**: Увеличьте `timeout` в настройках

---
📚 **Подробная документация**: см. [USAGE.md](USAGE.md) и [README.md](README.md) 