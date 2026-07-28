#Requires -RunAsAdministrator

<#
  Diagnostic SQL pour Life Production.

  Ce script lit les variables d'environnement Machine utilisees par l'API,
  affiche les chaines avec le mot de passe masque, puis teste l'ouverture SQL.
#>

$ErrorActionPreference = "Stop"

$ApiPhysicalPath = "C:\inetpub\wwwroot\api"
if (-not (Test-Path -LiteralPath $ApiPhysicalPath)) {
    throw "Le repertoire API IIS est introuvable: $ApiPhysicalPath"
}
Set-Location -LiteralPath $ApiPhysicalPath

function Mask-ConnectionString {
    param([string] $ConnectionString)

    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        return "<vide>"
    }

    return ($ConnectionString -replace "(?i)(Password|Pwd)=([^;]*)", '$1=***')
}

function Test-SqlConnection {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [Parameter(Mandatory = $true)]
        [string] $ConnectionString
    )

    Write-Host ""
    Write-Host "[$Name]"
    Write-Host (Mask-ConnectionString $ConnectionString)

    if ($ConnectionString -match "TON_MOT_DE_PASSE|__A_COMPLETER") {
        throw "$Name contient encore une valeur placeholder."
    }

    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    try {
        $connection.Open()
        Write-Host "Connexion SQL OK: $($connection.Database) / $($connection.DataSource)"
    }
    finally {
        $connection.Dispose()
    }
}

$defaultConnection = [Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Machine")
$quartzConnection = [Environment]::GetEnvironmentVariable("Quartz__quartz.dataSource.default.connectionString", "Machine")

Test-SqlConnection "ConnectionStrings__DefaultConnection" $defaultConnection
Test-SqlConnection "Quartz__quartz.dataSource.default.connectionString" $quartzConnection

Write-Host ""
Write-Host "Diagnostic SQL OK. Tu peux recycler l'App Pool API ou lancer iisreset."
