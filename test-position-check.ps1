#!/usr/bin/env pwsh

Write-Host "=== Тест позиционирования ошибок ===" -ForegroundColor Green
Write-Host ""

# Перейти в папку проекта
cd tex-lint

Write-Host "Собираем проект..." -ForegroundColor Yellow
dotnet build --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "Ошибка при сборке проекта!" -ForegroundColor Red
    exit 1
}

Write-Host "Запускаем сервер в фоновом режиме..." -ForegroundColor Yellow
$serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -PassThru -NoNewWindow
Start-Sleep -Seconds 3

try {
    Write-Host "Тестируем анализ файла test-position-check.tex..." -ForegroundColor Yellow
    Write-Host ""
    
    # Создаем JSON запрос
    $requestBody = @{
        filePath = "test-position-check.tex"
        latexContent = Get-Content "../test-position-check.tex" -Raw
    } | ConvertTo-Json -Depth 10
    
    # Отправляем запрос
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/lint/check" -Method Post -Body $requestBody -ContentType "application/json"
    
    Write-Host "=== РЕЗУЛЬТАТЫ ===" -ForegroundColor Cyan
    Write-Host "Найдено команд: $($response.commandsFound)" -ForegroundColor White
    Write-Host ""
    
    # Анализируем результаты тестов
    foreach ($testResult in $response.testResults) {
        if ($testResult.errors.Count -gt 0) {
            Write-Host "❌ $($testResult.testName):" -ForegroundColor Red
            foreach ($error in $testResult.errors) {
                Write-Host "   Файл: $($error.fileName)" -ForegroundColor Gray
                Write-Host "   Строка: $($error.lineNumber), Колонка: $($error.columnNumber)" -ForegroundColor Gray
                Write-Host "   Конец: Строка: $($error.endLineNumber), Колонка: $($error.endColumnNumber)" -ForegroundColor Gray
                Write-Host "   Тип: $($error.type)" -ForegroundColor Gray
                Write-Host "   Сообщение: $($error.info)" -ForegroundColor Yellow
                Write-Host "   Контекст: '$($error.originalText)'" -ForegroundColor Magenta
                Write-Host ""
            }
        } else {
            Write-Host "✅ $($testResult.testName): Ошибок нет" -ForegroundColor Green
        }
    }
    
    Write-Host ""
    Write-Host "=== ПРОВЕРКА ПОЗИЦИЙ ===" -ForegroundColor Cyan
    
    # Читаем тестовый файл для сравнения
    $testFileContent = Get-Content "../test-position-check.tex"
    for ($i = 0; $i -lt $testFileContent.Length; $i++) {
        $lineNumber = $i + 1
        $line = $testFileContent[$i]
        Write-Host "Строка $lineNumber`: '$line'" -ForegroundColor White
        
        # Показываем позиции символов
        $positions = ""
        for ($j = 0; $j -lt $line.Length; $j++) {
            $positions += ($j + 1).ToString().PadLeft(1)
        }
        if ($positions.Length -gt 0) {
            Write-Host "Позиции: $positions" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Host "Ошибка при тестировании: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Write-Host ""
    Write-Host "Завершаем сервер..." -ForegroundColor Yellow
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "Тест завершен." -ForegroundColor Green 