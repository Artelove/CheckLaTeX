#!/bin/bash
# Включаем режим отладки: выводим каждую команду перед ее выполнением
set -x 
# Прерываем выполнение скрипта, если любая команда завершится с ошибкой (кроме тех, что в if)
set -e

echo "--- [1/6] Attempting to pull latest changes from repository ---"

# Пытаемся обновиться, но не падаем, если не получится (например, нет сети).
if git fetch origin; then
    echo "Fetch successful. Resetting to origin/main."
    git reset --hard origin/main
else
    # Если команда git fetch завершилась с ошибкой, выводим предупреждение и продолжаем.
    echo "WARNING: Could not fetch from origin. Continuing with the local version of the code."
fi

echo "--- [2/6] Restoring dotnet dependencies ---"
# Восстанавливаем зависимости для проекта
dotnet restore ./tex-lint/tex-lint.csproj

echo "--- [3/6] Building and publishing application ---"
# Публикуем приложение в папку /app/publish
dotnet publish ./tex-lint/tex-lint.csproj -c Release -o /app/publish

echo "--- [4/6] Copying configuration files to publish directory ---"
# Эти файлы критически важны для работы приложения в рантайме
cp lint-rules.json /app/publish/
cp commands.json /app/publish/
cp environments.json /app/publish/

echo "--- [5/6] Verifying contents of publish directory ---"
# Эта команда покажет нам все файлы в директории запуска, чтобы убедиться, что все на месте.
ls -lR /app/publish

echo "--- [6/6] Starting CheckLaTeX backend ---"
# Переходим в папку с опубликованным приложением и запускаем его
cd /app/publish
exec dotnet tex-lint.dll 