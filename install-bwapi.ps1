# Install BWAPI 4.4.0 to local Starcraft folder
$ErrorActionPreference = "Stop"


$bwapiVersion = "4.4.0"
$downloadUrl = "https://github.com/bwapi/bwapi/releases/download/v${bwapiVersion}/BWAPI_Setup.exe"
$tempDir = "$env:TEMP\bwapi-install"
$setupFile = "$tempDir\BWAPI_Setup.exe"
$starcraftPath = "$PSScriptRoot\Starcraft"

Write-Host "Installing BWAPI $bwapiVersion to $starcraftPath" -ForegroundColor Cyan

# Create temp directory
if (!(Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
}

# Download BWAPI
Write-Host "Downloading BWAPI $bwapiVersion..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $setupFile -UseBasicParsing
    Write-Host "Download complete!" -ForegroundColor Green
} catch {
    Write-Host "Error downloading BWAPI: $_" -ForegroundColor Red
    exit 1
}

# Extract using 7zip or expand (BWAPI setup is an NSIS installer, we need to extract it)
Write-Host "Extracting BWAPI..." -ForegroundColor Yellow

# Try using 7zip if available
$sevenZip = "C:\Program Files\7-Zip\7z.exe"
if (Test-Path $sevenZip) {
    & $sevenZip x "$setupFile" "-o$tempDir\extracted" -y | Out-Null
} else {
    # Alternative: Run the installer silently to default location then copy
    Write-Host "7-Zip not found. Running BWAPI installer..." -ForegroundColor Yellow
    Write-Host "Note: You may need to manually specify the StarCraft directory as: $starcraftPath" -ForegroundColor Cyan
    Start-Process -FilePath $setupFile -ArgumentList "/S", "/D=$starcraftPath" -Wait
    Write-Host "BWAPI installation complete!" -ForegroundColor Green
    
    # Cleanup
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    exit 0
}

# Copy BWAPI files to Starcraft folder
Write-Host "Copying files to StarCraft directory..." -ForegroundColor Yellow

$bwapiFiles = @(
    "$tempDir\extracted\BWAPI.dll",
    "$tempDir\extracted\BWAPIClient.dll",
    "$tempDir\extracted\bwapi-data"
)

foreach ($item in $bwapiFiles) {
    if (Test-Path $item) {
        $destination = Join-Path $starcraftPath (Split-Path $item -Leaf)
        Copy-Item -Path $item -Destination $destination -Recurse -Force
        Write-Host "  Copied $(Split-Path $item -Leaf)" -ForegroundColor Gray
    }
}

# Cleanup
Write-Host "Cleaning up..." -ForegroundColor Yellow
Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`nBWAPI $bwapiVersion has been installed successfully!" -ForegroundColor Green
Write-Host "Location: $starcraftPath" -ForegroundColor Cyan
