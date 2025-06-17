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

echo "--- Copying configuration files to publish directory ---"
# Эти файлы критически важны для работы приложения в рантайме
cp lint-rules.json /app/publish/
cp commands.json /app/publish/
cp environments.json /app/publish/

echo "--- Starting CheckLaTeX backend ---"
# Переходим в папку с опубликованным приложением и запускаем его
cd /app/publish
exec dotnet tex-lint.dll 