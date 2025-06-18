# Руководство по развертыванию CheckLaTeX

Это руководство описывает процесс развертывания CheckLaTeX backend API на сервере с нуля.

## Быстрая установка (автоматический скрипт)

### Прямая установка из репозитория

Когда скрипты будут загружены в основной репозиторий:

```bash
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install.sh | sudo bash
```

### Локальная установка (сейчас)

Пока скрипты не в репозитории, скачайте их локально:

```bash
# Скачайте файлы install.sh и uninstall.sh в вашу директорию
# Затем выполните:
chmod +x install.sh uninstall.sh
sudo ./install.sh
```

## Требования к системе

- Ubuntu 20.04/22.04 LTS или Debian 11+ (рекомендуется)
- Минимум 1 GB RAM 
- 2 GB свободного места на диске
- Доступ к интернету для загрузки пакетов
- Права sudo/root

## Полная переустановка

Если нужно полностью переустановить CheckLaTeX, выполните команды очистки:

### Автоматическая очистка

```bash
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/uninstall.sh | sudo bash
```

### Ручная очистка

```bash
# Останавливаем и удаляем сервис
sudo systemctl stop checklatex 2>/dev/null || true
sudo systemctl disable checklatex 2>/dev/null || true
sudo rm -f /etc/systemd/system/checklatex.service

# Удаляем пользователя и группу
sudo userdel -r checklatex 2>/dev/null || true
sudo groupdel checklatex 2>/dev/null || true

# Удаляем файлы приложения
sudo rm -rf /opt/checklatex

# Удаляем временные файлы
sudo rm -rf /tmp/checklatex-*

# Перезагружаем systemd
sudo systemctl daemon-reload

echo "✅ CheckLaTeX полностью удален"
```

## Ручная установка (пошагово)

### Шаг 1: Обновление системы

```bash
# Обновляем систему
sudo apt update && sudo apt upgrade -y

# Устанавливаем базовые инструменты
sudo apt install -y wget curl git unzip net-tools
```

### Шаг 2: Установка .NET 8.0 SDK

#### Для Ubuntu 20.04:
```bash
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

#### Для Ubuntu 22.04:
```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

#### Для Debian 11:
```bash
wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

```bash
# Обновляем списки пакетов и устанавливаем .NET 8.0 SDK
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Проверяем установку
dotnet --version
```

### Шаг 3: Подготовка системы

```bash
# Создаем системного пользователя
sudo useradd --system --shell /bin/false --home /opt/checklatex --create-home checklatex

# Создаем рабочие директории
sudo mkdir -p /opt/checklatex/{app,logs,temp,backups}

# Настраиваем права доступа
sudo chown -R checklatex:checklatex /opt/checklatex
```

### Шаг 4: Сборка и развертывание

```bash
# Клонируем репозиторий
cd /tmp
git clone https://github.com/Artelove/CheckLaTeX.git
cd CheckLaTeX/tex-lint

# Собираем приложение
dotnet restore
dotnet publish -c Release -o /tmp/checklatex-publish --self-contained false

