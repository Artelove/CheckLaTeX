# PowerShell script for testing CheckLaTeX API with ZIP files

param(
    [Parameter(Mandatory=$true)]
    [string]$ZipPath,
    [string]$StartFile = "",
    [string]$ApiUrl = "http://localhost:5000"
)

Write-Host "Testing CheckLaTeX API with ZIP file upload..."
Write-Host "ZIP file: $ZipPath"
Write-Host "API URL: $ApiUrl"

# Проверяем существование файла
if (-not (Test-Path $ZipPath)) {
    Write-Host "Ошибка: ZIP файл не найден: $ZipPath" -ForegroundColor Red
    exit 1
}

# Проверяем расширение файла
if (-not $ZipPath.EndsWith(".zip")) {
    Write-Host "Ошибка: Поддерживаются только ZIP файлы" -ForegroundColor Red
    exit 1
}

try {
    # Подготавливаем multipart/form-data
    $boundary = [System.Guid]::NewGuid().ToString()
    $headers = @{
        "Content-Type" = "multipart/form-data; boundary=$boundary"
    }

    # Читаем ZIP файл
    $zipBytes = [System.IO.File]::ReadAllBytes($ZipPath)
    $zipFileName = [System.IO.Path]::GetFileName($ZipPath)

    # Формируем тело запроса
    $bodyLines = @()
    
    # ZIP файл
    $bodyLines += "--$boundary"
    $bodyLines += "Content-Disposition: form-data; name=`"zipFile`"; filename=`"$zipFileName`""
    $bodyLines += "Content-Type: application/zip"
    $bodyLines += ""
    
    # Преобразуем байты в строку (для простоты, в реальности лучше использовать Invoke-RestMethod)
    $encoding = [System.Text.Encoding]::GetEncoding("iso-8859-1")
    $bodyLines += $encoding.GetString($zipBytes)
    
    # StartFile параметр (если указан)
    if ($StartFile -ne "") {
        $bodyLines += "--$boundary"
        $bodyLines += "Content-Disposition: form-data; name=`"startFile`""
        $bodyLines += ""
        $bodyLines += $StartFile
    }
    
    $bodyLines += "--$boundary--"
    
    $body = $bodyLines -join "`r`n"

    Write-Host "Отправка запроса..." -ForegroundColor Yellow
    
    # Отправляем запрос (альтернативный способ с curl, если доступен)
    if (Get-Command curl -ErrorAction SilentlyContinue) {
        $curlArgs = @(
            "-X", "POST"
            "-H", "Content-Type: multipart/form-data"
            "-F", "zipFile=@$ZipPath"
            "$ApiUrl/api/lint/analyze-zip"
        )
        
        if ($StartFile -ne "") {
            $curlArgs += "-F", "startFile=$StartFile"
        }
        
        Write-Host "Используем curl для отправки запроса..."
        $response = & curl @curlArgs
        
        Write-Host "=== РЕЗУЛЬТАТ АНАЛИЗА ===" -ForegroundColor Green
        Write-Host $response
        
        # Сохраняем результат в файл
        $outputFile = "zip-analysis-result.txt"
        $response | Out-File -FilePath $outputFile -Encoding UTF8
        Write-Host ""
        Write-Host "Результат сохранен в файл: $outputFile" -ForegroundColor Cyan
        
    } else {
        Write-Host "curl не найден. Используйте альтернативный метод:" -ForegroundColor Yellow
        Write-Host "1. Установите curl или"
        Write-Host "2. Используйте Swagger UI по адресу: $ApiUrl/swagger"
        Write-Host "3. Или используйте Postman для отправки multipart/form-data запроса"
    }
    
} catch {
    Write-Host "=== ОШИБКА ===" -ForegroundColor Red
    Write-Host "Ошибка: $($_.Exception.Message)"
    
    # Проверяем доступность сервера
    try {
        $testConnection = Invoke-WebRequest -Uri $ApiUrl -Method Get -TimeoutSec 5
        Write-Host "Сервер отвечает, возможна проблема с endpoint" -ForegroundColor Yellow
    } catch {
        Write-Host "Сервер не отвечает на $ApiUrl" -ForegroundColor Red
        Write-Host "Убедитесь, что приложение запущено командой: dotnet run" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== ИНСТРУКЦИИ ПО ИСПОЛЬЗОВАНИЮ ===" -ForegroundColor Cyan
Write-Host "1. Запустите API: cd tex-lint && dotnet run"
Write-Host "2. Откройте Swagger UI: $ApiUrl/swagger"
Write-Host "3. Используйте endpoint: POST /api/lint/analyze-zip"
Write-Host "4. Загрузите ZIP файл и (опционально) укажите startFile"
Write-Host ""
Write-Host "Примеры запуска скрипта:"
Write-Host "  .\test-zip.ps1 -ZipPath 'path\to\document.zip'"
Write-Host "  .\test-zip.ps1 -ZipPath 'path\to\document.zip' -StartFile 'main.tex'"
Write-Host "  .\test-zip.ps1 -ZipPath 'path\to\document.zip' -ApiUrl 'http://localhost:8080'" 