# Руководство по развертыванию CheckLaTeX VS Code Extension

Это руководство описывает процесс компиляции, упаковки и установки расширения CheckLaTeX для Visual Studio Code.

## Быстрая установка (автоматический скрипт)

### Полная переустановка расширения

```bash
# Автоматическая переустановка расширения
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/uninstall-extension.sh | bash
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install-extension.sh | bash
```

### Локальная установка (сейчас)

Пока скрипты не в репозитории, скачайте их локально:

```bash
# Клонируйте репозиторий или скачайте файлы install-extension.sh и uninstall-extension.sh
# Затем выполните:
chmod +x install-extension.sh uninstall-extension.sh
./install-extension.sh
```

## Требования к системе

### Обязательные компоненты
- **Node.js** версии 16.x или выше
- **npm** версии 8.0 или выше  
- **Visual Studio Code** версии 1.74.0 или выше
- **TypeScript** (устанавливается автоматически)
- **VS Code Extension Manager (vsce)** для упаковки

### Дополнительные инструменты
- **Git** для клонирования репозитория
- **PowerShell** или **Bash** для выполнения скриптов

## Архитектура расширения

### Структура проекта
```
tex-lint-extension/
├── src/
│   └── extension.ts        # Основной код расширения (1148 строк)
├── media/
│   ├── icon.png           # Иконка расширения
│   └── icon.svg           # Векторная иконка
├── package.json           # Манифест расширения
├── tsconfig.json          # Конфигурация TypeScript
└── README.md             # Документация
```

### Функциональность расширения
- **Анализ LaTeX проектов**: Полный анализ всех .tex файлов в проекте
- **Анализ текущего файла**: Проверка открытого .tex файла
- **Автоматический анализ**: При сохранении файлов
- **Периодические проверки**: Фоновый анализ с настраиваемыми интервалами
- **Диагностика в реальном времени**: Подсветка ошибок в коде
- **Интеграция с сервером**: Работа с CheckLaTeX Backend API

## Ручная установка (пошагово)

### Шаг 1: Подготовка окружения

#### Установка Node.js
```bash
# Ubuntu/Debian
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# CentOS/RHEL
curl -fsSL https://rpm.nodesource.com/setup_18.x | sudo bash -
sudo yum install -y nodejs

# macOS (с Homebrew)
brew install node

# Windows (с Chocolatey)
choco install nodejs
```

#### Проверка установки
```bash
node --version    # должно быть >= 16.0.0
npm --version     # должно быть >= 8.0.0
```

#### Установка vsce (VS Code Extension Manager)
```bash
npm install -g vsce
```

### Шаг 2: Клонирование репозитория

```bash
# Клонируем репозиторий
git clone https://github.com/Artelove/CheckLaTeX.git
cd CheckLaTeX/tex-lint-extension

# Или если работаете с локальной копией
cd /path/to/your/CheckLaTeX/tex-lint-extension
```

### Шаг 3: Установка зависимостей

```bash
# Устанавливаем все зависимости
npm install

# Проверяем установленные пакеты
npm list --depth=0
```

### Шаг 4: Компиляция TypeScript

```bash
# Компиляция в режиме production
npm run compile

# Компиляция в режиме watch (для разработки)
npm run watch

# Проверка синтаксиса
npm run lint
```

### Шаг 5: Упаковка расширения

```bash
# Создаем .vsix пакет
npm run package

# Или используем vsce напрямую
vsce package

# Указываем конкретную версию
vsce package --out checklatex-extension-1.0.0.vsix
```

### Шаг 6: Установка расширения

#### Установка через VS Code UI
1. Откройте VS Code
2. Нажмите `Ctrl+Shift+P` (или `Cmd+Shift+P` на macOS)
3. Введите "Extensions: Install from VSIX..."
4. Выберите созданный файл `checklatex-extension-*.vsix`

#### Установка через командную строку
```bash
# Установка через code CLI
code --install-extension checklatex-extension-1.0.0.vsix

# Принудительная переустановка
code --install-extension checklatex-extension-1.0.0.vsix --force
```

#### Установка в VS Code Insiders
```bash
code-insiders --install-extension checklatex-extension-1.0.0.vsix
```

## Скрипты автоматической установки

### Скрипт установки расширения (install-extension.sh)

