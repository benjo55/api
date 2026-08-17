param(
    [Parameter(Mandatory = $true)]
    [string]$Recipient,

    [string]$ApiBaseUrl = "http://localhost:5247",

    [string]$BearerToken = "",

    [switch]$Admin
)

$ErrorActionPreference = "Stop"

$path = if ($Admin) { "api/admin/mail/test" } else { "api/development/email/test" }
$uri = "$($ApiBaseUrl.TrimEnd('/'))/$path"
$body = @{ recipient = $Recipient } | ConvertTo-Json -Depth 3
$headers = @{}

if ($Admin) {
    if ([string]::IsNullOrWhiteSpace($BearerToken)) {
        throw "Le mode -Admin requiert -BearerToken avec un JWT administrateur."
    }

    $headers["Authorization"] = "Bearer $BearerToken"
}

try {
    $response = Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -ContentType "application/json" -Body $body
    Write-Host "Réponse API :"
    $response | ConvertTo-Json -Depth 5
}
catch {
    $errorResponse = $_.Exception.Response
    if ($null -ne $errorResponse) {
        $reader = New-Object System.IO.StreamReader($errorResponse.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        $reader.Close()

        Write-Error "L'appel a échoué sur $uri"
        if ($errorBody) {
            Write-Error $errorBody
        }
    }
    else {
        throw
    }
}
