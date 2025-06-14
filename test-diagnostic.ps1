# Diagnostic test for CheckLaTeX API

$apiUrl = "http://localhost:5000/api/diagnostic/test"

# Simple test content
$testContent = 'Simple text with "wrong quotes" and hyphen - to find.'

# Simple JSON request
$requestBody = @{
    StartFile = "simple.tex"
    Files = @{
        "simple.tex" = $testContent
    }
} | ConvertTo-Json -Depth 2

Write-Host "Testing diagnostic endpoint..."
Write-Host "URL: $apiUrl"
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $requestBody -ContentType "application/json"
    Write-Host "SUCCESS! Diagnostic Response:"
    Write-Host $response
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)"
    }
} 