# CheckLaTeX VS Code Extension Installation Script for Windows
# Requires: PowerShell 5.1+, Node.js 16+, VS Code

param(
    [switch]$SkipDependencyCheck,
    [switch]$Verbose
)

# Настройка цветного вывода
function Write-StatusInfo($Message) {
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-StatusSuccess($Message) {
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-StatusWarning($Message) {
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-StatusError($Message) {
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

Write-StatusInfo "=== Установка CheckLaTeX VS Code Extension ==="

# Проверка зависимостей
function Test-Dependencies {
    Write-StatusInfo "Проверка зависимостей..."
    
    # Проверяем Node.js
    try {
        $nodeVersion = node --version 2>$null
        if (-not $nodeVersion) {
            throw "Node.js не найден"
        }
        
        $versionNumber = [int]($nodeVersion -replace 'v(\d+)\..*', '$1')
        if ($versionNumber -lt 16) {
            throw "Требуется Node.js версии 16 или выше. Текущая версия: $nodeVersion"
        }
        
        Write-StatusInfo "Node.js: $nodeVersion ✓"
    }
    catch {
        Write-StatusError "Node.js не установлен или версия < 16. Установите Node.js 16.x или выше."
        Write-StatusInfo "Скачайте с: https://nodejs.org/"
        exit 1
    }
    
    # Проверяем npm
    try {
        $npmVersion = npm --version 2>$null
        if (-not $npmVersion) {
            throw "npm не найден"
        }
        Write-StatusInfo "npm: $npmVersion ✓"
    }
    catch {
        Write-StatusError "npm не установлен"
        exit 1
    }
    
    # Проверяем VS Code
    try {
        $codeVersion = code --version 2>$null
        if (-not $codeVersion) {
            Write-StatusWarning "VS Code CLI не найден. Убедитесь, что VS Code установлен и добавлен в PATH"
        } else {
            Write-StatusInfo "VS Code: $($codeVersion[0]) ✓"
        }
    }
    catch {
        Write-StatusWarning "VS Code CLI недоступен"
    }
    
    # Проверяем Git
    try {
        $gitVersion = git --version 2>$null
        if (-not $gitVersion) {
            throw "Git не найден"
        }
        Write-StatusInfo "Git: $gitVersion ✓"
    }
    catch {
        Write-StatusError "Git не установлен. Установите Git с: https://git-scm.com/"
        exit 1
    }
    
    Write-StatusSuccess "Все зависимости присутствуют"
}

# Установка vsce
function Install-Vsce {
    try {
        $vsceVersion = vsce --version 2>$null
        if ($vsceVersion) {
            Write-StatusInfo "vsce уже установлен: $vsceVersion"
            return
        }
    }
    catch {}
    
    Write-StatusInfo "Установка vsce (VS Code Extension Manager)..."
    try {
        npm install -g vsce
        Write-StatusSuccess "vsce установлен"
    }
    catch {
        Write-StatusError "Не удалось установить vsce: $($_.Exception.Message)"
        exit 1
    }
}

# Удаление предыдущих версий
function Remove-PreviousExtension {
    Write-StatusInfo "Удаление предыдущих версий расширения..."
    
    try {
        $extensions = code --list-extensions 2>$null | Where-Object { $_ -match "checklatex" }
        if ($extensions) {
            foreach ($extension in $extensions) {
                Write-StatusInfo "Удаляем расширение: $extension"
                code --uninstall-extension $extension 2>$null
            }
        }
    }
    catch {
        Write-StatusWarning "Не удалось получить список расширений VS Code"
    }
    
    # Проверяем VS Code Insiders
    try {
        $extensionsInsiders = code-insiders --list-extensions 2>$null | Where-Object { $_ -match "checklatex" }
        if ($extensionsInsiders) {
            foreach ($extension in $extensionsInsiders) {
                Write-StatusInfo "Удаляем расширение из VS Code Insiders: $extension"
                code-insiders --uninstall-extension $extension 2>$null
            }
        }
    }
    catch {
        Write-StatusWarning "VS Code Insiders недоступен или ошибка при удалении расширений"
    }
}

# Основная установка
function Install-Extension {
    $tempDir = "$env:TEMP\checklatex-extension-install"
    $repoUrl = "https://github.com/Artelove/CheckLaTeX.git"
    
    # Очищаем временную директорию
    if (Test-Path $tempDir) {
        Remove-Item $tempDir -Recurse -Force
    }
    
    # Клонируем репозиторий
    Write-StatusInfo "Загрузка исходного кода..."
    try {
        git clone $repoUrl $tempDir
        if (-not $?) {
            throw "Ошибка клонирования репозитория"
        }
    }
    catch {
        Write-StatusError "Не удалось клонировать репозиторий: $($_.Exception.Message)"
        exit 1
    }
    
    # Переходим в директорию расширения
    $extensionDir = Join-Path $tempDir "tex-lint-extension"
    if (-not (Test-Path $extensionDir)) {
        Write-StatusError "Директория расширения не найдена: $extensionDir"
        exit 1
    }
    
    Set-Location $extensionDir
    
    # Устанавливаем зависимости
    Write-StatusInfo "Установка зависимостей..."
    try {
        npm install
        if (-not $?) {
            throw "Ошибка установки зависимостей"
        }
    }
    catch {
        Write-StatusError "Не удалось установить зависимости: $($_.Exception.Message)"
        exit 1
    }
    
    # Компилируем TypeScript
    Write-StatusInfo "Компиляция TypeScript..."
    try {
        npm run compile
        if (-not $?) {
            throw "Ошибка компиляции"
        }
    }
    catch {
        Write-StatusError "Не удалось скомпилировать проект: $($_.Exception.Message)"
        exit 1
    }
    
    # Проверяем линтером
    Write-StatusInfo "Проверка кода..."
    try {
        npm run lint
    }
    catch {
        Write-StatusWarning "Обнаружены предупреждения линтера (не критично)"
    }
    
    # Упаковываем расширение
    Write-StatusInfo "Упаковка расширения..."
    $packageName = "checklatex-extension-$(Get-Date -Format 'yyyyMMdd_HHmmss').vsix"
    try {
        vsce package --out $packageName
        if (-not $?) {
            throw "Ошибка упаковки"
        }
    }
    catch {
        Write-StatusError "Не удалось упаковать расширение: $($_.Exception.Message)"
        exit 1
    }
    
    # Устанавливаем расширение
    Write-StatusInfo "Установка расширения в VS Code..."
    try {
        code --install-extension $packageName --force
        if ($?) {
            Write-StatusSuccess "Расширение установлено в VS Code"
        } else {
            Write-StatusWarning "Возможная ошибка при установке в VS Code"
        }
    }
    catch {
        Write-StatusWarning "VS Code CLI недоступен. Установите расширение вручную: $extensionDir\$packageName"
    }
    
    # Устанавливаем в Insiders
    try {
        code-insiders --install-extension $packageName --force 2>$null
        if ($?) {
            Write-StatusSuccess "Расширение установлено в VS Code Insiders"
        }
    }
    catch {
        Write-StatusInfo "VS Code Insiders недоступен или ошибка установки"
    }
    
    # Копируем пакет в домашнюю директорию
    try {
        Copy-Item $packageName $env:USERPROFILE -Force
        Write-StatusInfo "Пакет сохранен в: $env:USERPROFILE\$packageName"
    }
    catch {
        Write-StatusWarning "Не удалось скопировать пакет в домашнюю директорию"
    }
    
    return $packageName
}

# Проверка установки
function Test-Installation {
    Write-StatusInfo "Проверка установки..."
    
    try {
        $extensions = code --list-extensions 2>$null | Where-Object { $_ -match "checklatex" }
        if ($extensions) {
            Write-StatusSuccess "Расширение успешно установлено в VS Code"
            return $true
        } else {
            Write-StatusError "Не удалось найти установленное расширение в VS Code"
            return $false
        }
    }
    catch {
        Write-StatusWarning "Не удалось проверить установку"
        return $false
    }
}

# Показ инструкций
function Show-Instructions {
    Write-StatusInfo "=== Расширение готово к использованию ==="
    Write-Host ""
    Write-Host "📋 Основные команды:" -ForegroundColor Cyan
    Write-Host "  • Ctrl+Alt+Shift+S - Полный анализ проекта"
    Write-Host "  • Ctrl+Alt+Shift+T - Быстрая настройка интервала"
    Write-Host "  • Ctrl+Shift+P -> 'CheckLaTeX' - Все команды"
    Write-Host ""
    Write-Host "⚙️  Настройки (File > Preferences > Settings > CheckLaTeX):" -ForegroundColor Cyan
    Write-Host "  • Server URL: http://localhost:5000 (по умолчанию)"
    Write-Host "  • Auto Analyze: включить автоматический анализ"
    Write-Host "  • Periodic Check: периодические проверки"
    Write-Host ""
    Write-Host "📁 Для работы расширения нужен запущенный CheckLaTeX сервер" -ForegroundColor Yellow
    Write-Host "   Запустите сервер согласно DEPLOYMENT_GUIDE.md"
}

# Основная функция
function Main {
    $originalLocation = Get-Location
    
    try {
        if (-not $SkipDependencyCheck) {
            Test-Dependencies
        }
        
        Install-Vsce
        Remove-PreviousExtension
        $package = Install-Extension
        
        if (Test-Installation) {
            Write-StatusSuccess "=== Установка завершена успешно! ==="
            Show-Instructions
        } else {
            Write-StatusError "Установка завершена с ошибками"
        }
    }
    catch {
        Write-StatusError "Произошла критическая ошибка: $($_.Exception.Message)"
        exit 1
    }
    finally {
        # Возвращаемся в исходную директорию
        Set-Location $originalLocation
        
        # Очищаем временные файлы
        $tempDir = "$env:TEMP\checklatex-extension-install"
        if (Test-Path $tempDir) {
            try {
                Remove-Item $tempDir -Recurse -Force
                Write-StatusInfo "Временные файлы очищены"
            }
            catch {
                Write-StatusWarning "Не удалось очистить временные файлы: $tempDir"
            }
        }
    }
}

# Запуск
Main