```bash
#!/bin/bash
set -e

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Функции для красивого вывода
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_status "=== Установка CheckLaTeX VS Code Extension ==="

# Проверка наличия необходимых инструментов
check_dependencies() {
    print_status "Проверка зависимостей..."
    
    # Проверяем Node.js
    if ! command -v node &> /dev/null; then
        print_error "Node.js не установлен. Установите Node.js 16.x или выше."
        exit 1
    fi
    
    NODE_VERSION=$(node --version | cut -d'v' -f2 | cut -d'.' -f1)
    if [ "$NODE_VERSION" -lt 16 ]; then
        print_error "Требуется Node.js версии 16 или выше. Текущая версия: $(node --version)"
        exit 1
    fi
    
    # Проверяем npm
    if ! command -v npm &> /dev/null; then
        print_error "npm не установлен"
        exit 1
    fi
    
    # Проверяем VS Code
    if ! command -v code &> /dev/null; then
        print_warning "VS Code CLI не найден. Убедитесь, что VS Code установлен и добавлен в PATH"
    fi
    
    print_success "Все зависимости присутствуют"
}

# Установка vsce если его нет
install_vsce() {
    if ! command -v vsce &> /dev/null; then
        print_status "Установка vsce (VS Code Extension Manager)..."
        npm install -g vsce
        print_success "vsce установлен"
    else
        print_status "vsce уже установлен: $(vsce --version)"
    fi
}

# Удаление предыдущей версии расширения
remove_previous_extension() {
    print_status "Удаление предыдущих версий расширения..."
    
    # Получаем список установленных расширений
    if command -v code &> /dev/null; then
        code --list-extensions | grep -i checklatex | while read -r extension; do
            print_status "Удаляем расширение: $extension"
            code --uninstall-extension "$extension" 2>/dev/null || true
        done
    fi
    
    # Также удаляем из Insiders если есть
    if command -v code-insiders &> /dev/null; then
        code-insiders --list-extensions | grep -i checklatex | while read -r extension; do
            print_status "Удаляем расширение из VS Code Insiders: $extension"
            code-insiders --uninstall-extension "$extension" 2>/dev/null || true
        done
    fi
}

# Основная функция установки
install_extension() {
    local TEMP_DIR="/tmp/checklatex-extension-install"
    local REPO_URL="https://github.com/Artelove/CheckLaTeX.git"
    
    # Очищаем временную директорию
    rm -rf "$TEMP_DIR"
    
    # Клонируем репозиторий
    print_status "Загрузка исходного кода..."
    git clone "$REPO_URL" "$TEMP_DIR"
    
    # Переходим в директорию расширения
    cd "$TEMP_DIR/tex-lint-extension"
    
    # Устанавливаем зависимости
    print_status "Установка зависимостей..."
    npm install
    
    # Компилируем TypeScript
    print_status "Компиляция TypeScript..."
    npm run compile
    
    # Проверяем код линтером
    print_status "Проверка кода..."
    npm run lint || print_warning "Обнаружены предупреждения линтера (не критично)"
    
    # Упаковываем расширение
    print_status "Упаковка расширения..."
    local PACKAGE_NAME="checklatex-extension-$(date +%Y%m%d_%H%M%S).vsix"
    vsce package --out "$PACKAGE_NAME"
    
    # Устанавливаем расширение
    print_status "Установка расширения в VS Code..."
    
    if command -v code &> /dev/null; then
        code --install-extension "$PACKAGE_NAME" --force
        print_success "Расширение установлено в VS Code"
    else
        print_warning "VS Code CLI не доступен. Установите расширение вручную: $TEMP_DIR/tex-lint-extension/$PACKAGE_NAME"
    fi
    
    # Устанавливаем в Insiders если есть
    if command -v code-insiders &> /dev/null; then
        code-insiders --install-extension "$PACKAGE_NAME" --force
        print_success "Расширение установлено в VS Code Insiders"
    fi
    
    # Копируем пакет в домашнюю директорию для резерва
    cp "$PACKAGE_NAME" "$HOME/" 2>/dev/null || true
    print_status "Пакет сохранен в: $HOME/$PACKAGE_NAME"
}

# Проверка установки
verify_installation() {
    print_status "Проверка установки..."
    
    if command -v code &> /dev/null; then
        if code --list-extensions | grep -i checklatex &> /dev/null; then
            print_success "Расширение успешно установлено в VS Code"
        else
            print_error "Не удалось найти установленное расширение в VS Code"
            return 1
        fi
    fi
    
    # Показываем инструкции по использованию
    print_status "=== Расширение готово к использованию ==="
    echo ""
    echo "📋 Основные команды:"
    echo "  • Ctrl+Alt+Shift+S - Полный анализ проекта"
    echo "  • Ctrl+Alt+Shift+T - Быстрая настройка интервала"
    echo "  • Ctrl+Shift+P -> 'CheckLaTeX' - Все команды"
    echo ""
    echo "⚙️  Настройки (File > Preferences > Settings > CheckLaTeX):"
    echo "  • Server URL: http://localhost:5000 (по умолчанию)"
    echo "  • Auto Analyze: включить автоматический анализ"
    echo "  • Periodic Check: периодические проверки"
    echo ""
    echo "📁 Для работы расширения нужен запущенный CheckLaTeX сервер"
    echo "   Запустите сервер согласно DEPLOYMENT_GUIDE.md"
}

# Основное выполнение
main() {
    check_dependencies
    install_vsce
    remove_previous_extension
    install_extension
    verify_installation
    
    print_success "=== Установка завершена успешно! ==="
    
    # Очистка временных файлов
    rm -rf "/tmp/checklatex-extension-install"
}

# Обработка ошибок
trap 'print_error "Произошла ошибка во время установки"; exit 1' ERR

# Запуск основной функции
main
```

