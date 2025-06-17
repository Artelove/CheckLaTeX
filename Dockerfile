# Этап 1: Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Настройки для работы с медленным интернетом
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
ENV NUGET_XMLDOC_MODE=skip
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV NUGET_TIMEOUT=600

# Копируем файл проекта для восстановления зависимостей
COPY tex-lint/*.csproj ./tex-lint/

# Восстанавливаем зависимости
RUN dotnet restore ./tex-lint/tex-lint.csproj --disable-parallel --force

# Копируем весь исходный код
COPY tex-lint/. ./tex-lint/

# Собираем и публикуем приложение
RUN dotnet publish ./tex-lint/tex-lint.csproj -c Release -o /app/publish

# Копируем конфигурационные файлы
COPY lint-rules.json /app/publish/
COPY commands.json /app/publish/
COPY environments.json /app/publish/

# Этап 2: Runtime образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Копируем собранное приложение
COPY --from=build /app/publish .

# Открываем порт 80
EXPOSE 80

# Запускаем приложение
ENTRYPOINT ["dotnet", "tex-lint.dll"] 