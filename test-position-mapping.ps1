# Тест API с новым маппингом позиций
Write-Host "Тестирование API с маппингом позиций..." -ForegroundColor Green

# Создаем ZIP архив с тестовым файлом
$zipPath = "test-position-mapping.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath
}

# Создаем архив
Compress-Archive -Path "test-position-mapping.tex" -DestinationPath $zipPath

# Отправляем запрос к API
$uri = "http://localhost:5000/api/lint/analyze-zip"
$headers = @{
    "Content-Type" = "application/octet-stream"
}

try {
    Write-Host "Отправка ZIP архива на анализ..." -ForegroundColor Yellow
    
    $fileBytes = [System.IO.File]::ReadAllBytes($zipPath)
    $response = Invoke-RestMethod -Uri $uri -Method Post -Body $fileBytes -Headers $headers
    
    Write-Host "Ответ получен!" -ForegroundColor Green
    Write-Host "JSON Response:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 10 | Write-Host
    
    # Анализируем результаты
    if ($response.results -and $response.results.Count -gt 0) {
        Write-Host "`nАнализ результатов:" -ForegroundColor Magenta
        
        foreach ($result in $response.results) {
            if ($result.errors -and $result.errors.Count -gt 0) {
                Write-Host "Найдено ошибок: $($result.errors.Count)" -ForegroundColor Red
                
                foreach ($error in $result.errors) {
                    Write-Host "  - Файл: $($error.fileName)" -ForegroundColor White
                    Write-Host "    Строка: $($error.lineNumber), Столбец: $($error.columnNumber)" -ForegroundColor White
                    Write-Host "    Сообщение: $($error.message)" -ForegroundColor White
                    Write-Host "    Оригинальный текст: '$($error.originalText)'" -ForegroundColor Gray
                    if ($error.suggestedFix) {
                        Write-Host "    Предлагаемое исправление: '$($error.suggestedFix)'" -ForegroundColor Green
                    }
                    Write-Host ""
                }
            } else {
                Write-Host "Ошибок не найдено в $($result.testName)" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "Результаты анализа пусты" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "Ошибка при отправке запроса: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Детали: $($_.Exception)" -ForegroundColor Red
}

# Очистка
if (Test-Path $zipPath) {
    Remove-Item $zipPath
    Write-Host "Временный архив удален" -ForegroundColor Gray
} 