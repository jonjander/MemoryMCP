#!/usr/bin/env pwsh
# Rebuild MemoryMCP for Cursor MCP.
# Stops any running MemoryMCP host (typically dotnet exec bin/mcp/MemoryMCP.dll) before build.
# After build: restart memorymcp in Cursor Settings > MCP if it does not reconnect automatically.

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot

$mcpDll = Join-Path $repoRoot "bin\mcp\MemoryMCP.dll"

function Get-MemoryMcpHostProcesses {
    $dllPattern = [regex]::Escape("MemoryMCP.dll")
    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object {
            $cmd = $_.CommandLine
            $cmd -and $cmd -match $dllPattern
        }
}

function Stop-MemoryMcpHostProcesses {
    $processes = @(Get-MemoryMcpHostProcesses)
    if ($processes.Count -eq 0) {
        Write-Host "No running MemoryMCP host found." -ForegroundColor DarkGray
        return
    }

    foreach ($proc in $processes) {
        $label = if ($proc.CommandLine.Length -gt 120) { $proc.CommandLine.Substring(0, 120) + "..." } else { $proc.CommandLine }
        Write-Host "Stopping PID $($proc.ProcessId) ($($proc.Name)): $label" -ForegroundColor Yellow
        Stop-Process -Id $proc.ProcessId -Force -ErrorAction Stop
    }
}

function Wait-FileUnlocked {
    param(
        [string]$Path,
        [int]$TimeoutSeconds = 15
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return $true
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            $stream.Close()
            $stream.Dispose()
            return $true
        }
        catch [System.IO.IOException] {
            Start-Sleep -Milliseconds 250
        }
    }

    return $false
}

Stop-MemoryMcpHostProcesses

if (-not (Wait-FileUnlocked -Path $mcpDll)) {
    $stillRunning = @(Get-MemoryMcpHostProcesses)
    if ($stillRunning.Count -gt 0) {
        Write-Error "MemoryMCP.dll is still locked after stopping host processes. Disable/restart memorymcp in Cursor Settings > MCP and run again."
    }
    Write-Error "MemoryMCP.dll is still locked: $mcpDll"
}

dotnet build MemoryMCP.csproj -o bin/mcp
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`nBuilt to bin/mcp. Tool list:" -ForegroundColor Green
dotnet exec bin/mcp/MemoryMCP.dll -- --list-tools
