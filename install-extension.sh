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
            if [ -n "$extension" ]; then
                print_status "Удаляем расширение: $extension"
                code --uninstall-extension "$extension" 2>/dev/null || true
            fi
        done
    fi
    
    # Также удаляем из Insiders если есть
    if command -v code-insiders &> /dev/null; then
        code-insiders --list-extensions | grep -i checklatex | while read -r extension; do
            if [ -n "$extension" ]; then
                print_status "Удаляем расширение из VS Code Insiders: $extension"
                code-insiders --uninstall-extension "$extension" 2>/dev/null || true
            fi
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