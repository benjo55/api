param(
    [string]$ApiProject = "C:\Life\api\api.csproj",
    [string]$FrontendBaseUrl = "http://localhost:5173",
    [string]$PaymentsPublicBaseUrl = $FrontendBaseUrl,
    [string]$MailjetApiKey = "",
    [string]$MailjetSecretKey = "",
    [string]$EodApiKey = "",
    [string]$HelloAssoClientId = "",
    [string]$HelloAssoClientSecret = "",
    [string]$HelloAssoOrganizationSlug = "",
    [string]$HelloAssoWebhookSignatureKey = "",
    [switch]$SkipEmptySecrets
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ApiProject)) {
    throw "Projet API introuvable: $ApiProject"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet est introuvable. Installe le SDK .NET ou corrige le PATH."
}

function Set-UserSecret {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [AllowEmptyString()]
        [string]$Value
    )

    if ($SkipEmptySecrets -and [string]::IsNullOrWhiteSpace($Value)) {
        Write-Host "SKIP $Name"
        return
    }

    dotnet user-secrets set $Name $Value --project $ApiProject | Write-Host
}

Set-UserSecret "Authentication:FrontendBaseUrl" $FrontendBaseUrl
Set-UserSecret "Authentication:EmailConfirmationTokenLifetime" "1.00:00:00"
Set-UserSecret "Authentication:PasswordResetTokenLifetime" "00:30:00"
Set-UserSecret "Authentication:MinimumEmailResendInterval" "00:02:00"
Set-UserSecret "Authentication:PasswordMinLength" "10"

Set-UserSecret "MailSettings:Host" "in-v3.mailjet.com"
Set-UserSecret "MailSettings:Port" "587"
Set-UserSecret "MailSettings:EnableSsl" "true"
Set-UserSecret "MailSettings:UserName" $MailjetApiKey
Set-UserSecret "MailSettings:Password" $MailjetSecretKey
Set-UserSecret "MailSettings:FromAddress" "no-reply@euroboost.top"
Set-UserSecret "MailSettings:FromName" "Life"

Set-UserSecret "Eod:ApiKey" $EodApiKey

Set-UserSecret "Payments:PublicBaseUrl" $PaymentsPublicBaseUrl
Set-UserSecret "Payments:HelloAsso:Enabled" "true"
Set-UserSecret "Payments:HelloAsso:Environment" "Sandbox"
Set-UserSecret "Payments:HelloAsso:BaseUrl" "https://api.helloasso-sandbox.com"
Set-UserSecret "Payments:HelloAsso:TokenBaseUrl" "https://api.helloasso-sandbox.com"
Set-UserSecret "Payments:HelloAsso:ApiBaseUrl" "https://api.helloasso-sandbox.com"
Set-UserSecret "Payments:HelloAsso:ClientId" $HelloAssoClientId
Set-UserSecret "Payments:HelloAsso:ClientSecret" $HelloAssoClientSecret
Set-UserSecret "Payments:HelloAsso:OrganizationSlug" $HelloAssoOrganizationSlug
Set-UserSecret "Payments:HelloAsso:WebhookSignatureKey" $HelloAssoWebhookSignatureKey
Set-UserSecret "Payments:HelloAsso:Credentials:acic-sandbox:ClientId" $HelloAssoClientId
Set-UserSecret "Payments:HelloAsso:Credentials:acic-sandbox:ClientSecret" $HelloAssoClientSecret
Set-UserSecret "Payments:HelloAsso:Credentials:acic-sandbox:Environment" "Sandbox"
Set-UserSecret "Payments:HelloAsso:Credentials:acic-sandbox:TokenBaseUrl" "https://api.helloasso-sandbox.com"
Set-UserSecret "Payments:HelloAsso:Credentials:acic-sandbox:ApiBaseUrl" "https://api.helloasso-sandbox.com"

Write-Host ""
Write-Host "Secrets Life Development mis a jour pour $ApiProject"
Write-Host "Redemarre l'API pour relire les user-secrets."
