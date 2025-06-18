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