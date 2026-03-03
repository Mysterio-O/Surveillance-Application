
param(
  [switch]$InstallDotNet = $true,
  [switch]$InstallTesseract = $true
)

Write-Host "== Surveil-Win setup =="

# .NET SDK
if ($InstallDotNet) {
  try {
    dotnet --info | Out-Null
    Write-Host "[OK] .NET SDK found"
  } catch {
    Write-Host "[INFO] Installing .NET 8 SDK via winget..."
    winget install Microsoft.DotNet.SDK.8 --silent
  }
}

# Tesseract OCR
if ($InstallTesseract) {
  $tes = Get-Command tesseract -ErrorAction SilentlyContinue
  if (-not $tes) {
    Write-Host "[INFO] Installing Tesseract OCR via winget (UB-Mannheim build)..."
    winget install UB-Mannheim.TesseractOCR --silent
  } else {
    Write-Host "[OK] Tesseract found"
  }
}

# ONNX model (CLIP small). This is optional; embeddings are disabled without it.
$onnxDir = Join-Path $PSScriptRoot "..\models\onnx"
$newPath = New-Item -ItemType Directory -Force -Path $onnxDir
$modelPath = Join-Path $onnxDir "clip-vit-b32.onnx"
if (-not (Test-Path $modelPath)) {
  Write-Host "[INFO] Downloading ONNX model (approx 100MB)."
  $url = "https://github.com/justinjmoses/onnx-clip-models/releases/download/v1.0/clip-vit-b32.onnx"
  Invoke-WebRequest -Uri $url -OutFile $modelPath
}

Write-Host "[INFO] Restoring NuGet packages..."
Push-Location (Join-Path $PSScriptRoot "..")
dotnet restore
Pop-Location

Write-Host "== Setup complete =="
