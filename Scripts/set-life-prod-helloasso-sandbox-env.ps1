#Requires -RunAsAdministrator

<#
  Bascule Life Production vers HelloAsso Sandbox.

  Objectif:
  - conserver le front/API de production euroboost.top;
  - creer les checkouts sur HelloAsso sandbox;
  - permettre les retours utilisateur vers le site de production;
  - remplacer uniquement les variables d'environnement HelloAsso / paiement utiles.

  Usage:
  1. Copier ce fichier sur le serveur Windows 2019.
  2. Renseigner les credentials sandbox HelloAsso ci-dessous.
  3. Adapter $FrontBaseUrl si le domaine canonique est different.
  4. Lancer PowerShell en administrateur:
       powershell -ExecutionPolicy Bypass -File .\set-life-prod-helloasso-sandbox-env.ps1

  Important:
  - Ne pas commit ce fichier une fois les vraies valeurs renseignees.
  - En base, l'organisme ACIC doit pointer vers:
      HelloAssoCredentialKey = 'acic-sandbox'
      HelloAssoOrganizationSlug = 'acic-tests'
      HelloAssoEnvironment = 'Sandbox'
#>

$ErrorActionPreference = "Stop"

$ApiPhysicalPath = "C:\inetpub\wwwroot\api"
if (-not (Test-Path -LiteralPath $ApiPhysicalPath)) {
    throw "Le repertoire API IIS est introuvable: $ApiPhysicalPath"
}
Set-Location -LiteralPath $ApiPhysicalPath

# Domaine front reel appele par HelloAsso apres paiement.
# D'apres ton serveur actuel, tu utilises www.euroboost.top.
$FrontBaseUrl = "https://www.euroboost.top"

# App Pool IIS de l'API. Adapter si ton App Pool a un autre nom.
$ApiAppPoolName = "Api"

# Credentials HelloAsso sandbox.
$HelloAssoSandboxClientId = "50df99375ff34e2cba039cfdbba03d8a"
$HelloAssoSandboxClientSecret = "+1o1MyLUV+ichwjzwNUCM6zyQd7cJ306"

# Slug de l'association sandbox HelloAsso.
$HelloAssoSandboxOrganizationSlug = "acic-tests"

# Optionnel: seulement si HelloAsso sandbox te fournit/active une signature webhook.
$HelloAssoSandboxWebhookSignatureKey = ""

function Assert-Configured {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [AllowEmptyString()]
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value.Contains("__A_COMPLETER")) {
        throw "La variable $Name n'est pas renseignee. Remplace le placeholder avant execution."
    }
}

Assert-Configured "HelloAssoSandboxClientId" $HelloAssoSandboxClientId
Assert-Configured "HelloAssoSandboxClientSecret" $HelloAssoSandboxClientSecret
Assert-Configured "HelloAssoSandboxOrganizationSlug" $HelloAssoSandboxOrganizationSlug

$vars = [ordered]@{
    "Payments__PublicBaseUrl"                                      = $FrontBaseUrl
    "Payments__DefaultCurrency"                                    = "EUR"

    "Payments__HelloAsso__Enabled"                                 = "true"
    "Payments__HelloAsso__Environment"                             = "Sandbox"
    "Payments__HelloAsso__BaseUrl"                                 = "https://api.helloasso-sandbox.com"
    "Payments__HelloAsso__TokenBaseUrl"                            = "https://api.helloasso-sandbox.com"
    "Payments__HelloAsso__ApiBaseUrl"                              = "https://api.helloasso-sandbox.com"

    # Credentials globaux: renseignes aussi pour satisfaire la validation au demarrage.
    # L'alias organisme reste la source explicite pour ACIC.
    "Payments__HelloAsso__ClientId"                                = $HelloAssoSandboxClientId
    "Payments__HelloAsso__ClientSecret"                            = $HelloAssoSandboxClientSecret

    # Alias utilise par ACIC en base: HelloAssoCredentialKey = acic-sandbox.
    "Payments__HelloAsso__Credentials__acic-sandbox__ClientId"     = $HelloAssoSandboxClientId
    "Payments__HelloAsso__Credentials__acic-sandbox__ClientSecret" = $HelloAssoSandboxClientSecret
    "Payments__HelloAsso__Credentials__acic-sandbox__Environment"  = "Sandbox"
    "Payments__HelloAsso__Credentials__acic-sandbox__TokenBaseUrl" = "https://api.helloasso-sandbox.com"
    "Payments__HelloAsso__Credentials__acic-sandbox__ApiBaseUrl"   = "https://api.helloasso-sandbox.com"

    # Valeurs globales de confort, utilisees si l'organisme n'a pas de slug specifique.
    "Payments__HelloAsso__OrganizationSlug"                        = $HelloAssoSandboxOrganizationSlug
    "Payments__HelloAsso__WebhookSignatureKey"                     = $HelloAssoSandboxWebhookSignatureKey
    "Payments__HelloAsso__ItemName"                                = "Don à l'association"

    # URLs de retour: elles restent en production, meme si le checkout est sandbox.
    "Payments__HelloAsso__ReturnUrl"                               = "$FrontBaseUrl/my-space/donations/payment/helloasso/return"
    "Payments__HelloAsso__BackUrl"                                 = "$FrontBaseUrl/my-space"
    "Payments__HelloAsso__ErrorUrl"                                = "$FrontBaseUrl/my-space/donations/payment/helloasso/error"

    "Payments__HelloAsso__HttpTimeoutSeconds"                      = "20"
    "Payments__HelloAsso__RetryCount"                              = "3"

    "Payments__PayPal__Enabled"                                    = "false"
    "Payments__CardProvider__Enabled"                              = "false"
}

foreach ($entry in $vars.GetEnumerator()) {
    [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, "Machine")
    Set-Item -Path "Env:$($entry.Key)" -Value $entry.Value
    Write-Host "OK $($entry.Key)"
}

Write-Host ""
Write-Host "HelloAsso Sandbox est configure pour Life Production."
Write-Host "Callback a configurer dans HelloAsso sandbox:"
Write-Host "  https://api.euroboost.top/api/payments/webhooks/helloasso"
Write-Host ""
Write-Host "Verification SQL recommandee pour ACIC:"
Write-Host "  HelloAssoCredentialKey = acic-sandbox"
Write-Host "  HelloAssoOrganizationSlug = acic-tests"
Write-Host "  HelloAssoEnvironment = Sandbox"
Write-Host "  IsHelloAssoPaymentEnabled = 1"
Write-Host ""

try {
    Import-Module WebAdministration -ErrorAction Stop
    if (Test-Path "IIS:\AppPools\$ApiAppPoolName") {
        Restart-WebAppPool -Name $ApiAppPoolName
        Write-Host "App Pool redemarre: $ApiAppPoolName"
    }
    else {
        Write-Warning "App Pool introuvable: $ApiAppPoolName. Redemarre l'App Pool API ou lance iisreset."
    }
}
catch {
    Write-Warning "Module WebAdministration indisponible. Lance iisreset ou recycle l'App Pool API manuellement."
}
