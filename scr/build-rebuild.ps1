[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

Push-Location (Resolve-Path "$PSScriptRoot\..")
try {
    dotnet build KoreForge.AppLifecycle.slnx --force -c $Configuration
} finally {
    Pop-Location
}