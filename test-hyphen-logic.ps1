# Тест логики дефисов
Write-Host "Тестирование логики дефисов..." -ForegroundColor Green

# Создаем ZIP архив с тестовым файлом
$zipPath = "test-hyphen-logic.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath
}

# Создаем архив
Compress-Archive -Path "test-hyphen-logic.tex" -DestinationPath $zipPath

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
    
    # Анализируем результаты для дефисов
    if ($response.results -and $response.results.Count -gt 0) {
        Write-Host "`nАнализ результатов для дефисов:" -ForegroundColor Magenta
        
        foreach ($result in $response.results) {
            if ($result.testName -like "*Hyphen*" -and $result.errors -and $result.errors.Count -gt 0) {
                Write-Host "Тест дефисов - найдено ошибок: $($result.errors.Count)" -ForegroundColor Red
                
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
            } elseif ($result.testName -like "*Hyphen*") {
                Write-Host "Тест дефисов - ошибок не найдено" -ForegroundColor Green
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