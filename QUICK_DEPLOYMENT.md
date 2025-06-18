# CheckLaTeX - Быстрое развертывание

Краткие инструкции для быстрого развертывания всей системы CheckLaTeX.

## 🚀 Быстрая установка (Весь стек)

### Полная автоматическая установка

```bash
# 1. Установка backend сервера
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install.sh | sudo bash

# 2. Установка VS Code расширения  
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install-extension.sh | bash
```

### Windows (PowerShell)

```powershell
# 1. Установка VS Code расширения (backend устанавливается отдельно)
& ([scriptblock]::Create((Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install-extension.ps1').Content))
```

## 📋 Компоненты системы

| Компонент | Описание | Порт/Путь |
|-----------|----------|-----------|
| **Backend API** | .NET Core сервер для анализа LaTeX | `http://localhost:5000` |
| **VS Code Extension** | Расширение для интеграции с VS Code | Встроено в VS Code |
| **Swagger UI** | Документация API | `http://localhost:5000/swagger` |

## ⚡ Быстрые команды

### Backend сервер

```bash
# Статус сервиса
sudo systemctl status checklatex

# Логи
sudo journalctl -u checklatex -f

# Перезапуск
sudo systemctl restart checklatex

# Обновление
sudo /opt/checklatex/update.sh
```

### VS Code расширение

```bash
# Проверка установки
code --list-extensions | grep checklatex

# Переустановка
./install-extension.sh

# Удаление
./uninstall-extension.sh
```

### Windows PowerShell

```powershell
# Установка с параметрами
.\install-extension.ps1 -Verbose

# Удаление с подтверждением
.\uninstall-extension.ps1 -Force
```

## 🔧 Основные настройки

### VS Code настройки

```json
{
    "checklatex.serverUrl": "http://localhost:5000",
    "checklatex.autoAnalyze": true,
    "checklatex.periodicCheck": false
}
```

### Горячие клавиши

- `Ctrl+Alt+Shift+S` - Полный анализ проекта
- `Ctrl+Alt+Shift+T` - Настройка интервала проверки
- `Ctrl+Shift+P` → "CheckLaTeX" - Все команды

## 🏗️ Локальная разработка

### Backend (tex-lint)

```bash
cd tex-lint
dotnet restore
dotnet run
```

### Extension (tex-lint-extension)

```bash
cd tex-lint-extension
npm install
npm run compile
npm run watch    # для разработки
vsce package     # создание .vsix
```

## 🧪 Тестирование

### Тест API

```bash
# Проверка работоспособности
curl -I http://localhost:5000/swagger

# Тест анализа
curl -X POST http://localhost:5000/api/lint/check \
  -H "Content-Type: application/json" \
  -d '{"latexContent": "\\documentclass{article}\n\\begin{document}\nТест\n\\end{document}", "fileName": "test.tex"}'
```

### Тест расширения

1. Откройте проект с .tex файлами в VS Code
2. Нажмите `Ctrl+Alt+Shift+S`
3. Проверьте вкладку "Problems" и "Output" → "CheckLaTeX Extension"

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

### Ошибки подключения

1. Проверьте URL сервера в настройках расширения
2. Проверьте firewall: `sudo ufw status`
3. Проверьте, что сервер слушает: `sudo netstat -tlnp | grep :5000`

## 📚 Полная документация

- [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) - Подробная установка backend
- [EXTENSION_DEPLOYMENT_GUIDE.md](EXTENSION_DEPLOYMENT_GUIDE.md) - Подробная установка расширения
- [README.md](README.md) - Общая информация о проекте

## 🆘 Поддержка

При возникновении проблем:

1. Проверьте [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) для backend
2. Проверьте [EXTENSION_DEPLOYMENT_GUIDE.md](EXTENSION_DEPLOYMENT_GUIDE.md) для расширения  
3. Создайте issue в GitHub репозитории
4. Приложите логи: `sudo journalctl -u checklatex` и логи VS Code

---

**💡 Совет:** Для продакшн развертывания используйте HTTPS и настройте обратный прокси (nginx/Apache). 