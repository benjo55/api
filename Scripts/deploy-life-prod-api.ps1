param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot "..\api.csproj"),
    [string]$PublishDir = (Join-Path $PSScriptRoot "..\publish"),
    [string]$TargetDir = "C:\inetpub\wwwroot\api",
    [string]$TargetComputerName,
    [string]$Configuration = "Release",
    [switch]$DryRun,
    [switch]$Mirror
)

$ErrorActionPreference = "Stop"

function Resolve-FullPath([string]$Path) {
    $executionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

$project = Resolve-FullPath $ProjectPath
$publish = Resolve-FullPath $PublishDir
$target = if ($TargetComputerName) {
    if ($TargetDir -notmatch "^[A-Za-z]:\\") {
        throw "When TargetComputerName is set, TargetDir must be a local drive path on the server, for example C:\inetpub\wwwroot\api"
    }

    $drive = $TargetDir.Substring(0, 1)
    $pathWithoutDrive = $TargetDir.Substring(2).TrimStart("\")
    "\\$TargetComputerName\$drive`$\$pathWithoutDrive"
} elseif ($TargetDir -like "\\*") {
    $TargetDir
} else {
    Resolve-FullPath $TargetDir
}

if (-not (Test-Path $project)) {
    throw "Project not found: $project"
}

if (-not (Test-Path $target)) {
    throw "Target folder not found: $target"
}

Write-Host "Publishing $project -> $publish"
dotnet publish $project -c $Configuration -o $publish

$appOfflinePath = Join-Path $target "app_offline.htm"
$createdAppOffline = $false

try {
    if (-not $DryRun) {
        "<html><body>Deployment in progress.</body></html>" | Set-Content -Path $appOfflinePath -Encoding UTF8
        $createdAppOffline = $true
        Start-Sleep -Seconds 2
    }

    $copyMode = if ($Mirror) { "/MIR" } else { "/E" }
    $robocopyArgs = @(
        $publish,
        $target,
        $copyMode,
        "/FFT",
        "/R:2",
        "/W:2",
        "/NP",
        "/MT:16",
        "/XD",
        "App_Data",
        "logs",
        "Backup",
        "/XF",
        "appsettings.Production.json"
    )

    if ($DryRun) {
        $robocopyArgs += "/L"
    }

    Write-Host "Synchronizing changed files with robocopy..."
    & robocopy @robocopyArgs
    $robocopyExitCode = $LASTEXITCODE

    if ($robocopyExitCode -gt 7) {
        throw "Robocopy failed with exit code $robocopyExitCode"
    }

    if ($DryRun) {
        Write-Host "Dry run completed. No files were copied."
    } else {
        Write-Host "Deployment completed. Robocopy exit code: $robocopyExitCode"
    }
}
finally {
    if ($createdAppOffline -and (Test-Path $appOfflinePath)) {
        Remove-Item -Path $appOfflinePath -Force
    }
}
