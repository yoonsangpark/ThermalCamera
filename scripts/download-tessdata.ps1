# tessdata/eng.traineddata 다운로드 스크립트
# 실행: .\scripts\download-tessdata.ps1

$tessdataUrl = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
$targetDir = Join-Path (Join-Path $PSScriptRoot "..") "tessdata"
$targetFile = Join-Path $targetDir "eng.traineddata"

if (Test-Path $targetFile) {
    Write-Host "eng.traineddata 이미 존재합니다: $targetFile"
    exit 0
}

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
Write-Host "다운로드 중: $tessdataUrl"
Invoke-WebRequest -Uri $tessdataUrl -OutFile $targetFile -UseBasicParsing
Write-Host "완료: $targetFile"
