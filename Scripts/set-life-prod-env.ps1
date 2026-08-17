#Requires -RunAsAdministrator

<#
  Script de parametrage des variables d'environnement Production pour Life.

  Usage:
  1. Copier ce fichier sur le serveur Windows 2019.
  2. Remplacer toutes les valeurs __A_COMPLETER__.
  3. Executer PowerShell en administrateur.
  4. Lancer:
       powershell -ExecutionPolicy Bypass -File .\set-life-prod-env.ps1

  Les variables sont posees au niveau Machine pour remplacer dotnet user-secrets.
  Ne committez jamais ce fichier une fois renseigne avec de vraies valeurs.
#>

$ErrorActionPreference = "Stop"

$ApiPhysicalPath = "C:\inetpub\wwwroot\api"
if (-not (Test-Path -LiteralPath $ApiPhysicalPath)) {
    throw "Le repertoire API IIS est introuvable: $ApiPhysicalPath"
}
Set-Location -LiteralPath $ApiPhysicalPath

# Domaines canoniques front.
# L'assurance reste sur euroboost.top, les dons et retours HelloAsso sur cerfa.top.
$InsuranceFrontBaseUrl = "https://www.euroboost.top"
$DonationFrontBaseUrl = "https://cerfa.top"
$UrbanizationFrontBaseUrl = "https://urbanisation.world"
$ApiBaseUrl = "https://api.euroboost.top"

# App Pool IIS de l'API. Adapter au nom reel dans IIS.
$ApiAppPoolName = "Api"

# SQL Server production.
# En PowerShell, l'instance SQL s'ecrit avec un seul antislash: WIN-G01GKH465QH\SQLEXPRESS.
$SqlServerInstance = "WIN-G01GKH465QH\SQLEXPRESS"
$SqlDatabase = "Life"
$SqlUser = "api"
$SqlPassword = "Pipouce2020!!"
$SqlConnectionString = "Data Source=$SqlServerInstance;Initial Catalog=$SqlDatabase;Integrated Security=False;User ID=$SqlUser;Password=$SqlPassword;Encrypt=True;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"

# JWT: valeur longue, aleatoire, 32 caracteres minimum, idealement 64+.
$JwtKey = "MaSuperCleSecreteTresLongue123456789"

# Brevo SMTP. Utiliser le login SMTP Brevo et une cle SMTP, pas une cle API.
$BrevoSmtpLogin = "b5c207001@smtp-brevo.com"
$BrevoSmtpKey = "__A_COMPLETER_BREVO_SMTP_KEY__"

# EOD Historical Data.
$EodApiKey = "684f34554390c3.19045690"

# INSEE Sirene / geo.
$InseeApiKey = "84ae371e-fb23-47f3-ae37-1efb2357f36f"

# HelloAsso Production.
$HelloAssoClientId = "0a0a08ba5b0a46b2874a8ad753afe987"
$HelloAssoClientSecret = "+pcW0wwbZW/wdGT2tZyHG0xXOnTXVwKv"

# Optionnel: seulement si HelloAsso vous fournit/active une signature webhook.
$HelloAssoWebhookSignatureKey = ""

# Virements desactives pour l'instant. Garder une valeur forte si vous les activez plus tard.
$BankEncryptionKey = "__A_COMPLETER_CLE_CHIFFREMENT_BANQUE_OU_LAISSER_INUTILISEE__"

function Assert-Configured {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [AllowEmptyString()]
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value.Contains("__A_COMPLETER")) {
        throw "La variable $Name n'est pas renseignee. Remplacez la valeur placeholder avant execution."
    }
}

Assert-Configured "SqlConnectionString" $SqlConnectionString
Assert-Configured "SqlPassword" $SqlPassword
Assert-Configured "JwtKey" $JwtKey
Assert-Configured "BrevoSmtpLogin" $BrevoSmtpLogin
Assert-Configured "BrevoSmtpKey" $BrevoSmtpKey
Assert-Configured "EodApiKey" $EodApiKey
Assert-Configured "InseeApiKey" $InseeApiKey
Assert-Configured "HelloAssoClientId" $HelloAssoClientId
Assert-Configured "HelloAssoClientSecret" $HelloAssoClientSecret

