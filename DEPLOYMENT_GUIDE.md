# Руководство по развертыванию CheckLaTeX

## Вариант 1: Прямое развертывание на сервере (Рекомендуется)

### Требования к серверу
- Ubuntu/Debian Linux (или другой Unix-совместимый сервер)
- .NET 6.0 Runtime
- Минимум 512 MB RAM
- 1 GB свободного места на диске

### Шаг 1: Установка .NET 6.0 Runtime на сервере

#### Для Ubuntu 20.04/22.04:
```bash
# Обновляем систему
sudo apt update

# Устанавливаем .NET 6.0 Runtime
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y aspnetcore-runtime-6.0
```

#### Для Debian 11:
```bash
# Обновляем систему
sudo apt update

# Устанавливаем .NET 6.0 Runtime
wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y aspnetcore-runtime-6.0
```

#### Для CentOS/RHEL 8:
```bash
# Добавляем Microsoft repository
sudo rpm -Uvh https://packages.microsoft.com/config/centos/8/packages-microsoft-prod.rpm

# Устанавливаем .NET 6.0 Runtime
sudo dnf install -y aspnetcore-runtime-6.0
```

### Шаг 2: Подготовка приложения на локальной машине

#### Сборка приложения:
```bash
# В папке проекта
cd tex-lint

# Публикуем приложение для Linux
dotnet publish -c Release -r linux-x64 --self-contained false -o ../publish

# Возвращаемся в корневую папку
cd ..

# Копируем конфигурационные файлы в папку публикации
cp lint-rules.json publish/
cp commands.json publish/
cp environments.json publish/
```

### Шаг 3: Загрузка на сервер

#### Создание архива:
```bash
# Создаем архив для передачи на сервер
tar -czf checklatex-app.tar.gz publish/
```

#### Передача на сервер (выберите один способ):

**Через SCP:**
```bash
scp checklatex-app.tar.gz user@your-server:/home/user/
```

**Через rsync:**
```bash
rsync -avz checklatex-app.tar.gz user@your-server:/home/user/
```

**Через FTP/SFTP:** используйте ваш любимый FTP клиент

### Шаг 4: Развертывание на сервере

```bash
# Подключаемся к серверу
ssh user@your-server

# Создаем папку для приложения
sudo mkdir -p /opt/checklatex
cd /home/user

# Распаковываем архив
tar -xzf checklatex-app.tar.gz

# Перемещаем файлы в системную папку
sudo mv publish/* /opt/checklatex/
sudo chown -R www-data:www-data /opt/checklatex
sudo chmod +x /opt/checklatex/tex-lint

# Делаем исполняемым
sudo chmod +x /opt/checklatex/tex-lint
```

### Шаг 5: Создание systemd сервиса

```bash
# Создаем файл сервиса
sudo nano /etc/systemd/system/checklatex.service
```

Содержимое файла `/etc/systemd/system/checklatex.service`:
```ini
[Unit]
Description=CheckLaTeX Web API Service
After=network.target

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=/opt/checklatex
ExecStart=/usr/bin/dotnet /opt/checklatex/tex-lint.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=checklatex
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:5000

[Install]
WantedBy=multi-user.target
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

### Шаг 7: Настройка firewall (если нужно)

```bash
# Открываем порт 5000
sudo ufw allow 5000/tcp

# Или через iptables
sudo iptables -A INPUT -p tcp --dport 5000 -j ACCEPT
```

### Шаг 8: Проверка работы

```bash
# Проверяем, что сервис слушает порт
sudo netstat -tlnp | grep :5000

# Проверяем API
curl http://localhost:5000/swagger

# Или с другой машины
curl http://your-server-ip:5000/swagger
```

### Управление сервисом

```bash
# Остановка сервиса
sudo systemctl stop checklatex

# Запуск сервиса
sudo systemctl start checklatex

# Перезапуск сервиса
sudo systemctl restart checklatex

# Просмотр логов
sudo journalctl -u checklatex -f

# Просмотр логов за последний час
sudo journalctl -u checklatex --since "1 hour ago"
```

### Обновление приложения

```bash
# Останавливаем сервис
sudo systemctl stop checklatex

# Создаем backup
sudo cp -r /opt/checklatex /opt/checklatex.backup.$(date +%Y%m%d_%H%M%S)

