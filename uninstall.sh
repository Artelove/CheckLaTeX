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

# Проверка прав root
if [[ $EUID -ne 0 ]]; then
   print_error "Этот скрипт должен быть запущен от имени root (sudo)"
   exit 1
fi

print_status "=== Удаление CheckLaTeX Backend ==="

# Останавливаем и удаляем сервис
print_status "Остановка и удаление сервиса..."
systemctl stop checklatex 2>/dev/null || true
systemctl disable checklatex 2>/dev/null || true
rm -f /etc/systemd/system/checklatex.service

# Удаляем пользователя и группу
print_status "Удаление пользователя..."
userdel -r checklatex 2>/dev/null || true
groupdel checklatex 2>/dev/null || true

# Удаляем файлы приложения
print_status "Удаление файлов приложения..."
rm -rf /opt/checklatex

# Удаляем временные файлы
print_status "Удаление временных файлов..."
rm -rf /tmp/checklatex-*
# Удаляем клонированный репозиторий если есть
if [ -d "/tmp/CheckLaTeX" ]; then
    rm -rf /tmp/CheckLaTeX
fi

if [ -d "CheckLaTeX" ]; then
    rm -rf CheckLaTeX
fi

# Перезагружаем systemd
print_status "Перезагрузка systemd..."
systemctl daemon-reload

# Удаляем правила firewall (опционально)
echo ""
echo -n "Удалить правила firewall для порта 5000? (y/N): "
read -r REPLY
if [[ $REPLY =~ ^[Yy]$ ]]; then
    ufw delete allow 5000/tcp 2>/dev/null || true
    print_status "Правила firewall удалены"
else
    print_status "Правила firewall оставлены без изменений"
fi

echo ""
print_success "CheckLaTeX полностью удален из системы"

# Показываем что удалили
echo ""
print_status "Удаленные компоненты:"
echo "  ✓ SystemD сервис checklatex"
echo "  ✓ Пользователь и группа checklatex" 
echo "  ✓ Файлы приложения в /opt/checklatex"
echo "  ✓ Временные файлы"

echo ""
print_status "Для повторной установки выполните:"
echo "  curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install.sh | sudo bash" 