$vars = [ordered]@{
    "ASPNETCORE_ENVIRONMENT"                                          = "Production"

    "ConnectionStrings__DefaultConnection"                            = $SqlConnectionString

    "Jwt__Key"                                                        = $JwtKey
    "Jwt__Issuer"                                                     = $ApiBaseUrl
    "Jwt__Audience"                                                   = $InsuranceFrontBaseUrl
    "Jwt__DurationInMinutes"                                          = "60"

    "AllowedHosts"                                                    = "api.euroboost.top"

    "PublicOrigins__DefaultExperience"                                = "Insurance"
    "PublicOrigins__UnknownHostPolicy"                                = "Reject"
    "PublicOrigins__Experiences__Insurance__Origin"                   = $InsuranceFrontBaseUrl
    "PublicOrigins__Experiences__Insurance__Domains__0"               = "euroboost.top"
    "PublicOrigins__Experiences__Insurance__Domains__1"               = "www.euroboost.top"
    "PublicOrigins__Experiences__Insurance__Domains__2"               = "api.euroboost.top"
    "PublicOrigins__Experiences__Donation__Origin"                    = $DonationFrontBaseUrl
    "PublicOrigins__Experiences__Donation__Domains__0"                = "cerfa.top"
    "PublicOrigins__Experiences__Donation__Domains__1"                = "www.cerfa.top"
    "PublicOrigins__Experiences__Urbanization__Origin"                = $UrbanizationFrontBaseUrl
    "PublicOrigins__Experiences__Urbanization__Domains__0"            = "urbanisation.world"
    "PublicOrigins__Experiences__Urbanization__Domains__1"            = "www.urbanisation.world"

    "Authentication__FrontendBaseUrl"                                 = $InsuranceFrontBaseUrl
    "Authentication__EmailConfirmationTokenLifetime"                  = "1.00:00:00"
    "Authentication__PasswordResetTokenLifetime"                      = "00:30:00"
    "Authentication__MinimumEmailResendInterval"                      = "00:02:00"
    "Authentication__PasswordMinLength"                               = "10"

    "MailSettings__Provider"                                          = "Brevo"
    "MailSettings__Host"                                              = "smtp-relay.brevo.com"
    "MailSettings__Port"                                              = "587"
    "MailSettings__EnableSsl"                                         = "true"
    "MailSettings__UserName"                                          = $BrevoSmtpLogin
    "MailSettings__Password"                                          = $BrevoSmtpKey
    "MailSettings__FromAddress"                                       = "no-reply@euroboost.top"
    "MailSettings__FromName"                                          = "Financial Life"

    "Eod__ApiKey"                                                     = $EodApiKey
    "EodSettings__OnlyStrategyMode"                                   = "true"

    "Insee__ApiKey"                                                   = $InseeApiKey
    "Insee__BaseUrl"                                                  = "https://api.insee.fr/api-sirene/3.11/"
    "Insee__GeoBaseUrl"                                               = "https://api.insee.fr/metadonnees/V1/geo/"
    "Insee__TimeoutSeconds"                                           = "10"
    "Insee__CacheDurationMinutes"                                     = "720"
    "Insee__MaxSearchResults"                                         = "10"

    "ExternalFeeds__News__Provider"                                   = "Rss"
    "ExternalFeeds__News__CacheDurationMinutes"                       = "15"
    "ExternalFeeds__News__DefaultLimit"                               = "6"
    "ExternalFeeds__News__MaxLimit"                                   = "12"
    "ExternalFeeds__FinancialMarkets__Provider"                       = "Eod"
    "ExternalFeeds__FinancialMarkets__CacheDurationMinutes"           = "5"

    "DonationCheckout__MinAmountEur"                                  = "1.00"
    "DonationCheckout__MaxAmountEur"                                  = "10000.00"
    "DonationCheckout__StatusPollingMaxSeconds"                       = "120"
    "DonationCheckout__ReceiptTokenLifetimeMinutes"                   = "15"

    "Payments__PublicBaseUrl"                                         = $DonationFrontBaseUrl
    "Payments__DefaultCurrency"                                       = "EUR"
    "Payments__BankTransfersEnabled"                                  = "false"
    "Payments__BankEncryptionKey"                                     = $BankEncryptionKey

    "Payments__HelloAsso__Enabled"                                    = "true"
    "Payments__HelloAsso__Environment"                                = "Production"
    "Payments__HelloAsso__BaseUrl"                                    = "https://api.helloasso.com"
    "Payments__HelloAsso__TokenBaseUrl"                               = "https://api.helloasso.com"
    "Payments__HelloAsso__ApiBaseUrl"                                 = "https://api.helloasso.com"
    "Payments__HelloAsso__ClientId"                                   = $HelloAssoClientId
    "Payments__HelloAsso__ClientSecret"                               = $HelloAssoClientSecret
    "Payments__HelloAsso__OrganizationSlug"                           = ""
    "Payments__HelloAsso__WebhookSignatureKey"                        = $HelloAssoWebhookSignatureKey
    "Payments__HelloAsso__ItemName"                                   = "Don a l'association"
    "Payments__HelloAsso__ReturnUrl"                                  = "$DonationFrontBaseUrl/donate/return"
    "Payments__HelloAsso__BackUrl"                                    = "$DonationFrontBaseUrl/donate"
    "Payments__HelloAsso__ErrorUrl"                                   = "$DonationFrontBaseUrl/donate/error"
    "Payments__HelloAsso__HttpTimeoutSeconds"                         = "20"
    "Payments__HelloAsso__RetryCount"                                 = "3"

    "Payments__HelloAsso__Credentials__acic-production__ClientId"     = $HelloAssoClientId
    "Payments__HelloAsso__Credentials__acic-production__ClientSecret" = $HelloAssoClientSecret
    "Payments__HelloAsso__Credentials__acic-production__Environment"  = "Production"
    "Payments__HelloAsso__Credentials__acic-production__TokenBaseUrl" = "https://api.helloasso.com"
    "Payments__HelloAsso__Credentials__acic-production__ApiBaseUrl"   = "https://api.helloasso.com"

    "Payments__PayPal__Enabled"                                       = "false"
    "Payments__PayPal__Environment"                                   = "Production"
    "Payments__PayPal__ClientId"                                      = ""
    "Payments__PayPal__ClientSecret"                                  = ""
    "Payments__PayPal__WebhookId"                                     = ""

    "Payments__CardProvider__Enabled"                                 = "false"
    "Payments__CardProvider__ProviderName"                            = ""
    "Payments__CardProvider__PublicKeyAlias"                          = ""
    "Payments__CardProvider__SecretKeyAlias"                          = ""

    "Quartz__quartz.serializer.type"                                  = "json"
    "Quartz__quartz.scheduler.instanceName"                           = "LifeProductionScheduler"
    "Quartz__quartz.scheduler.instanceId"                             = "AUTO"
    "Quartz__quartz.jobStore.type"                                    = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz"
    "Quartz__quartz.jobStore.useProperties"                           = "true"
    "Quartz__quartz.jobStore.dataSource"                              = "default"
    "Quartz__quartz.jobStore.tablePrefix"                             = "QRTZ_"
    "Quartz__quartz.dataSource.default.provider"                      = "SqlServer"
    "Quartz__quartz.dataSource.default.connectionString"              = $SqlConnectionString
    "Quartz__quartz.threadPool.type"                                  = "Quartz.Simpl.SimpleThreadPool, Quartz"
    "Quartz__quartz.threadPool.threadCount"                           = "5"
    "Quartz__quartz.threadPool.threadPriority"                        = "Normal"
}

foreach ($entry in $vars.GetEnumerator()) {
    [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, "Machine")
    Set-Item -Path "Env:$($entry.Key)" -Value $entry.Value
    Write-Host "OK $($entry.Key)"
}

Write-Host ""
Write-Host "Variables Life Production posees au niveau Machine."
Write-Host "Callback HelloAsso a configurer dans l'espace HelloAsso:"
Write-Host "  $ApiBaseUrl/api/payments/webhooks/helloasso"
Write-Host ""

try {
    Import-Module WebAdministration -ErrorAction Stop
    if (Test-Path "IIS:\AppPools\$ApiAppPoolName") {
        Restart-WebAppPool -Name $ApiAppPoolName
        Write-Host "App Pool redemarre: $ApiAppPoolName"
    }
    else {
        Write-Warning "App Pool introuvable: $ApiAppPoolName. Redemarrez l'App Pool API ou lancez iisreset."
    }
}
catch {
    Write-Warning "Module WebAdministration indisponible. Lancez iisreset ou recyclez l'App Pool API manuellement."
}