# Загружаем новую версию (повторяем шаги 2-4)
# Затем перезапускаем
sudo systemctl start checklatex
```

### Мониторинг и отладка

#### Просмотр логов в реальном времени:
```bash
sudo journalctl -u checklatex -f
```

#### Проверка использования ресурсов:
```bash
sudo systemctl status checklatex
htop  # для мониторинга CPU/RAM
```

#### Проверка конфигурационных файлов:
```bash
ls -la /opt/checklatex/*.json
```

### Настройка через reverse proxy (опционально)

Если вы хотите использовать стандартный порт 80/443, можете настроить nginx:

```bash
# Устанавливаем nginx
sudo apt install nginx

# Создаем конфигурацию
sudo nano /etc/nginx/sites-available/checklatex
```

Содержимое файла nginx:
```nginx
server {
    listen 80;
    server_name your-domain.com;  # замените на ваш домен или IP

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
    }
}
```

```bash
# Активируем конфигурацию
sudo ln -s /etc/nginx/sites-available/checklatex /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

---

## Вариант 2: Развертывание через Docker (альтернативный способ)

Это руководство предназначено для разработчиков и системных администраторов и описывает полный процесс развертывания backend-сервиса CheckLaTeX с нуля на сервере под управлением Unix-подобной операционной системы (например, Ubuntu/Debian).

## Содержание
1.  [Подготовка сервера и установка Docker](#шаг-1-подготовка-сервера-и-установка-docker)
2.  [Получение исходного кода проекта](#шаг-2-получение-исходного-кода-проекта)
3.  [Файлы конфигурации развертывания](#шаг-3-файлы-конфигурации-развертывания)
4.  [Сборка и запуск Docker-контейнера](#шаг-4-сборка-и-запуск-docker-контейнера)
5.  [Проверка и управление контейнером](#шаг-5-проверка-и-управление-контейнером)
6.  [Сборка и установка расширения VS Code](#шаг-6-сборка-и-установка-расширения-vs-code)

---

## Шаг 1: Подготовка сервера и установка Docker

Предполагается, что вы подключились к вашему серверу по SSH.

#### 1.1. Обновите списки пакетов
Это гарантирует, что вы устанавливаете последнюю доступную версию ПО.
```bash
sudo apt-get update -y
```

#### 1.2. Установите Docker
Эта команда установит Docker Community Edition.
```bash
sudo apt-get install -y docker.io
```

#### 1.3. Запустите и активируйте сервис Docker
Это запустит Docker и настроит его автоматический запуск при старте системы.
```bash
sudo systemctl start docker
sudo systemctl enable docker
```

#### 1.4. Проверьте установку
Убедитесь, что Docker установлен и работает корректно.
```bash
docker --version
```
Вы должны увидеть версию Docker, например: `Docker version 20.10.12, build 20.10.12-0ubuntu2~20.04.1`.

> **💡 Совет:** Чтобы избежать необходимости использовать `sudo` для каждой команды `docker`, добавьте своего пользователя в группу `docker`.
> ```bash
> sudo usermod -aG docker ${USER}
> ```
> После этого вам нужно будет перелогиниться, чтобы изменения вступили в силу.

---

## Шаг 2: Получение исходного кода проекта

#### 2.1. Установите Git (если он не установлен)
```bash
sudo apt-get install -y git
```

#### 2.2. Клонируйте репозиторий
```bash
git clone https://github.com/Artelove/CheckLaTeX.git
```

#### 2.3. Перейдите в директорию проекта
Все последующие команды нужно выполнять из корневой директории проекта.
```bash
cd CheckLaTeX
```

> **Важно:** Файлы `Dockerfile` и `entrypoint.sh` уже есть в репозитории. Следующий шаг описывает их содержимое для справки. Вам не нужно создавать их вручную, если вы клонировали репозиторий.

---

## Шаг 3: Файлы конфигурации развертывания

#### 3.1. `Dockerfile`
`Dockerfile` — это текстовый файл, который содержит инструкции для сборки образа. Он настроен так, чтобы копировать локальные файлы проекта и затем использовать `entrypoint.sh` для их обновления при запуске.

```dockerfile
# Используем образ с полным .NET SDK, так как он нужен для сборки в рантайме
FROM mcr.microsoft.com/dotnet/sdk:6.0

# Устанавливаем git
RUN apt-get update && apt-get install -y git

# Создаем и устанавливаем рабочую директорию
WORKDIR /app

# Копируем все файлы из локального контекста сборки в контейнер.
# Это включает и папку .git, что позволяет entrypoint.sh делать git pull.
COPY . .

# Даем права на выполнение нашему скрипту
RUN chmod +x entrypoint.sh

# Открываем порт, который будет слушать приложение
EXPOSE 80

# Устанавливаем наш скрипт как точку входа.
# Он будет выполняться каждый раз при запуске контейнера.
ENTRYPOINT ["./entrypoint.sh"]
```

#### 3.2. `entrypoint.sh`
Это главный скрипт, который выполняется при каждом запуске контейнера. Он загружает последние изменения из репозитория, пересобирает и запускает приложение.

```bash
#!/bin/bash
# Прерываем выполнение скрипта, если любая команда завершится с ошибкой
set -e

echo "--- Pulling latest changes from repository ---"
# Получаем последние изменения из ветки main. git reset отменяет любые случайные локальные изменения.
git fetch origin
git reset --hard origin/main

echo "--- Restoring dotnet dependencies ---"
# Восстанавливаем зависимости для проекта
dotnet restore ./tex-lint/tex-lint.csproj

echo "--- Building and publishing application ---"
# Публикуем приложение в папку /app/publish
dotnet publish ./tex-lint/tex-lint.csproj -c Release -o /app/publish

echo "--- Starting CheckLaTeX backend ---"
# Переходим в папку с опубликованным приложением и запускаем его
cd /app/publish
exec dotnet tex-lint.dll
```

---

## Шаг 4: Сборка и запуск Docker-контейнера

#### 4.1. Соберите Docker-образ
Эта команда прочитает `Dockerfile` и создаст образ с тегом `checklatex-backend`. Так как репозиторий клонируется во время сборки, этот шаг может занять некоторое время.
```bash
sudo docker build -t checklatex-backend .
```

#### 4.2. Запустите контейнер из созданного образа
Эта команда запустит контейнер. При каждом запуске он будет автоматически подтягивать последнюю версию кода.
```bash
sudo docker run -d -p 5000:80 --name checklatex-server checklatex-backend
```

---

## Шаг 5: Проверка и управление контейнером

#### 5.1. Проверьте статус контейнера
Убедитесь, что контейнер запущен и работает.
```bash
sudo docker ps
```
Вы должны увидеть что-то похожее на это:
```
CONTAINER ID   IMAGE                COMMAND                  CREATED          STATUS          PORTS                  NAMES
a1b2c3d4e5f6   checklatex-backend   "dotnet tex-lint.dll"    15 seconds ago   Up 14 seconds   0.0.0.0:5000->80/tcp   checklatex-server
```

#### 5.2. Проверьте работоспособность API
Вы можете использовать `curl` или браузер, чтобы убедиться, что Swagger UI доступен.
```bash
# Из терминала сервера
curl http://localhost:5000/swagger/index.html

# Из вашего браузера
http://<your-server-ip>:5000/swagger/index.html
```
Если все работает, вы получите в ответ HTML-код страницы Swagger.

#### 5.3. Просмотр логов (для отладки)
Если что-то пошло не так, вы можете посмотреть логи контейнера:
```bash
sudo docker logs checklatex-server
```

#### 5.4. Остановка и удаление контейнера
```bash
# Остановка
sudo docker stop checklatex-server

# Удаление
sudo docker rm checklatex-server
```

---

## Шаг 6: Сборка и установка расширения VS Code

Этот шаг выполняется на локальной машине разработчика, а не на сервере.

#### 6.1. Установите зависимости
Вам понадобится `Node.js` и `npm`. Перейдите в директорию расширения и установите зависимости:
```bash
cd tex-lint-extension
npm install
```

#### 6.2. Установите `vsce`
`vsce` — это официальный инструмент для сборки расширений VS Code. Установите его глобально:
```bash
npm install -g @vscode/vsce
```

#### 6.3. Скомпилируйте расширение
Выполните команду для сборки `.vsix` файла:
```bash
vsce package
```
В директории `tex-lint-extension` появится файл с расширением `.vsix` (например, `tex-lint-extension-0.0.1.vsix`).

#### 6.4. Установите расширение в VS Code
1.  Откройте Visual Studio Code.
2.  Перейдите в панель "Расширения" (`Ctrl+Shift+X`).
3.  Нажмите на три точки (`...`) в правом верхнем углу панели и выберите **"Install from VSIX..."**.
4.  Укажите путь к вашему `.vsix` файлу.

После установки расширение будет готово к работе и будет взаимодействовать с запущенным на сервере Docker-контейнером (не забудьте указать в настройках расширения адрес вашего сервера). 