#!/bin/bash
# Прерываем выполнение скрипта, если любая команда завершится с ошибкой (кроме тех, что в if)
set -e

echo "--- Attempting to pull latest changes from repository ---"

# Пытаемся обновиться, но не падаем, если не получится (например, нет сети).
if git fetch origin; then
    echo "Fetch successful. Resetting to origin/main."
    git reset --hard origin/main
else
    # Если команда git fetch завершилась с ошибкой, выводим предупреждение и продолжаем.
    echo "WARNING: Could not fetch from origin. Continuing with the local version of the code."
fi

echo "--- Restoring dotnet dependencies ---"
# Восстанавливаем зависимости для проекта
dotnet restore ./tex-lint/tex-lint.csproj

echo "--- Building and publishing application ---"
# Публикуем приложение в папку /app/publish
dotnet publish ./tex-lint/tex-lint.csproj -c Release -o /app/publish

echo "--- Copying configuration files to publish directory ---"
# Эти файлы критически важны для работы приложения в рантайме
cp lint-rules.json /app/publish/
cp commands.json /app/publish/
cp environments.json /app/publish/

echo "--- Starting CheckLaTeX backend ---"
# Переходим в папку с опубликованным приложением и запускаем его
cd /app/publish
exec dotnet tex-lint.dll 