# Развертываем приложение
sudo cp -r /tmp/checklatex-publish/* /opt/checklatex/app/

# Копируем конфигурационные файлы
cd /tmp/CheckLaTeX
sudo cp lint-rules.json commands.json environments.json /opt/checklatex/app/

# Настраиваем права
sudo chown -R checklatex:checklatex /opt/checklatex
sudo chmod +x /opt/checklatex/app/tex-lint

# Очищаем временные файлы
rm -rf /tmp/CheckLaTeX /tmp/checklatex-publish
```

### Шаг 5: Настройка systemd сервиса

```bash
sudo tee /etc/systemd/system/checklatex.service > /dev/null << 'EOF'
[Unit]
Description=CheckLaTeX LaTeX Linting Web API
Documentation=https://github.com/Artelove/CheckLaTeX
After=network.target
Wants=network.target

[Service]
Type=notify
User=checklatex
Group=checklatex
WorkingDirectory=/opt/checklatex/app
ExecStart=/usr/bin/dotnet /opt/checklatex/app/tex-lint.dll

# Restart policy
Restart=always
RestartSec=10
KillSignal=SIGINT
TimeoutStopSec=30

# Environment variables
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=checklatex

[Install]
WantedBy=multi-user.target
EOF
```

### Шаг 6: Запуск сервиса

```bash
# Перезагружаем systemd
sudo systemctl daemon-reload

# Включаем автозапуск
sudo systemctl enable checklatex

# Запускаем сервис
sudo systemctl start checklatex

# Проверяем статус
sudo systemctl status checklatex
```

### Шаг 7: Настройка firewall

```bash
# Устанавливаем UFW (если не установлен)
sudo apt install -y ufw

# Разрешаем SSH и порт 5000
sudo ufw allow ssh
sudo ufw allow 5000/tcp

# Включаем firewall (осторожно с SSH!)
sudo ufw --force enable

# Проверяем статус
sudo ufw status
```

### Шаг 8: Проверка работы

```bash
# Проверяем статус сервиса
sudo systemctl status checklatex

# Проверяем порты
sudo netstat -tlnp | grep :5000

# Тестируем API
curl -I http://localhost:5000/swagger

# Проверяем логи
sudo journalctl -u checklatex --no-pager -l
```

## Скрипты автоматической установки

### Скрипт установки (install.sh)

```bash
#!/bin/bash
set -e

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Функция для красивого вывода
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

print_status "=== Установка CheckLaTeX Backend ==="

# Определяем дистрибутив
if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$NAME
    VER=$VERSION_ID
else
    print_error "Не удалось определить дистрибутив"
    exit 1
fi

print_status "Обнаружен дистрибутив: $OS $VER"

# Очистка предыдущей установки
print_status "Очистка предыдущей установки..."
systemctl stop checklatex 2>/dev/null || true
systemctl disable checklatex 2>/dev/null || true
rm -f /etc/systemd/system/checklatex.service
userdel -r checklatex 2>/dev/null || true
groupdel checklatex 2>/dev/null || true
rm -rf /opt/checklatex
rm -rf /tmp/checklatex-*
systemctl daemon-reload

# Обновление системы
print_status "Обновление системы..."
apt update
apt upgrade -y
apt install -y wget curl git unzip net-tools

# Установка .NET 8.0 SDK
print_status "Установка .NET 8.0 SDK..."

# Определяем правильную версию пакета для Microsoft репозитория
if [[ "$OS" == *"Ubuntu"* ]]; then
    if [[ "$VER" == "20.04" ]]; then
        wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    elif [[ "$VER" == "22.04" ]]; then
        wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    else
        # Для других версий Ubuntu используем 22.04
        wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    fi
elif [[ "$OS" == *"Debian"* ]]; then
    wget -q https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
else
    print_error "Неподдерживаемый дистрибутив: $OS"
    exit 1
fi

dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

apt update
apt install -y dotnet-sdk-8.0

# Проверяем установку .NET
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK не установлен"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
print_success ".NET SDK установлен: версия $DOTNET_VERSION"

# Создание пользователя и директорий
print_status "Создание пользователя и директорий..."
useradd --system --shell /bin/false --home /opt/checklatex --create-home checklatex
mkdir -p /opt/checklatex/{app,logs,temp,backups}

# Клонирование и сборка приложения
print_status "Загрузка и сборка приложения..."
cd /tmp
git clone https://github.com/Artelove/CheckLaTeX.git
cd CheckLaTeX/tex-lint

print_status "Восстановление зависимостей..."
dotnet restore

print_status "Сборка приложения..."
dotnet publish -c Release -o /tmp/checklatex-publish --self-contained false

# Развертывание приложения
print_status "Развертывание приложения..."
cp -r /tmp/checklatex-publish/* /opt/checklatex/app/

# Копирование конфигурационных файлов
cd /tmp/CheckLaTeX
cp lint-rules.json commands.json environments.json /opt/checklatex/app/

# Настройка прав доступа
chown -R checklatex:checklatex /opt/checklatex
chmod +x /opt/checklatex/app/tex-lint

# Создание systemd сервиса
print_status "Создание systemd сервиса..."
cat > /etc/systemd/system/checklatex.service << 'EOF'
[Unit]
Description=CheckLaTeX LaTeX Linting Web API
Documentation=https://github.com/Artelove/CheckLaTeX
After=network.target
Wants=network.target

[Service]
Type=notify
User=checklatex
Group=checklatex
WorkingDirectory=/opt/checklatex/app
ExecStart=/usr/bin/dotnet /opt/checklatex/app/tex-lint.dll

# Restart policy
Restart=always
RestartSec=10
KillSignal=SIGINT
TimeoutStopSec=30

# Environment variables
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=checklatex

[Install]
WantedBy=multi-user.target
EOF

# Настройка firewall
print_status "Настройка firewall..."
apt install -y ufw
ufw allow ssh
ufw allow 5000/tcp
ufw --force enable

# Запуск сервиса
print_status "Запуск сервиса..."
systemctl daemon-reload
systemctl enable checklatex
systemctl start checklatex

# Ожидание запуска
sleep 5

# Проверка статуса
if systemctl is-active --quiet checklatex; then
    print_success "CheckLaTeX успешно установлен и запущен!"
    print_status "API доступен по адресу: http://$(hostname -I | awk '{print $1}'):5000/swagger"
    print_status "Локальный адрес: http://localhost:5000/swagger"
    
    # Показываем статус
    echo ""
    print_status "Статус сервиса:"
    systemctl status checklatex --no-pager -l
    
    echo ""
    print_status "Полезные команды:"
    echo "  Статус сервиса:    sudo systemctl status checklatex"
    echo "  Логи сервиса:      sudo journalctl -u checklatex -f"
    echo "  Перезапуск:        sudo systemctl restart checklatex"
    echo "  Остановка:         sudo systemctl stop checklatex"
    echo "  Обновление:        sudo /opt/checklatex/update.sh"
    
else
    print_error "Ошибка запуска сервиса!"
    print_status "Логи ошибок:"
    journalctl -u checklatex --no-pager -l
    exit 1
fi

# Очистка временных файлов
print_status "Очистка временных файлов..."
cd /
rm -rf /tmp/CheckLaTeX /tmp/checklatex-publish

# Создание скрипта обновления
print_status "Создание скрипта обновления..."
cat > /opt/checklatex/update.sh << 'EOFUPDATE'
#!/bin/bash
set -e

echo "=== CheckLaTeX Update Script ==="

# Переменные
APP_DIR="/opt/checklatex/app"
BACKUP_DIR="/opt/checklatex/backups"
TEMP_DIR="/tmp/checklatex-update"
REPO_URL="https://github.com/Artelove/CheckLaTeX.git"

# Создаем директорию для бэкапов
mkdir -p $BACKUP_DIR

# Создаем бэкап текущей версии
BACKUP_NAME="backup-$(date +%Y%m%d_%H%M%S)"
echo "Creating backup: $BACKUP_NAME"
cp -r $APP_DIR $BACKUP_DIR/$BACKUP_NAME

# Останавливаем сервис
echo "Stopping CheckLaTeX service..."
systemctl stop checklatex

# Клонируем последнюю версию
echo "Downloading latest version..."
rm -rf $TEMP_DIR
git clone $REPO_URL $TEMP_DIR

# Собираем новую версию
echo "Building application..."
cd $TEMP_DIR/tex-lint
dotnet restore
dotnet publish -c Release -o /tmp/checklatex-new --self-contained false

# Заменяем файлы
echo "Deploying new version..."
rm -rf $APP_DIR/*
cp -r /tmp/checklatex-new/* $APP_DIR/

# Копируем конфигурационные файлы
cd $TEMP_DIR
cp lint-rules.json $APP_DIR/
cp commands.json $APP_DIR/
cp environments.json $APP_DIR/

# Настраиваем права
chown -R checklatex:checklatex /opt/checklatex
chmod +x $APP_DIR/tex-lint

# Запускаем сервис
echo "Starting CheckLaTeX service..."
systemctl start checklatex

# Проверяем статус
sleep 5
if systemctl is-active --quiet checklatex; then
    echo "✅ Update completed successfully!"
    echo "Service is running on: http://localhost:5000/swagger"
else
    echo "❌ Update failed! Restoring backup..."
    systemctl stop checklatex
    rm -rf $APP_DIR/*
    cp -r $BACKUP_DIR/$BACKUP_NAME/* $APP_DIR/
    systemctl start checklatex
    echo "Backup restored. Check logs: journalctl -u checklatex"
fi

# Очистка
rm -rf $TEMP_DIR /tmp/checklatex-new

echo "=== Update process completed ==="
EOFUPDATE

chmod +x /opt/checklatex/update.sh

print_success "=== Установка завершена успешно! ==="
```

### Скрипт удаления (uninstall.sh)

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

# Проверка прав root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}[ERROR]${NC} Этот скрипт должен быть запущен от имени root (sudo)"
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

# Перезагружаем systemd
systemctl daemon-reload

# Удаляем правила firewall (опционально)
read -p "Удалить правила firewall для порта 5000? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    ufw delete allow 5000/tcp 2>/dev/null || true
    print_status "Правила firewall удалены"
fi

print_success "CheckLaTeX полностью удален из системы"
```

## Управление сервисом

### Основные команды

```bash
# Статус сервиса
sudo systemctl status checklatex

# Запуск/остановка/перезапуск
sudo systemctl start checklatex
sudo systemctl stop checklatex
sudo systemctl restart checklatex

# Просмотр логов
sudo journalctl -u checklatex -f
sudo journalctl -u checklatex --since "1 hour ago"

# Обновление приложения
sudo /opt/checklatex/update.sh
```

### Проверка работоспособности

```bash
# Проверка портов
sudo netstat -tlnp | grep :5000

# Тест API
curl -I http://localhost:5000/swagger

# Проверка логов на ошибки
sudo journalctl -u checklatex --no-pager | grep -i error
```

## Устранение неполадок

### Частые проблемы

1. **Сервис не запускается**: 
   ```bash
   sudo journalctl -u checklatex --no-pager -l
   ```

2. **Ошибка NAMESPACE**: Убрать строгие настройки безопасности из systemd конфигурации

3. **Порт занят**: 
   ```bash
   sudo netstat -tlnp | grep :5000
   sudo lsof -i :5000
   ```

4. **Нет доступа к файлам**: 
   ```bash
   sudo chown -R checklatex:checklatex /opt/checklatex
   ```

5. **Ошибки компиляции**: Это предупреждения nullable reference types, не влияют на работу

### Полная переустановка

```bash
# Автоматическая переустановка
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/uninstall.sh | sudo bash
curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install.sh | sudo bash
```

## Безопасность

- Сервис работает под отдельным пользователем `checklatex`
- Настроен firewall для ограничения доступа
- Логирование всех операций через systemd
- Регулярно обновляйте систему: `sudo apt update && sudo apt upgrade`

---

**Готово!** CheckLaTeX установлен и готов к работе. API доступен по адресу `http://your-server:5000/swagger`