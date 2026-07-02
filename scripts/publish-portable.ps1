#!/usr/bin/env pwsh
# Build a portable, offline-capable publish folder for MemoryMCP.
# Run on a machine with internet (restore + publish). Copy the output folder to
# an offline Linux Docker (or Windows) host and run:
#   dotnet MemoryMCP.dll --typ sqlite

param(
    [string[]]$Runtime = @("linux-x64", "win-x64")
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot

Write-Host "Restoring packages..." -ForegroundColor Cyan
dotnet restore MemoryMCP.csproj
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

foreach ($rid in $Runtime) {
    $outputDir = Join-Path $repoRoot "dist\$rid"
    Write-Host "`nPublishing for $rid -> $outputDir" -ForegroundColor Cyan

    dotnet publish MemoryMCP.csproj `
        -c Release `
        -r $rid `
        --self-contained false `
        -o $outputDir

    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Verifying $rid build..." -ForegroundColor DarkGray
    dotnet exec (Join-Path $outputDir "MemoryMCP.dll") -- --typ sqlite --list-tools
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "`nPortable publish complete." -ForegroundColor Green
Write-Host "Copy dist/<runtime>/ to the target host and run:" -ForegroundColor Green
Write-Host "  dotnet MemoryMCP.dll --typ sqlite" -ForegroundColor Yellow
