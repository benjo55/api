param(
    [Parameter(Mandatory = $true)]
    [string]$Recipient,

    [string]$ApiBaseUrl = "http://localhost:5247"
)

$ErrorActionPreference = "Stop"

$uri = "$ApiBaseUrl/api/development/email/test"
$body = @{ recipient = $Recipient } | ConvertTo-Json -Depth 3

try {
    $response = Invoke-RestMethod -Method Post -Uri $uri -ContentType "application/json" -Body $body
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
