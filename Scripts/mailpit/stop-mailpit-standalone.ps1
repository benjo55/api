$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$toolsDir = Join-Path $root ".tools\mailpit"
$pidFile = Join-Path $toolsDir "mailpit.pid"

if (-not (Test-Path $pidFile)) {
    Write-Host "Aucun PID Mailpit trouvé."
    exit 0
}

$pidText = Get-Content $pidFile -Raw
if ($pidText -notmatch '^\d+$') {
    Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
    throw "Le fichier PID Mailpit est invalide."
}

$pid = [int]$pidText
$process = Get-Process -Id $pid -ErrorAction SilentlyContinue
if ($process) {
    Stop-Process -Id $pid -Force
    Write-Host "Mailpit arrêté (PID $pid)."
} else {
    Write-Host "Le processus Mailpit n'existait déjà plus (PID $pid)."
}

Remove-Item $pidFile -Force -ErrorAction SilentlyContinue