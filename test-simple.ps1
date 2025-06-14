# Simple PowerShell script for testing CheckLaTeX API

$apiUrl = "http://localhost:5000/api/lint"

# Read test document
$testDocContent = Get-Content -Path "test-document.tex" -Raw -Encoding UTF8

# Prepare JSON request
$requestBody = @{
    StartFile = "test-document.tex"
    Files = @{
        "test-document.tex" = $testDocContent
    }
} | ConvertTo-Json -Depth 3

# Headers
$headers = @{
    "Content-Type" = "application/json; charset=utf-8"
}

Write-Host "Testing CheckLaTeX API..."
Write-Host "URL: $apiUrl"
Write-Host "Document size: $($testDocContent.Length) characters"
Write-Host ""

try {
    # Send request
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $requestBody -Headers $headers -ContentType "application/json; charset=utf-8"
    
    Write-Host "=== ANALYSIS RESULT ==="
    Write-Host $response
    
    # Save result to file
    $response | Out-File -FilePath "test-results.txt" -Encoding UTF8
    Write-Host ""
    Write-Host "Results saved to test-results.txt"
    
} catch {
    Write-Host "=== ERROR ==="
    Write-Host "Error: $($_.Exception.Message)"
    Write-Host "Details: $($_.ErrorDetails.Message)"
    
    # Check if server is running
    try {
        $testConnection = Invoke-WebRequest -Uri "http://localhost:5000" -Method Get -TimeoutSec 5
        Write-Host "Server responds, possibly endpoint issue"
    } catch {
        Write-Host "Server not responding on http://localhost:5000"
        Write-Host "Make sure application is running with: dotnet run"
    }
} 