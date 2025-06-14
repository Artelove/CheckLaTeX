# Тестирование диагностических возможностей CheckLaTeX Server
# Этот скрипт проверяет, что сервер возвращает точные позиции ошибок для VS Code

Write-Host "🧪 Тестирование диагностических возможностей CheckLaTeX Server" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green

# Проверяем, что сервер запущен
$serverUrl = "http://localhost:5000"
$checkEndpoint = "$serverUrl/api/lint/check"

Write-Host "📡 Проверяем доступность сервера..." -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "$serverUrl/health" -Method GET -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ Сервер доступен" -ForegroundColor Green
} catch {
    Write-Host "❌ Сервер недоступен. Запустите сервер командой: cd tex-lint && dotnet run" -ForegroundColor Red
    Write-Host "   Подождите пока сервер запустится, затем повторите тест" -ForegroundColor Yellow
    exit 1
}

# Тестовый LaTeX документ с намеренными ошибками
$testLatex = @"
\documentclass{article}
\usepackage[utf8]{inputenc}
\usepackage[russian]{babel}

\begin{document}

\title{Тестовый документ}
\author{Тестировщик}
\date{\today}
\maketitle

\section{Введение}

Этот текст содержит "неправильные" кавычки для тестирования.
Также здесь есть дефис - который должен быть тире.

Еще один пример с 'одинарными' кавычками.
И еще один дефис - тире для проверки.

\section{Заключение}

Текст с "двойными" кавычками и дефисом - тире.

\end{document}
"@

Write-Host "📄 Тестовый LaTeX документ создан" -ForegroundColor Cyan
Write-Host "   Содержит ошибки: неправильные кавычки и дефисы вместо тире" -ForegroundColor Yellow

# Подготавливаем JSON запрос
$requestBody = @{
    content = $testLatex
} | ConvertTo-Json -Depth 3

Write-Host "🚀 Отправляем запрос на анализ..." -ForegroundColor Cyan

