# Тест исправления путей файлов в ответах API
Write-Host "Testing FilePath Fix" -ForegroundColor Green

$serverUrl = "http://localhost:5000"
$checkEndpoint = "$serverUrl/api/lint/check"

# Тестовый контент с ошибками
$testContent = @"
\documentclass{article}
\begin{document}

Text with "wrong" quotes.
Another example with 'single' quotes.
Text - hyphen instead of dash.

\end{document}
"@

# Оригинальный путь файла пользователя
$originalFilePath = "C:\Users\username\Documents\project\main.tex"

$requestBody = @{
    content = $testContent
    filePath = $originalFilePath
} | ConvertTo-Json

try {
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    $response = Invoke-RestMethod -Uri $checkEndpoint -Method POST -Body $requestBody -Headers $headers -TimeoutSec 30
    
    Write-Host "Request completed successfully" -ForegroundColor Green
    
    $quotationErrors = 0
    $hyphenErrors = 0
    $correctPaths = 0
    $totalErrors = 0
    
    foreach ($testResult in $response.testResults) {
        $errorCount = $testResult.errors.Count
        $totalErrors += $errorCount
        
        Write-Host "Test: $($testResult.testName) - $errorCount errors" -ForegroundColor Cyan
        
        if ($testResult.testName -eq "TestQuotationMarks") {
            $quotationErrors = $errorCount
        }
        
        if ($testResult.testName -eq "TestHyphenInsteadOfDash") {
            $hyphenErrors = $errorCount
        }
        
        foreach ($err in $testResult.errors) {
            Write-Host "  Line $($err.lineNumber), Column $($err.columnNumber)" -ForegroundColor White
            Write-Host "  File: $($err.fileName)" -ForegroundColor Yellow
            
            # Проверяем, что путь файла корректный (не временный)
            if ($err.fileName -eq $originalFilePath) {
                $correctPaths++
                Write-Host "  ✓ Correct file path" -ForegroundColor Green
            } elseif ($err.fileName -and !$err.fileName.Contains("tmp") -and !$err.fileName.Contains("temp")) {
                $correctPaths++
                Write-Host "  ✓ Non-temporary file path" -ForegroundColor Green
            } else {
                Write-Host "  ✗ Temporary file path detected: $($err.fileName)" -ForegroundColor Red
            }
        }
    }
    
    Write-Host ""
    Write-Host "RESULTS:" -ForegroundColor Yellow
    Write-Host "Quotation errors: $quotationErrors" -ForegroundColor White
    Write-Host "Hyphen errors: $hyphenErrors" -ForegroundColor White
    Write-Host "Total errors: $totalErrors" -ForegroundColor White
    Write-Host "Correct file paths: $correctPaths / $totalErrors" -ForegroundColor White
    
    # Проверяем результаты
    $success = $true
    
    if ($quotationErrors -lt 3) {
        Write-Host "ISSUE: Too few quotation errors found" -ForegroundColor Red
        $success = $false
    } else {
        Write-Host "OK: Quotation detection works" -ForegroundColor Green
    }
    
    if ($hyphenErrors -lt 1) {
        Write-Host "ISSUE: Too few hyphen errors found" -ForegroundColor Red
        $success = $false
    } else {
        Write-Host "OK: Hyphen detection works" -ForegroundColor Green
    }
    
    if ($correctPaths -ne $totalErrors) {
        Write-Host "ISSUE: Not all file paths are correct ($correctPaths/$totalErrors)" -ForegroundColor Red
        $success = $false
    } else {
        Write-Host "OK: All file paths are correct" -ForegroundColor Green
    }
    
    if ($success) {
        Write-Host "SUCCESS: FilePath fix works correctly" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "ERROR: FilePath fix has issues" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the server is running: cd tex-lint; dotnet run" -ForegroundColor Yellow
    exit 2
} 