### Скрипт удаления расширения (uninstall-extension.sh)

```bash
#!/bin/bash

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_status "=== Удаление CheckLaTeX VS Code Extension ==="

# Удаление из VS Code
if command -v code &> /dev/null; then
    print_status "Поиск установленных расширений CheckLaTeX в VS Code..."
    
    EXTENSIONS=$(code --list-extensions | grep -i checklatex || true)
    
    if [ -n "$EXTENSIONS" ]; then
        echo "$EXTENSIONS" | while read -r extension; do
            if [ -n "$extension" ]; then
                print_status "Удаляем расширение: $extension"
                code --uninstall-extension "$extension"
            fi
        done
        print_success "Расширения удалены из VS Code"
    else
        print_warning "Расширения CheckLaTeX не найдены в VS Code"
    fi
else
    print_warning "VS Code CLI не найден"
fi

# Удаление из VS Code Insiders
if command -v code-insiders &> /dev/null; then
    print_status "Поиск установленных расширений CheckLaTeX в VS Code Insiders..."
    
    EXTENSIONS_INSIDERS=$(code-insiders --list-extensions | grep -i checklatex || true)
    
    if [ -n "$EXTENSIONS_INSIDERS" ]; then
        echo "$EXTENSIONS_INSIDERS" | while read -r extension; do
            if [ -n "$extension" ]; then
                print_status "Удаляем расширение из Insiders: $extension"
                code-insiders --uninstall-extension "$extension"
            fi
        done
        print_success "Расширения удалены из VS Code Insiders"
    else
        print_warning "Расширения CheckLaTeX не найдены в VS Code Insiders"
    fi
fi

# Удаление .vsix файлов из домашней директории
print_status "Удаление .vsix файлов..."
find "$HOME" -name "*checklatex*extension*.vsix" -type f -delete 2>/dev/null || true

# Удаление временных файлов
print_status "Удаление временных файлов..."
rm -rf /tmp/checklatex-extension-*

print_success "CheckLaTeX расширение полностью удалено"
```

## Разработка и отладка

### Режим разработки

```bash
# Запуск в режиме watch для автоматической перекомпиляции
npm run watch

# В другом терминале - запуск тестов
npm run test

# Проверка кода линтером
npm run lint

# Автоматическое исправление линтером
npx eslint src --ext ts --fix
```

### Отладка в VS Code

1. Откройте папку `tex-lint-extension` в VS Code
2. Нажмите `F5` для запуска Extension Development Host
3. В новом окне VS Code откройте LaTeX проект
4. Установите точки останова в `src/extension.ts`
5. Используйте команды расширения для отладки

### Логирование и диагностика

```typescript
// В extension.ts доступны функции логирования:
console.log('Debug info');
vscode.window.showInformationMessage('Info message');
vscode.window.showWarningMessage('Warning message');
vscode.window.showErrorMessage('Error message');

// Просмотр логов в VS Code:
// View > Output > CheckLaTeX Extension
```

## Настройка расширения

### Основные настройки (settings.json)

```json
{
    "checklatex.serverUrl": "http://localhost:5000",
    "checklatex.autoAnalyze": true,
    "checklatex.autoAnalyzeScope": "project",
    "checklatex.autoAnalyzeDelay": 1000,
    "checklatex.periodicCheck": false,
    "checklatex.periodicCheckInterval": 300000,
    "checklatex.timeout": 30000,
    "checklatex.excludePatterns": [
        "**/node_modules/**",
        "**/build/**",
        "**/dist/**",
        "**/.git/**"
    ]
}
```

