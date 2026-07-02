#!/usr/bin/env pwsh
# Build a portable, offline-capable publish folder for MemoryMCP.
# Run on a machine with internet (restore + publish). Copy the output folder to
# an offline Linux Docker (or Windows) host and run:
#   dotnet MemoryMCP.dll --typ sqlite
#
# linux-x64 defaults to net8.0 (for dotnet/sdk:8.0 containers).
# win-x64 defaults to net10.0 (local dev on Windows).

param(
    [string[]]$Runtime = @("linux-x64", "win-x64"),
    [string]$LinuxFramework = "net8.0",
    [string]$WindowsFramework = "net10.0"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot

function Get-FrameworkForRuntime {
    param([string]$Rid)
    switch ($Rid) {
        "linux-x64" { return $LinuxFramework }
        "win-x64" { return $WindowsFramework }
        default { throw "Unknown runtime '$Rid'. Supported: linux-x64, win-x64." }
    }
}

Write-Host "Restoring packages..." -ForegroundColor Cyan
dotnet restore MemoryMCP.csproj
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

foreach ($rid in $Runtime) {
    $framework = Get-FrameworkForRuntime -Rid $rid
    $outputDir = Join-Path $repoRoot "dist\$rid"
    Write-Host "`nPublishing $framework for $rid -> $outputDir" -ForegroundColor Cyan

    dotnet publish MemoryMCP.csproj `
        -f $framework `
        -c Release `
        -r $rid `
        --self-contained false `
        -o $outputDir

    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if ($rid -eq "win-x64" -and $IsWindows) {
        Write-Host "Verifying $rid build..." -ForegroundColor DarkGray
        dotnet exec (Join-Path $outputDir "MemoryMCP.dll") -- --typ sqlite --list-tools
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    else {
        Write-Host "Skipping local verify for $rid (cross-platform publish)." -ForegroundColor DarkGray
    }
}

Write-Host "`nPortable publish complete." -ForegroundColor Green
Write-Host "Copy dist/<runtime>/ to the target host and run:" -ForegroundColor Green
Write-Host "  dotnet MemoryMCP.dll --typ sqlite" -ForegroundColor Yellow
Write-Host "`nRuntime requirements:" -ForegroundColor Green
Write-Host "  dist/linux-x64  -> .NET 8 runtime (e.g. mcr.microsoft.com/dotnet/sdk:8.0)" -ForegroundColor Yellow
Write-Host "  dist/win-x64    -> .NET 10 runtime" -ForegroundColor Yellow
