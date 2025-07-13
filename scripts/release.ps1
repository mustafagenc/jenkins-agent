# Release için otomasyon scripti

# Uygulamayı Release modunda derle
Write-Host "[1/2] Release build başlatılıyor..."
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build başarısız! Kurulum paketi oluşturulmadı." -ForegroundColor Red
    exit 1
}

# Inno Setup ile kurulum dosyasını oluştur
Write-Host "[2/2] Inno Setup ile kurulum paketi hazırlanıyor..."
$innoPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$issFile = Join-Path $PSScriptRoot "..\JenkinsAgentSetup.iss"

if (!(Test-Path $innoPath)) {
    Write-Host "Inno Setup bulunamadı: $innoPath" -ForegroundColor Red
    exit 1
}

& "$innoPath" $issFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "Kurulum paketi başarıyla oluşturuldu." -ForegroundColor Green
} else {
    Write-Host "Kurulum paketi oluşturulamadı!" -ForegroundColor Red
    exit 1
}