### Горячие клавиши (keybindings.json)

```json
[
    {
        "key": "ctrl+alt+shift+s",
        "command": "checklatex.analyzeProjectFull",
        "when": "workspaceFolderCount > 0"
    },
    {
        "key": "ctrl+alt+shift+t", 
        "command": "checklatex.setQuickInterval",
        "when": "workspaceFolderCount > 0"
    },
    {
        "key": "ctrl+alt+l",
        "command": "checklatex.analyzeCurrentFile",
        "when": "resourceExtname == .tex"
    }
]
```

## Развертывание в Marketplace

### Подготовка к публикации

```bash
# Обновите версию в package.json
npm version patch  # или minor, major

# Обновите CHANGELOG.md
# Обновите README.md

# Создайте финальный пакет
vsce package
```

### Публикация

```bash
# Получите Personal Access Token из Azure DevOps
# https://dev.azure.com/

# Войдите в vsce
vsce login <publisher-name>

# Опубликуйте расширение
vsce publish

# Или опубликуйте конкретную версию
vsce publish 1.0.1
```

## Устранение неполадок

### Частые проблемы

1. **Ошибка компиляции TypeScript**:
   ```bash
   # Очистите node_modules и переустановите
   rm -rf node_modules package-lock.json
   npm install
   npm run compile
   ```

2. **Ошибка "Cannot find module"**:
   ```bash
   # Проверьте версию Node.js
   node --version
   # Переустановите зависимости
   npm install
   ```

3. **Расширение не активируется**:
   - Проверьте логи: `View > Output > CheckLaTeX Extension`
   - Убедитесь, что открыт .tex файл или проект с .tex файлами
   - Перезагрузите VS Code

4. **Не подключается к серверу**:
   - Проверьте URL сервера в настройках
   - Убедитесь, что CheckLaTeX backend запущен
   - Проверьте файрвол и сетевые настройки

5. **Проблемы с упаковкой**:
   ```bash
   # Обновите vsce
   npm install -g vsce@latest
   
   # Очистите кэш
   npm cache clean --force
   ```

### Логи и диагностика

```bash
# Логи VS Code (Linux)
tail -f ~/.config/Code/logs/*/main.log

# Логи VS Code (macOS)  
tail -f ~/Library/Application\ Support/Code/logs/*/main.log

# Логи VS Code (Windows)
# %APPDATA%\Code\logs\*\main.log

# Проверка установленных расширений
code --list-extensions --show-versions | grep -i checklatex
```

## Обновление расширения

### Автоматическое обновление

```bash
# Создайте скрипт update-extension.sh
#!/bin/bash
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install-extension.sh | bash
```

### Ручное обновление

```bash
# Скачайте новую версию
git pull origin main

# Переустановите расширение
cd tex-lint-extension
npm install
npm run compile
vsce package
code --install-extension *.vsix --force
```

## Интеграция с CI/CD

### GitHub Actions для автоматической сборки

```yaml
# .github/workflows/build-extension.yml
name: Build Extension

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup Node.js
      uses: actions/setup-node@v3
      with:
        node-version: '18'
        cache: 'npm'
        cache-dependency-path: tex-lint-extension/package-lock.json
    
    - name: Install dependencies
      run: |
        cd tex-lint-extension
        npm ci
    
    - name: Lint code
      run: |
        cd tex-lint-extension
        npm run lint
    
    - name: Compile TypeScript
      run: |
        cd tex-lint-extension
        npm run compile
    
    - name: Package extension
      run: |
        cd tex-lint-extension
        npm install -g vsce
        vsce package
    
    - name: Upload artifact
      uses: actions/upload-artifact@v3
      with:
        name: checklatex-extension
        path: tex-lint-extension/*.vsix
```

---

**Готово!** CheckLaTeX VS Code Extension успешно установлено и готово к работе с LaTeX проектами.

**📋 Быстрый старт:**
1. Убедитесь, что CheckLaTeX backend запущен (`http://localhost:5000`)
2. Откройте проект с .tex файлами в VS Code
3. Нажмите `Ctrl+Alt+Shift+S` для полного анализа проекта
4. Настройте автоматический анализ в Settings > CheckLaTeX

**🔧 Поддержка:** При возникновении проблем проверьте логи в `View > Output > CheckLaTeX Extension`