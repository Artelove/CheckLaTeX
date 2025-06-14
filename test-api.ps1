# PowerShell скрипт для тестирования CheckLaTeX API

# Адрес API
$apiUrl = "http://localhost:5000/api/lint"

# Чтение тестового документа
$testDocContent = Get-Content -Path "test-document.tex" -Raw -Encoding UTF8

# Подготовка JSON запроса
$requestBody = @{
    StartFile = "test-document.tex"
    Files = @{
        "test-document.tex" = $testDocContent
    }
} | ConvertTo-Json -Depth 3

# Настройка заголовков
$headers = @{
    "Content-Type" = "application/json; charset=utf-8"
}

Write-Host "=== ТЕСТИРОВАНИЕ CHECKLATEX API ===" -ForegroundColor Green
Write-Host "Отправляем запрос к: $apiUrl" -ForegroundColor Yellow
Write-Host "Размер документа: $($testDocContent.Length) символов" -ForegroundColor Yellow
Write-Host ""

try {
    # Отправка запроса
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $requestBody -Headers $headers -ContentType "application/json; charset=utf-8"
    
    Write-Host "=== РЕЗУЛЬТАТ АНАЛИЗА ===" -ForegroundColor Green
    Write-Host $response -ForegroundColor White
    
    # Сохранение результата в файл
    $response | Out-File -FilePath "test-results.txt" -Encoding UTF8
    Write-Host ""
    Write-Host "Результаты сохранены в test-results.txt" -ForegroundColor Cyan
    
} catch {
    Write-Host "=== ОШИБКА ЗАПРОСА ===" -ForegroundColor Red
    Write-Host "Ошибка: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Детали: $($_.ErrorDetails.Message)" -ForegroundColor Red
    
    # Проверим, запущен ли сервер
    try {
        $testConnection = Invoke-WebRequest -Uri "http://localhost:5000" -Method Get -TimeoutSec 5
        Write-Host "Сервер отвечает, возможно проблема с endpoint'ом" -ForegroundColor Yellow
    } catch {
        Write-Host "Сервер не отвечает на http://localhost:5000" -ForegroundColor Red
        Write-Host "Убедитесь, что приложение запущено командой: dotnet run" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== ДОПОЛНИТЕЛЬНАЯ ИНФОРМАЦИЯ ===" -ForegroundColor Blue
Write-Host "Для анализа результатов проверьте файл test-results.txt" -ForegroundColor White
Write-Host "Ожидаемые результаты:" -ForegroundColor White
Write-Host "- Ошибки кавычек: 15-25 случаев" -ForegroundColor White
Write-Host "- Ошибки дефисов: 10-20 случаев" -ForegroundColor White
Write-Host "- Общее количество проблем: 25-45" -ForegroundColor White 