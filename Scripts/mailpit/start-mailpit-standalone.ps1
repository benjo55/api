$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$toolsDir = Join-Path $root ".tools\mailpit"
$versionFile = Join-Path $toolsDir "version.txt"
$pidFile = Join-Path $toolsDir "mailpit.pid"
$exePath = Join-Path $toolsDir "mailpit.exe"
$releaseApi = "https://api.github.com/repos/axllent/mailpit/releases/latest"

New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null

if (-not (Get-Command Invoke-RestMethod -ErrorAction SilentlyContinue)) {
    throw "Invoke-RestMethod est indisponible dans cette session PowerShell."
}

$release = Invoke-RestMethod -Uri $releaseApi -Headers @{ "User-Agent" = "PowerShell" }
$tagName = [string]$release.tag_name
$windowsAsset = $release.assets | Where-Object { $_.name -eq "mailpit-windows-amd64.zip" } | Select-Object -First 1

if (-not $windowsAsset) {
    throw "Impossible de trouver l'asset mailpit-windows-amd64.zip pour $tagName."
}

if ((Test-Path $exePath) -and (Test-Path $versionFile)) {
    $currentVersion = Get-Content $versionFile -Raw
    if ($currentVersion -eq $tagName) {
        Write-Host "Mailpit $tagName est déjà installé dans $toolsDir."
    }
    else {
        Remove-Item $exePath -Force -ErrorAction SilentlyContinue
    }
}

if (-not (Test-Path $exePath)) {
    $zipPath = Join-Path $toolsDir "mailpit-$tagName.zip"
    Write-Host "Téléchargement de Mailpit $tagName..."
    Invoke-WebRequest -Uri $windowsAsset.browser_download_url -OutFile $zipPath

    $extractDir = Join-Path $toolsDir "extract"
    Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue
    Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

    $foundExe = Get-ChildItem -Path $extractDir -Recurse -Filter "mailpit.exe" | Select-Object -First 1
    if (-not $foundExe) {
        throw "mailpit.exe introuvable après extraction."
    }

    Copy-Item $foundExe.FullName $exePath -Force
    Set-Content -Path $versionFile -Value $tagName
    Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
    Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue
}

if (Test-Path $pidFile) {
    $existingPid = Get-Content $pidFile -Raw
    if ($existingPid -match '^\d+$' -and (Get-Process -Id [int]$existingPid -ErrorAction SilentlyContinue)) {
        Write-Host "Mailpit est déjà lancé (PID $existingPid)."
        Write-Host "UI: http://localhost:8025"
        Write-Host "SMTP: 127.0.0.1:1025"
        exit 0
    }
    Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
}

$env:MP_SMTP_BIND_ADDR = "127.0.0.1:1025"
$env:MP_UI_BIND_ADDR = "127.0.0.1:8025"

Remove-Item Env:MP_SMTP_AUTH -ErrorAction SilentlyContinue
Remove-Item Env:MP_SMTP_AUTH_FILE -ErrorAction SilentlyContinue
Remove-Item Env:MP_SMTP_AUTH_ALLOW_INSECURE -ErrorAction SilentlyContinue
Remove-Item Env:MP_SMTP_AUTH_ACCEPT_ANY -ErrorAction SilentlyContinue
Remove-Item Env:MP_SMTP_REQUIRE_STARTTLS -ErrorAction SilentlyContinue
Remove-Item Env:MP_SMTP_REQUIRE_TLS -ErrorAction SilentlyContinue

$process = Start-Process -FilePath $exePath -WorkingDirectory $toolsDir -PassThru
Set-Content -Path $pidFile -Value $process.Id

Write-Host "Mailpit $tagName lancé."
Write-Host "PID: $($process.Id)"
Write-Host "UI: http://localhost:8025"
Write-Host "SMTP: 127.0.0.1:1025"