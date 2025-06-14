# CheckLaTeX Extension Setup Script
# Скрипт для быстрой установки и настройки расширения

Write-Host "=== CheckLaTeX Extension Setup ===" -ForegroundColor Green

# Проверяем наличие Node.js
Write-Host "Проверяем Node.js..." -ForegroundColor Yellow
if (Get-Command node -ErrorAction SilentlyContinue) {
    $nodeVersion = node --version
    Write-Host "Node.js найден: $nodeVersion" -ForegroundColor Green
} else {
    Write-Host "ОШИБКА: Node.js не найден!" -ForegroundColor Red
    Write-Host "Установите Node.js с https://nodejs.org/" -ForegroundColor Red
    exit 1
}

# Проверяем наличие npm
Write-Host "Проверяем npm..." -ForegroundColor Yellow
if (Get-Command npm -ErrorAction SilentlyContinue) {
    $npmVersion = npm --version
    Write-Host "npm найден: $npmVersion" -ForegroundColor Green
} else {
    Write-Host "ОШИБКА: npm не найден!" -ForegroundColor Red
    exit 1
}

# Переходим в директорию расширения
$extensionPath = Split-Path -Parent $PSScriptRoot
Set-Location $extensionPath

Write-Host "Рабочая директория: $extensionPath" -ForegroundColor Cyan

# Устанавливаем зависимости
Write-Host "Устанавливаем зависимости..." -ForegroundColor Yellow
npm install

if ($LASTEXITCODE -ne 0) {
    Write-Host "ОШИБКА: Не удалось установить зависимости!" -ForegroundColor Red
    exit 1
}

Write-Host "Зависимости установлены успешно!" -ForegroundColor Green

# Компилируем TypeScript
Write-Host "Компилируем TypeScript..." -ForegroundColor Yellow
npm run compile

if ($LASTEXITCODE -ne 0) {
    Write-Host "ОШИБКА: Не удалось скомпилировать TypeScript!" -ForegroundColor Red
    exit 1
}

Write-Host "Компиляция завершена успешно!" -ForegroundColor Green

# Создаем пакет расширения
Write-Host "Создаем пакет расширения..." -ForegroundColor Yellow

# Проверяем наличие vsce
if (Get-Command vsce -ErrorAction SilentlyContinue) {
    Write-Host "vsce найден" -ForegroundColor Green
} else {
    Write-Host "Устанавливаем vsce..." -ForegroundColor Yellow
    npm install -g vsce
}

# Создаем .vsix файл
vsce package

if ($LASTEXITCODE -ne 0) {
    Write-Host "ПРЕДУПРЕЖДЕНИЕ: Не удалось создать пакет. Возможно, нужно настроить publisher." -ForegroundColor Yellow
    Write-Host "Расширение все равно готово к разработке." -ForegroundColor Yellow
} else {
    Write-Host "Пакет расширения создан успешно!" -ForegroundColor Green
    
    # Показываем созданные файлы
    $vsixFiles = Get-ChildItem -Path . -Filter "*.vsix"
    if ($vsixFiles.Count -gt 0) {
        Write-Host "Созданные пакеты:" -ForegroundColor Cyan
        foreach ($file in $vsixFiles) {
            Write-Host "  - $($file.Name)" -ForegroundColor Cyan
        }
    }
}

Write-Host ""
Write-Host "=== Настройка завершена ===" -ForegroundColor Green
Write-Host ""
Write-Host "Для запуска в режиме разработки:" -ForegroundColor Cyan
Write-Host "1. Откройте папку tex-lint-extension в VS Code" -ForegroundColor White
Write-Host "2. Нажмите F5 для запуска Extension Development Host" -ForegroundColor White
Write-Host "3. В новом окне VS Code откройте LaTeX проект" -ForegroundColor White
Write-Host "4. Используйте Ctrl+Shift+P → 'CheckLaTeX' для тестирования" -ForegroundColor White
Write-Host ""
Write-Host "Для установки расширения:" -ForegroundColor Cyan
Write-Host "1. Откройте VS Code" -ForegroundColor White
Write-Host "2. Ctrl+Shift+P → 'Extensions: Install from VSIX...'" -ForegroundColor White
Write-Host "3. Выберите созданный .vsix файл" -ForegroundColor White

Write-Host ""
Write-Host "Настройка сервера:" -ForegroundColor Cyan
Write-Host "- Откройте настройки VS Code (Ctrl+,)" -ForegroundColor White
Write-Host "- Найдите 'CheckLaTeX' в поиске" -ForegroundColor White
Write-Host "- Установите 'checklatex.serverUrl' (по умолчанию: http://localhost:5000)" -ForegroundColor White 