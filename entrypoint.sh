#!/bin/bash
# Включаем режим отладки: выводим каждую команду перед ее выполнением
set -x 
# Прерываем выполнение скрипта, если любая команда завершится с ошибкой
set -e

echo "--- [1/4] Working with code already copied to container ---"
# Код уже скопирован командой COPY . . в Dockerfile, поэтому Git-операции не нужны

echo "--- [2/4] Restoring dotnet dependencies ---"
dotnet restore ./tex-lint/tex-lint.csproj

echo "--- [3/4] Building and publishing application ---"
dotnet publish ./tex-lint/tex-lint.csproj -c Release -o /app/publish

echo "--- [4/4] Copying configuration files to publish directory ---"
cp lint-rules.json /app/publish/
cp commands.json /app/publish/
cp environments.json /app/publish/

echo "--- Verifying contents of publish directory ---"
# Эта команда покажет нам все файлы в директории запуска, чтобы убедиться, что все на месте.
ls -lR /app/publish

echo "--- Starting CheckLaTeX backend ---"
# Переходим в папку с опубликованным приложением и запускаем его
cd /app/publish
exec dotnet tex-lint.dll 