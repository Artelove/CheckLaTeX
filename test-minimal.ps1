# Minimal test for CheckLaTeX API

$apiUrl = "http://localhost:5000/api/lint"

# Simple test content
$testContent = "Простой текст с \"неправильными кавычками\" и дефисом - который нужно найти."

# Simple JSON request
$requestBody = @{
    StartFile = "simple.tex"
    Files = @{
        "simple.tex" = $testContent
    }
} | ConvertTo-Json -Depth 2

Write-Host "Testing with minimal content..."
Write-Host "Content: $testContent"
Write-Host "Request body:"
Write-Host $requestBody
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $requestBody -ContentType "application/json"
    Write-Host "SUCCESS! Response:"
    Write-Host $response
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)"
    }
} 