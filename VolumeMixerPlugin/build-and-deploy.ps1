# Build and Deploy VolumeMixerPlugin to Macro Deck
# Run this script from the project root directory

param(
    [switch]$Release,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot
$ProjectFile = Join-Path $ProjectDir "VolumeMixerPlugin.csproj"
$Configuration = if ($Release) { "Release" } else { "Debug" }
$OutputDir = Join-Path $ProjectDir "bin" $Configuration "net8.0-windows"
$PluginDestination = "$env:APPDATA\Macro Deck\plugins\VolumeMixerPlugin"

Write-Host "=== VolumeMixer Plugin Build & Deploy ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Output: $OutputDir"
Write-Host "Destination: $PluginDestination"
Write-Host ""

if (-not $SkipBuild) {
    Write-Host "[1/4] Building project..." -ForegroundColor Yellow
    dotnet build $ProjectFile -c $Configuration
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build successful!" -ForegroundColor Green
} else {
    Write-Host "[1/4] Skipping build..." -ForegroundColor Gray
}

Write-Host ""
Write-Host "[2/4] Checking if Macro Deck is running..." -ForegroundColor Yellow
$macroDeckProcess = Get-Process -Name "Macro Deck 2" -ErrorAction SilentlyContinue
if ($macroDeckProcess) {
    Write-Host "Macro Deck is running. Stopping it..." -ForegroundColor Yellow
    Stop-Process -Name "Macro Deck 2" -Force
    Start-Sleep -Seconds 2
    Write-Host "Macro Deck stopped." -ForegroundColor Green
} else {
    Write-Host "Macro Deck is not running." -ForegroundColor Gray
}

Write-Host ""
Write-Host "[3/4] Deploying to Macro Deck plugins folder..." -ForegroundColor Yellow

if (Test-Path $PluginDestination) {
    Write-Host "Removing old plugin files..." -ForegroundColor Gray
    Remove-Item -Path $PluginDestination -Recurse -Force
}

New-Item -ItemType Directory -Path $PluginDestination -Force | Out-Null

$filesToCopy = @(
    "VolumeMixerPlugin.dll",
    "VolumeMixerPlugin.pdb",
    "NAudio.dll",
    "NAudio.Core.dll",
    "NAudio.Wasapi.dll",
    "ExtensionManifest.json"
)

foreach ($file in $filesToCopy) {
    $sourcePath = Join-Path $OutputDir $file
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $PluginDestination -Force
        Write-Host "  Copied: $file" -ForegroundColor Gray
    } else {
        Write-Host "  Warning: $file not found" -ForegroundColor Yellow
    }
}

Write-Host "Plugin deployed successfully!" -ForegroundColor Green

Write-Host ""
Write-Host "[4/4] Starting Macro Deck..." -ForegroundColor Yellow
$macroDeckPath = "${env:ProgramFiles}\Macro Deck 2\Macro Deck 2.exe"
$macroDeckPathX86 = "${env:ProgramFiles(x86)}\Macro Deck 2\Macro Deck 2.exe"

if (Test-Path $macroDeckPath) {
    Start-Process $macroDeckPath
    Write-Host "Macro Deck started!" -ForegroundColor Green
} elseif (Test-Path $macroDeckPathX86) {
    Start-Process $macroDeckPathX86
    Write-Host "Macro Deck started!" -ForegroundColor Green
} else {
    Write-Host "Macro Deck executable not found. Please start it manually." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Done! ===" -ForegroundColor Cyan
Write-Host "Check Macro Deck logs at: $env:APPDATA\Macro Deck\logs\" -ForegroundColor Gray
