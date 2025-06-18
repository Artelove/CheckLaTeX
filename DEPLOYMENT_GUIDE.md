# Руководство по развертыванию CheckLaTeX

Это руководство описывает полный процесс развертывания CheckLaTeX backend API на сервере с нуля.

## Требования к системе

- Ubuntu 20.04/22.04 LTS или Debian 11+ (рекомендуется)
- Минимум 1 GB RAM 
- 2 GB свободного места на диске
- Доступ к интернету для загрузки пакетов

## Шаг 1: Обновление системы и установка базовых пакетов

### 1.1 Обновление списков пакетов и системы

```bash
# Обновляем списки пакетов
sudo apt update

# Обновляем установленные пакеты
sudo apt upgrade -y

# Устанавливаем базовые инструменты
sudo apt install -y wget curl git unzip
```

### 1.2 Проверка обновлений

```bash
# Проверяем наличие обновлений безопасности
sudo apt list --upgradable

# При необходимости перезагружаем систему
sudo reboot
```

## Шаг 2: Установка .NET 8.0 SDK

### 2.1 Добавление репозитория Microsoft

#### Для Ubuntu 20.04:
```bash
# Скачиваем и устанавливаем Microsoft package signing key
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

#### Для Ubuntu 22.04:
```bash
# Скачиваем и устанавливаем Microsoft package signing key
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

#### Для Debian 11:
```bash
# Скачиваем и устанавливаем Microsoft package signing key
wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

### 2.2 Обновление списков пакетов после добавления Microsoft репозитория

```bash
sudo apt update
```

### 2.3 Установка .NET 8.0 SDK

```bash
# Устанавливаем .NET 8.0 SDK (включает runtime)
sudo apt install -y dotnet-sdk-8.0

# Проверяем установку
dotnet --version

# Проверяем доступные SDK
dotnet --list-sdks

# Проверяем доступные runtimes
dotnet --list-runtimes
```

Вы должны увидеть вывод похожий на:
```
8.0.xxx
```

### 2.4 Настройка переменных окружения (опционально)

```bash
# Добавляем .NET tools в PATH
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc
```

## Шаг 3: Создание пользователя для приложения

```bash
# Создаем системного пользователя для запуска приложения
sudo useradd --system --shell /bin/false --home /opt/checklatex --create-home checklatex

# Создаем рабочие директории
sudo mkdir -p /opt/checklatex/app
sudo mkdir -p /opt/checklatex/logs
sudo mkdir -p /opt/checklatex/temp

# Настраиваем права доступа
sudo chown -R checklatex:checklatex /opt/checklatex
```

## Шаг 4: Получение и сборка приложения

### 4.1 Клонирование репозитория

```bash
# Переходим в домашнюю директорию
cd ~

# Клонируем репозиторий
git clone https://github.com/Artelove/CheckLaTeX.git
cd CheckLaTeX
```

### 4.2 Сборка приложения

```bash
# Переходим в директорию проекта
cd tex-lint

# Восстанавливаем зависимости
dotnet restore

# Собираем проект
dotnet build -c Release