try {
    $headers = @{
        "Content-Type" = "application/json"
        "Accept" = "application/json"
    }
    
    $response = Invoke-RestMethod -Uri $checkEndpoint -Method POST -Body $requestBody -Headers $headers -TimeoutSec 30
    
    Write-Host "✅ Запрос выполнен успешно" -ForegroundColor Green
    Write-Host "📊 Найдено команд: $($response.commandsFound)" -ForegroundColor Cyan
    
    # Анализируем результаты тестирования
    Write-Host "" 
    Write-Host "🔍 Результаты диагностики:" -ForegroundColor Yellow
    Write-Host "=========================" -ForegroundColor Yellow
    
    $totalErrors = 0
    $diagnosticErrors = 0
    
    foreach ($testResult in $response.testResults) {
        $errorCount = $testResult.errors.Count
        $totalErrors += $errorCount
        
        if ($errorCount -gt 0) {
            Write-Host ""
            Write-Host "📋 Тест: $($testResult.testName)" -ForegroundColor Magenta
            Write-Host "   Найдено ошибок: $errorCount" -ForegroundColor White
            
            foreach ($error in $testResult.errors) {
                Write-Host "   ├─ Тип: $($error.type)" -ForegroundColor White
                Write-Host "   ├─ Сообщение: $($error.info)" -ForegroundColor White
                
                # Проверяем наличие диагностических полей
                if ($error.fileName -or $error.lineNumber -or $error.columnNumber) {
                    $diagnosticErrors++
                    Write-Host "   ├─ 📍 Диагностика:" -ForegroundColor Green
                    
                    if ($error.fileName) {
                        Write-Host "   │  ├─ Файл: $($error.fileName)" -ForegroundColor Green
                    }
                    if ($error.lineNumber) {
                        Write-Host "   │  ├─ Строка: $($error.lineNumber)" -ForegroundColor Green
                    }
                    if ($error.columnNumber) {
                        Write-Host "   │  ├─ Колонка: $($error.columnNumber)" -ForegroundColor Green
                    }
                    if ($error.endLineNumber -and $error.endColumnNumber) {
                        Write-Host "   │  ├─ Конец: строка $($error.endLineNumber), колонка $($error.endColumnNumber)" -ForegroundColor Green
                    }
                    if ($error.originalText) {
                        Write-Host "   │  ├─ Контекст: '$($error.originalText)'" -ForegroundColor Green
                    }
                    if ($error.suggestedFix) {
                        Write-Host "   │  └─ Исправление: '$($error.suggestedFix)'" -ForegroundColor Green
                    }
                } else {
                    Write-Host "   └─ ⚠️  Диагностические данные отсутствуют" -ForegroundColor Red
                }
                
                Write-Host ""
            }
        }
    }
    
    # Итоговая статистика
    Write-Host "📈 Итоговая статистика:" -ForegroundColor Yellow
    Write-Host "======================" -ForegroundColor Yellow
    Write-Host "Всего ошибок: $totalErrors" -ForegroundColor White
    Write-Host "С диагностикой: $diagnosticErrors" -ForegroundColor Green
    Write-Host "Без диагностики: $($totalErrors - $diagnosticErrors)" -ForegroundColor Red
    
    if ($diagnosticErrors -gt 0) {
        $percentage = [math]::Round(($diagnosticErrors / $totalErrors) * 100, 1)
        Write-Host "Покрытие диагностикой: $percentage%" -ForegroundColor Green
        
        if ($percentage -eq 100) {
            Write-Host "🎉 Отлично! Все ошибки содержат диагностические данные" -ForegroundColor Green
        } elseif ($percentage -ge 50) {
            Write-Host "✅ Хорошо! Большинство ошибок содержат диагностические данные" -ForegroundColor Yellow
        } else {
            Write-Host "⚠️  Внимание! Мало ошибок содержат диагностические данные" -ForegroundColor Red
        }
    } else {
        Write-Host "❌ Диагностические данные не найдены!" -ForegroundColor Red
    }
    
    # Проверяем структуру ответа
    Write-Host ""
    Write-Host "🔧 Проверка структуры API:" -ForegroundColor Yellow
    Write-Host "=========================" -ForegroundColor Yellow
    
    $apiChecksPassed = 0
    $totalApiChecks = 4
    
    # Проверка 1: Наличие основных полей
    if ($response.commandsFound -ne $null -and $response.testResults -ne $null) {
        Write-Host "✅ Основные поля ответа присутствуют" -ForegroundColor Green
        $apiChecksPassed++
    } else {
        Write-Host "❌ Основные поля ответа отсутствуют" -ForegroundColor Red
    }
    
    # Проверка 2: Структура testResults
    if ($response.testResults -is [array] -and $response.testResults.Count -gt 0) {
        Write-Host "✅ testResults является массивом с данными" -ForegroundColor Green
        $apiChecksPassed++
    } else {
        Write-Host "❌ testResults не является массивом или пустой" -ForegroundColor Red
    }
    
    # Проверка 3: Структура errors
    $hasErrorStructure = $false
    foreach ($testResult in $response.testResults) {
        if ($testResult.errors -is [array]) {
            $hasErrorStructure = $true
            break
        }
    }
    
    if ($hasErrorStructure) {
        Write-Host "✅ Структура errors корректна" -ForegroundColor Green
        $apiChecksPassed++
    } else {
        Write-Host "❌ Структура errors некорректна" -ForegroundColor Red
    }
    
    # Проверка 4: Новые диагностические поля
    $hasDiagnosticFields = $false
    foreach ($testResult in $response.testResults) {
        foreach ($error in $testResult.errors) {
            if ($error.PSObject.Properties.Name -contains "fileName" -or
                $error.PSObject.Properties.Name -contains "lineNumber" -or
                $error.PSObject.Properties.Name -contains "columnNumber") {
                $hasDiagnosticFields = $true
                break
            }
        }
        if ($hasDiagnosticFields) { break }
    }
    
    if ($hasDiagnosticFields) {
        Write-Host "✅ Диагностические поля присутствуют в API" -ForegroundColor Green
        $apiChecksPassed++
    } else {
        Write-Host "❌ Диагностические поля отсутствуют в API" -ForegroundColor Red
    }
    
    Write-Host "API проверки пройдено: $apiChecksPassed/$totalApiChecks" -ForegroundColor White
    
    # Общий результат
    Write-Host ""
    Write-Host "🏁 ИТОГОВЫЙ РЕЗУЛЬТАТ:" -ForegroundColor Yellow
    Write-Host "=====================" -ForegroundColor Yellow
    
    if ($apiChecksPassed -eq $totalApiChecks -and $diagnosticErrors -gt 0) {
        Write-Host "🎉 УСПЕХ! Диагностические возможности работают корректно" -ForegroundColor Green
        Write-Host "   VS Code расширение получит точные позиции ошибок" -ForegroundColor Green
        exit 0
    } elseif ($apiChecksPassed -ge 3) {
        Write-Host "⚠️  ЧАСТИЧНО РАБОТАЕТ: Некоторые проверки не прошли" -ForegroundColor Yellow
        Write-Host "   Требуется дополнительная настройка" -ForegroundColor Yellow
        exit 1
    } else {
        Write-Host "❌ ОШИБКА: Диагностические возможности не работают" -ForegroundColor Red
        Write-Host "   Проверьте изменения в серверном коде" -ForegroundColor Red
        exit 2
    }
    
} catch {
    Write-Host "❌ Ошибка при выполнении запроса:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Message -match "404") {
        Write-Host "💡 Подсказка: Убедитесь, что эндпоинт /api/lint/check существует" -ForegroundColor Yellow
    } elseif ($_.Exception.Message -match "500") {
        Write-Host "💡 Подсказка: Проверьте логи сервера на наличие ошибок" -ForegroundColor Yellow
    } elseif ($_.Exception.Message -match "timeout") {
        Write-Host "💡 Подсказка: Сервер работает медленно или завис" -ForegroundColor Yellow
    }
    
    exit 3
} 