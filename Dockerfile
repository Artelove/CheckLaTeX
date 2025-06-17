# Этап 1: Сборка приложения (выполняется при docker build)
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

# Настраиваем переменные окружения для работы в условиях ограниченного доступа
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
ENV NUGET_XMLDOC_MODE=skip
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Увеличиваем таймауты для медленного соединения
ENV NUGET_TIMEOUT=600

# Настраиваем альтернативные источники NuGet для работы из России
RUN mkdir -p /root/.nuget/NuGet

# Создаем кастомный NuGet.config с несколькими зеркалами
RUN echo '<?xml version="1.0" encoding="utf-8"?>\n\
<configuration>\n\
  <config>\n\
    <add key="http_timeout" value="600" />\n\
  </config>\n\
  <packageSources>\n\
    <clear />\n\
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />\n\
    <add key="MyGet" value="https://www.myget.org/F/dotnet-core/api/v3/index.json" />\n\
    <add key="Azure Artifacts" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-libraries/nuget/v3/index.json" />\n\
  </packageSources>\n\
  <packageSourceCredentials />\n\
</configuration>' > /root/.nuget/NuGet/NuGet.Config

# Копируем файл проекта и восстанавливаем зависимости
# Это кэширует слой, чтобы при изменении только кода не перекачивать пакеты заново
COPY tex-lint/*.csproj ./tex-lint/

# Используем более подробный вывод для отладки проблем с сетью
# Отключаем параллельное скачивание для большей стабильности
RUN dotnet restore ./tex-lint/tex-lint.csproj --verbosity detailed --disable-parallel --force

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