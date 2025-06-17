# Используем образ с полным .NET SDK, так как он нужен для сборки в рантайме
FROM mcr.microsoft.com/dotnet/sdk:6.0

# Устанавливаем git
RUN apt-get update && apt-get install -y git

# Создаем и устанавливаем рабочую директорию
WORKDIR /app

# Клонируем репозиторий при первоначальной сборке образа
# Используем URL, который вы указали
RUN git clone https://github.com/Artelove/CheckLaTeX.git .

# Копируем скрипт запуска в контейнер
COPY entrypoint.sh .
# Даем права на выполнение
RUN chmod +x entrypoint.sh

# Копируем конфигурационные файлы, чтобы они были в контексте сборки
COPY lint-rules.json .
COPY commands.json .
COPY environments.json .

# Открываем порт, который будет слушать приложение
EXPOSE 80

# Устанавливаем наш скрипт как точку входа.
# Он будет выполняться каждый раз при запуске контейнера.
ENTRYPOINT ["./entrypoint.sh"] 