# CheckLaTeX VS Code Extension Uninstall Script for Windows
# Requires: PowerShell 5.1+, VS Code

param(
    [switch]$Verbose,
    [switch]$Force
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

Write-StatusInfo "=== Удаление CheckLaTeX VS Code Extension ==="

# Удаление из VS Code
function Remove-FromVSCode {
    Write-StatusInfo "Поиск установленных расширений CheckLaTeX в VS Code..."
    
    try {
        $extensions = code --list-extensions 2>$null | Where-Object { $_ -match "checklatex" }
        
        if ($extensions) {
            foreach ($extension in $extensions) {
                if ($extension.Trim()) {
                    Write-StatusInfo "Удаляем расширение: $extension"
                    try {
                        code --uninstall-extension $extension
                        if ($?) {
                            Write-StatusSuccess "Расширение $extension удалено"
                        } else {
                            Write-StatusWarning "Возможная ошибка при удалении $extension"
                        }
                    }
                    catch {
                        Write-StatusError "Ошибка при удалении $extension : $($_.Exception.Message)"
                    }
                }
            }
            Write-StatusSuccess "Расширения удалены из VS Code"
        } else {
            Write-StatusWarning "Расширения CheckLaTeX не найдены в VS Code"
        }
    }
    catch {
        Write-StatusError "VS Code CLI не найден или недоступен"
    }
}

# Удаление из VS Code Insiders
function Remove-FromVSCodeInsiders {
    Write-StatusInfo "Поиск установленных расширений CheckLaTeX в VS Code Insiders..."
    
    try {
        $extensionsInsiders = code-insiders --list-extensions 2>$null | Where-Object { $_ -match "checklatex" }
        
        if ($extensionsInsiders) {
            foreach ($extension in $extensionsInsiders) {
                if ($extension.Trim()) {
                    Write-StatusInfo "Удаляем расширение из Insiders: $extension"
                    try {
                        code-insiders --uninstall-extension $extension
                        if ($?) {
                            Write-StatusSuccess "Расширение $extension удалено из Insiders"
                        } else {
                            Write-StatusWarning "Возможная ошибка при удалении $extension из Insiders"
                        }
                    }
                    catch {
                        Write-StatusError "Ошибка при удалении $extension из Insiders: $($_.Exception.Message)"
                    }
                }
            }
            Write-StatusSuccess "Расширения удалены из VS Code Insiders"
        } else {
            Write-StatusWarning "Расширения CheckLaTeX не найдены в VS Code Insiders"
        }
    }
    catch {
        Write-StatusWarning "VS Code Insiders недоступен"
    }
}

# Удаление .vsix файлов
function Remove-VsixFiles {
    Write-StatusInfo "Удаление .vsix файлов..."
    
    $searchPaths = @(
        $env:USERPROFILE,
        "$env:USERPROFILE\Downloads",
        "$env:USERPROFILE\Desktop"
    )
    
    $removedCount = 0
    
    foreach ($path in $searchPaths) {
        if (Test-Path $path) {
            try {
                $vsixFiles = Get-ChildItem -Path $path -Filter "*checklatex*extension*.vsix" -File -ErrorAction SilentlyContinue
                
                foreach ($file in $vsixFiles) {
                    try {
                        if ($Verbose) {
                            Write-StatusInfo "Удаляем файл: $($file.FullName)"
                        }
                        Remove-Item $file.FullName -Force
                        $removedCount++
                    }
                    catch {
                        Write-StatusWarning "Не удалось удалить файл $($file.FullName): $($_.Exception.Message)"
                    }
                }
            }
            catch {
                Write-StatusWarning "Ошибка при поиске в $path : $($_.Exception.Message)"
            }
        }
    }
    
    if ($removedCount -gt 0) {
        Write-StatusSuccess "Удалено $removedCount .vsix файлов"
    } else {
        Write-StatusInfo "Файлы .vsix не найдены"
    }
}

# Удаление временных файлов
function Remove-TempFiles {
    Write-StatusInfo "Удаление временных файлов..."
    
    $tempPaths = @(
        "$env:TEMP\checklatex-extension-*",
        "$env:TEMP\checklatex-*",
        "$env:LOCALAPPDATA\Temp\checklatex-extension-*"
    )
    
    $removedCount = 0
    
    foreach ($pattern in $tempPaths) {
        try {
            $items = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue
            
            foreach ($item in $items) {
                try {
                    if ($Verbose) {
                        Write-StatusInfo "Удаляем временную папку: $($item.FullName)"
                    }
                    Remove-Item $item.FullName -Recurse -Force
                    $removedCount++
                }
                catch {
                    Write-StatusWarning "Не удалось удалить $($item.FullName): $($_.Exception.Message)"
                }
            }
        }
        catch {
            # Игнорируем ошибки поиска
        }
    }
    
    if ($removedCount -gt 0) {
        Write-StatusSuccess "Удалено $removedCount временных папок"
    } else {
        Write-StatusInfo "Временные файлы не найдены"
    }
}

# Очистка npm кэша (опционально)
function Clear-NpmCache {
    if ($Force) {
        Write-StatusInfo "Очистка npm кэша..."
        try {
            npm cache clean --force 2>$null
            Write-StatusSuccess "npm кэш очищен"
        }
        catch {
            Write-StatusWarning "Не удалось очистить npm кэш: $($_.Exception.Message)"
        }
    }
}

# Показ инструкций по переустановке
function Show-ReinstallInstructions {
    Write-StatusInfo "=== Инструкции по переустановке ==="
    Write-Host ""
    Write-Host "🔄 Для переустановки расширения:" -ForegroundColor Cyan
    Write-Host "  • Bash: curl -fsSL https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install-extension.sh | bash"
    Write-Host "  • PowerShell: & ([scriptblock]::Create((Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/Artelove/CheckLaTeX/main/install-extension.ps1').Content))"
    Write-Host ""
    Write-Host "📁 Или локально:" -ForegroundColor Cyan
    Write-Host "  • ./install-extension.sh (Linux/macOS)"
    Write-Host "  • .\install-extension.ps1 (Windows)"
    Write-Host ""
}

# Подтверждение действия
function Confirm-Removal {
    if (-not $Force) {
        Write-Host ""
        $confirmation = Read-Host "Вы уверены, что хотите удалить CheckLaTeX расширение? (y/N)"
        if ($confirmation -notmatch '^[Yy]$') {
            Write-StatusInfo "Отмена удаления"
            exit 0
        }
    }
}

# Основная функция
function Main {
    try {
        Confirm-Removal
        
        Remove-FromVSCode
        Remove-FromVSCodeInsiders
        Remove-VsixFiles
        Remove-TempFiles
        Clear-NpmCache
        
        Write-StatusSuccess "CheckLaTeX расширение полностью удалено"
        
        if (-not $Force) {
            Show-ReinstallInstructions
        }
    }
    catch {
        Write-StatusError "Произошла критическая ошибка: $($_.Exception.Message)"
        exit 1
    }
}

# Запуск
Main