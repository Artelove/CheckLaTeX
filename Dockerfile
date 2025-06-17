# Этап 1: Сборка приложения (выполняется при docker build)
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

# Копируем файл проекта и восстанавливаем зависимости
# Это кэширует слой, чтобы при изменении только кода не перекачивать пакеты заново
COPY tex-lint/*.csproj ./tex-lint/
RUN dotnet restore ./tex-lint/tex-lint.csproj

# Копируем остальные файлы проекта
COPY tex-lint/. ./tex-lint/

# Публикуем приложение в папку /app/publish
RUN dotnet publish ./tex-lint/tex-lint.csproj -c Release -o /app/publish

# Копируем конфигурационные файлы в ту же папку
COPY lint-rules.json /app/publish/
COPY commands.json /app/publish/
COPY environments.json /app/publish/

# Этап 2: Финальный runtime-образ (меньше по размеру)
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

# Копируем уже собранное приложение из первого этапа
COPY --from=build /app/publish .

# Открываем порт
EXPOSE 80

# Запускаем приложение напрямую (без entrypoint.sh)
ENTRYPOINT ["dotnet", "tex-lint.dll"] 