# Публикуем приложение
dotnet publish -c Release -o /tmp/checklatex-publish --self-contained false
```

### 4.3 Развертывание на сервере

```bash
# Копируем опубликованное приложение
sudo cp -r /tmp/checklatex-publish/* /opt/checklatex/app/

# Копируем конфигурационные файлы из корня проекта
cd ..
sudo cp lint-rules.json /opt/checklatex/app/
sudo cp commands.json /opt/checklatex/app/
sudo cp environments.json /opt/checklatex/app/

# Настраиваем права доступа
sudo chown -R checklatex:checklatex /opt/checklatex

# Делаем исполняемый файл исполняемым
sudo chmod +x /opt/checklatex/app/tex-lint

# Очищаем временные файлы
rm -rf /tmp/checklatex-publish
```

## Шаг 5: Настройка systemd сервиса

### 5.1 Создание файла сервиса

```bash
sudo nano /etc/systemd/system/checklatex.service
```

### 5.2 Содержимое файла сервиса

```ini
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
```

> **Примечание**: Для повышения безопасности в production среде можно добавить дополнительные директивы безопасности:
> ```ini
> # Security settings (добавить после проверки работоспособности)
> NoNewPrivileges=true
> PrivateTmp=true
> ProtectSystem=strict
> ProtectHome=true
> ReadWritePaths=/opt/checklatex/logs /opt/checklatex/temp
> 
> # Resource limits
> LimitNOFILE=65536
> LimitNPROC=4096
> ```

### 5.3 Активация и запуск сервиса

```bash
# Перезагружаем systemd для чтения нового сервиса
sudo systemctl daemon-reload

# Включаем автозапуск сервиса
sudo systemctl enable checklatex

# Запускаем сервис
sudo systemctl start checklatex

# Проверяем статус
sudo systemctl status checklatex
```

## Шаг 6: Настройка firewall

### 6.1 Установка и настройка UFW (если не установлен)

```bash
# Устанавливаем UFW
sudo apt install -y ufw

# Разрешаем SSH (важно сделать до включения firewall!)
sudo ufw allow ssh

# Разрешаем порт 5000 для CheckLaTeX API
sudo ufw allow 5000/tcp

# Включаем firewall
sudo ufw enable

# Проверяем статус
sudo ufw status
```

### 6.2 Альтернативно через iptables

```bash
# Разрешаем входящие соединения на порт 5000
sudo iptables -A INPUT -p tcp --dport 5000 -j ACCEPT

# Сохраняем правила (для Ubuntu/Debian)
sudo apt install -y iptables-persistent
sudo netfilter-persistent save
```

## Шаг 7: Проверка развертывания

### 7.1 Проверка работы сервиса

```bash
# Проверяем статус сервиса
sudo systemctl status checklatex

# Проверяем логи
sudo journalctl -u checklatex -f

# Проверяем, что порт прослушивается
sudo netstat -tlnp | grep :5000
# или
sudo ss -tlnp | grep :5000
```

### 7.2 Тестирование API

```bash
# Проверяем доступность Swagger UI
curl -I http://localhost:5000/swagger

# Проверяем health check (если реализован)
curl http://localhost:5000/health

# Тестируем API endpoint
curl -X GET http://localhost:5000/api/diagnostic/test
```

### 7.3 Проверка с внешней машины

```bash
# Замените YOUR_SERVER_IP на IP адрес вашего сервера
curl http://YOUR_SERVER_IP:5000/swagger
```

Откройте в браузере: `http://YOUR_SERVER_IP:5000/swagger`

## Шаг 8: Настройка reverse proxy (опционально)

Если вы хотите использовать стандартные порты 80/443, настройте nginx.

### 8.1 Установка nginx

```bash
sudo apt install -y nginx
```

### 8.2 Настройка конфигурации

```bash
sudo nano /etc/nginx/sites-available/checklatex
```

Содержимое файла:
```nginx
server {
    listen 80;
    server_name your-domain.com www.your-domain.com;  # замените на ваш домен

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_buffering off;
        proxy_read_timeout 300s;
        proxy_connect_timeout 60s;
        proxy_send_timeout 300s;
    }
}
```

### 8.3 Активация конфигурации

```bash
# Создаем символическую ссылку
sudo ln -s /etc/nginx/sites-available/checklatex /etc/nginx/sites-enabled/

# Удаляем дефолтный сайт
sudo rm -f /etc/nginx/sites-enabled/default

# Проверяем конфигурацию
sudo nginx -t

# Перезагружаем nginx
sudo systemctl reload nginx

# Добавляем nginx в автозапуск
sudo systemctl enable nginx
```

### 8.4 Обновление firewall для nginx

```bash
# Разрешаем HTTP и HTTPS
sudo ufw allow 'Nginx Full'

# Удаляем прямой доступ к порту 5000 (опционально)
sudo ufw delete allow 5000/tcp
```

## Шаг 9: Управление сервисом

### 9.1 Основные команды

```bash
# Просмотр статуса
sudo systemctl status checklatex

# Запуск сервиса
sudo systemctl start checklatex

# Остановка сервиса
sudo systemctl stop checklatex

# Перезапуск сервиса
sudo systemctl restart checklatex

# Перезагрузка конфигурации
sudo systemctl reload checklatex

# Просмотр логов в реальном времени
sudo journalctl -u checklatex -f

# Просмотр логов за последние 2 часа
sudo journalctl -u checklatex --since "2 hours ago"
```

### 9.2 Мониторинг

```bash
# Проверка использования ресурсов
sudo systemctl status checklatex
htop

# Проверка использования диска
df -h /opt/checklatex

# Проверка сетевых соединений
sudo netstat -tupln | grep :5000
```

## Шаг 10: Обновление приложения

### 10.1 Создание скрипта обновления

```bash
sudo nano /opt/checklatex/update.sh
```

Содержимое скрипта:
```bash
#!/bin/bash
set -e

echo "=== CheckLaTeX Update Script ==="

# Переменные
APP_DIR="/opt/checklatex/app"
BACKUP_DIR="/opt/checklatex/backups"
TEMP_DIR="/tmp/checklatex-update"
REPO_URL="https://github.com/Artelove/CheckLaTeX.git"

# Создаем директорию для бэкапов
sudo mkdir -p $BACKUP_DIR

# Создаем бэкап текущей версии
BACKUP_NAME="backup-$(date +%Y%m%d_%H%M%S)"
echo "Creating backup: $BACKUP_NAME"
sudo cp -r $APP_DIR $BACKUP_DIR/$BACKUP_NAME

# Останавливаем сервис
echo "Stopping CheckLaTeX service..."
sudo systemctl stop checklatex

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
sudo rm -rf $APP_DIR/*
sudo cp -r /tmp/checklatex-new/* $APP_DIR/

# Копируем конфигурационные файлы
cd $TEMP_DIR
sudo cp lint-rules.json $APP_DIR/
sudo cp commands.json $APP_DIR/
sudo cp environments.json $APP_DIR/

# Настраиваем права
sudo chown -R checklatex:checklatex /opt/checklatex
sudo chmod +x $APP_DIR/tex-lint

# Запускаем сервис
echo "Starting CheckLaTeX service..."
sudo systemctl start checklatex

# Проверяем статус
sleep 5
if sudo systemctl is-active --quiet checklatex; then
    echo "✅ Update completed successfully!"
    echo "Service is running on: http://localhost:5000/swagger"
else
    echo "❌ Update failed! Restoring backup..."
    sudo systemctl stop checklatex
    sudo rm -rf $APP_DIR/*
    sudo cp -r $BACKUP_DIR/$BACKUP_NAME/* $APP_DIR/
    sudo systemctl start checklatex
    echo "Backup restored. Check logs: sudo journalctl -u checklatex"
fi

# Очистка
rm -rf $TEMP_DIR /tmp/checklatex-new

echo "=== Update process completed ==="
```

### 10.2 Использование скрипта обновления

```bash
# Делаем скрипт исполняемым
sudo chmod +x /opt/checklatex/update.sh

# Запускаем обновление
sudo /opt/checklatex/update.sh
```

## Устранение неполадок

### Проверка логов
```bash
# Системные логи
sudo journalctl -u checklatex --no-pager -l

# Логи за последний час
sudo journalctl -u checklatex --since "1 hour ago"

# Логи приложения (если настроено file logging)
sudo tail -f /opt/checklatex/logs/app.log
```

### Проверка конфигурации
```bash
# Проверка прав доступа
ls -la /opt/checklatex/app/

# Проверка конфигурационных файлов
ls -la /opt/checklatex/app/*.json

# Проверка .NET runtime
dotnet --info
```

### Частые проблемы

1. **Сервис не запускается**: Проверьте логи `sudo journalctl -u checklatex`

2. **Ошибка NAMESPACE в логах**: Если видите ошибку "Failed to set up mount namespacing", отключите строгие настройки безопасности в systemd конфигурации:
   ```bash
   sudo nano /etc/systemd/system/checklatex.service
   # Уберите строки: ProtectSystem, ProtectHome, PrivateTmp, ReadWritePaths
   sudo systemctl daemon-reload
   sudo systemctl restart checklatex
   ```

3. **Порт занят**: Проверьте `sudo netstat -tlnp | grep 5000`

4. **Нет доступа к файлам**: Проверьте права `sudo chown -R checklatex:checklatex /opt/checklatex`

5. **Ошибки nullable reference types**: Это предупреждения компилятора, не влияющие на работу приложения

## Безопасность

### Рекомендации
- Регулярно обновляйте систему: `sudo apt update && sudo apt upgrade`
- Мониторьте логи на предмет подозрительной активности
- Используйте fail2ban для защиты от брутфорса
- Настройте SSL/TLS сертификаты (Let's Encrypt)
- Ограничьте доступ к серверу через SSH ключи

---

**Поздравляем!** CheckLaTeX успешно развернут и готов к работе. API доступен по адресу `http://your-server:5000/swagger`.