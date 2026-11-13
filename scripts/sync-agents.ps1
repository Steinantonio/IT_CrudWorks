# Syncs the IT CrudWorks agent definitions into the OpenCode project config.
# OpenCode discovers agents under <workspace>/.opencode/agents/<namespace>/.
# The canonical files stay in IT_CrudWorks/agents; this copy is generated.
#
# Usage: powershell -File scripts/sync-agents.ps1
# Re-run after editing any agent file.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot          # IT_CrudWorks
$source = Join-Path $root 'agents'
$workspace = Split-Path -Parent $root             # C:\Trabalho
$target = Join-Path $workspace '.opencode\agents\it_crudworks'

$agents = @(
    'orchestrator',
    'architect',
    'backend-implementer',
    'frontend-implementer',
    'reviewer',
    'qa-engineer'
)

if (-not (Test-Path $target)) {
    New-Item -ItemType Directory -Path $target -Force | Out-Null
}

foreach ($name in $agents) {
    $file = Join-Path $source "$name.md"
    if (-not (Test-Path $file)) {
        throw "Missing agent file: $file"
    }
    Copy-Item -LiteralPath $file -Destination (Join-Path $target "$name.md") -Force
}

Write-Output "Synced $($agents.Count) agents to $target"
Get-ChildItem $target -File | Select-Object Name, Length | Format-Table -AutoSize
