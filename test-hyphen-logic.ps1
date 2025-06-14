# ���� ������ �������
Write-Host "������������ ������ �������..." -ForegroundColor Green

# ������� ZIP ����� � �������� ������
$zipPath = "test-hyphen-logic.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath
}

# ������� �����
Compress-Archive -Path "test-hyphen-logic.tex" -DestinationPath $zipPath

# ���������� ������ � API
$uri = "http://localhost:5000/api/lint/analyze-zip"
$headers = @{
    "Content-Type" = "application/octet-stream"
}

try {
    Write-Host "�������� ZIP ������ �� ������..." -ForegroundColor Yellow
    
    $fileBytes = [System.IO.File]::ReadAllBytes($zipPath)
    $response = Invoke-RestMethod -Uri $uri -Method Post -Body $fileBytes -Headers $headers
    
    Write-Host "����� �������!" -ForegroundColor Green
    
    # ����������� ���������� ��� �������
    if ($response.results -and $response.results.Count -gt 0) {
        Write-Host "`n������ ����������� ��� �������:" -ForegroundColor Magenta
        
        foreach ($result in $response.results) {
            if ($result.testName -like "*Hyphen*" -and $result.errors -and $result.errors.Count -gt 0) {
                Write-Host "���� ������� - ������� ������: $($result.errors.Count)" -ForegroundColor Red
                
                foreach ($error in $result.errors) {
                    Write-Host "  - ����: $($error.fileName)" -ForegroundColor White
                    Write-Host "    ������: $($error.lineNumber), �������: $($error.columnNumber)" -ForegroundColor White
                    Write-Host "    ���������: $($error.message)" -ForegroundColor White
                    Write-Host "    ������������ �����: '$($error.originalText)'" -ForegroundColor Gray
                    if ($error.suggestedFix) {
                        Write-Host "    ������������ �����������: '$($error.suggestedFix)'" -ForegroundColor Green
                    }
                    Write-Host ""
                }
            } elseif ($result.testName -like "*Hyphen*") {
                Write-Host "���� ������� - ������ �� �������" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "���������� ������� �����" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "������ ��� �������� �������: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "������: $($_.Exception)" -ForegroundColor Red
}

# �������
if (Test-Path $zipPath) {
    Remove-Item $zipPath
    Write-Host "��������� ����� ������" -ForegroundColor Gray
}