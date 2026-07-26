[CmdletBinding()]
param(
    [switch]$Full
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
Set-Location -LiteralPath $PSScriptRoot

$verifyArgs = @("scripts/harness/verify.mjs")
if ($Full) {
    $verifyArgs += "--full"
}

& node @verifyArgs
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Harness baseline is healthy."
Write-Host "Read feature_list.json and select at most one in-progress feature."
