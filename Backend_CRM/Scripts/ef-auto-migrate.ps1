# Runs after Debug build: creates a migration when the EF model changed, then updates PostgreSQL without triggering child MSBuild loops.
param(
    [switch]$SkipAddMigration
)

$ErrorActionPreference = "Continue"

# Guard against child build recursion
$env:EFAutoMigrateRunning = "true"
$env:SkipEfMigrate = "true"

$projectDir = Split-Path -Parent $PSScriptRoot
Set-Location $projectDir

Write-Host "[EF] Checking for model changes since last migration..."
dotnet ef migrations has-pending-model-changes --no-build 2>&1 | Out-Host
$hasModelChanges = $LASTEXITCODE -ne 0

if ($hasModelChanges -and -not $SkipAddMigration) {
    $name = "Auto_" + (Get-Date -Format "yyyyMMdd_HHmmss")
    Write-Host "[EF] Model changed - adding migration '$name'..."
    dotnet ef migrations add $name --no-build 2>&1 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "[EF] Could not add migration (is PostgreSQL running?). Migration will run on API startup."
        exit 0
    }
}

Write-Host "[EF] Applying pending migrations to database..."
dotnet ef database update --no-build 2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) {
    Write-Warning "[EF] database update failed (is PostgreSQL running?). Migrations will run on next API startup."
    exit 0
}

Write-Host "[EF] Database is up to date."
exit 0
