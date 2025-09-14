# setup-ffmpeg.ps1
param(
    [string]$Destination = "$env:USERPROFILE\ffmpeg"
)

$ErrorActionPreference = "Stop"

# Latest FFmpeg static build (essentials) from Gyan.dev
$ffmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
$zipPath = "$env:TEMP\ffmpeg.zip"

Write-Host "Downloading FFmpeg from $ffmpegUrl ..."
Invoke-WebRequest -Uri $ffmpegUrl -OutFile $zipPath

Write-Host "Extracting to $Destination ..."
if (Test-Path $Destination) {
    Remove-Item -Recurse -Force $Destination
}
Expand-Archive -Path $zipPath -DestinationPath $Destination

# Find extracted folder (usually versioned like ffmpeg-2025-09-14-essentials_build)
$subfolder = Get-ChildItem $Destination | Where-Object { $_.PSIsContainer } | Select-Object -First 1
$binPath = Join-Path $subfolder.FullName "bin"

Write-Host "FFmpeg binaries located in: $binPath"

# --- Permanently update PATH (user-level) ---
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")

if ($currentPath -notlike "*$binPath*") {
    $newPath = "$currentPath;$binPath"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    Write-Host "PATH updated permanently for the current user."
    Write-Host "You may need to restart your terminal or log out/in for changes to take effect."
} else {
    Write-Host "PATH already contains $binPath"
}

# Verify install
Write-Host "Testing ffmpeg version..."
& "$binPath\ffmpeg.exe" -version
& "$binPath\ffprobe.exe" -version
