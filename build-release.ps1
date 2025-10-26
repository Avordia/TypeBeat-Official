# TypeBeat Release Builder
# This script builds and packages TypeBeat for distribution with auto-update support

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [string]$OutputDir = ".\Releases"
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  TypeBeat Release Builder" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Build the application
Write-Host "Building TypeBeat v$Version..." -ForegroundColor Yellow
$publishDir = ".\TypeBeat.Desktop\bin\$Configuration\net8.0\win-x64\publish"

dotnet publish .\TypeBeat.Desktop\TypeBeat.Desktop.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:Version=$Version `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Create NuGet package for Squirrel
Write-Host "Creating NuGet package..." -ForegroundColor Yellow

$nuspecContent = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
  <metadata>
    <id>TypeBeat</id>
    <version>$Version</version>
    <title>TypeBeat</title>
    <authors>TypeBeat Team</authors>
    <description>A rhythm game built with osu!framework</description>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
  </metadata>
</package>
"@

$nuspecPath = Join-Path $publishDir "TypeBeat.nuspec"
$nuspecContent | Out-File -FilePath $nuspecPath -Encoding UTF8

# Package with Squirrel
Write-Host "Packaging with Squirrel..." -ForegroundColor Yellow

# Download Squirrel if not present
$squirrelDir = Join-Path $env:TEMP "SquirrelTemp"
$squirrelExe = Join-Path $squirrelDir "tools\squirrel.exe"

if (-not (Test-Path $squirrelExe)) {
    Write-Host "Downloading Squirrel.Windows..." -ForegroundColor Yellow
    
    # Clean and recreate directory
    if (Test-Path $squirrelDir) {
        Remove-Item -Path $squirrelDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $squirrelDir -Force | Out-Null
    
    # Download Squirrel package
    $squirrelNuget = "https://www.nuget.org/api/v2/package/Clowd.Squirrel/2.11.1"
    $zipPath = Join-Path $squirrelDir "squirrel.zip"
    
    try {
        Invoke-WebRequest -Uri $squirrelNuget -OutFile $zipPath -UseBasicParsing
        Expand-Archive -Path $zipPath -DestinationPath $squirrelDir -Force
        
        # Verify we have the necessary files
        if ((Test-Path $squirrelExe) -and (Test-Path (Join-Path $squirrelDir "tools\Update.exe"))) {
            Write-Host "Squirrel downloaded successfully" -ForegroundColor Green
        } else {
            Write-Host "Could not find required Squirrel files in package" -ForegroundColor Red
            Write-Host "Looking for files in: $squirrelDir" -ForegroundColor Yellow
            exit 1
        }
    } catch {
        Write-Host "Failed to download Squirrel: $_" -ForegroundColor Red
        exit 1
    }
}

# Create Squirrel package
Write-Host "Creating installer package..." -ForegroundColor Yellow
& $squirrelExe pack `
    --packId "TypeBeat" `
    --packVersion $Version `
    --packDirectory $publishDir `
    --releaseDir $OutputDir `
    --icon ".\TypeBeat.Desktop\game.ico" `
    --allowUnaware

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host "  Release package created successfully!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Output directory: $OutputDir" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Files created:" -ForegroundColor Yellow
    Get-ChildItem $OutputDir | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "To distribute:" -ForegroundColor Cyan
    Write-Host "  1. Upload all files in $OutputDir to your update server" -ForegroundColor White
    Write-Host "  2. Users can install using TypeBeatSetup.exe" -ForegroundColor White
    Write-Host "  3. Updates will be automatically detected and downloaded" -ForegroundColor White
} else {
    Write-Host "Packaging failed!" -ForegroundColor Red
    exit 1
}
