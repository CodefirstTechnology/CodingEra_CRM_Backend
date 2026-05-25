# Runs after Debug build: creates a migration when the EF model changed, then updates PostgreSQL.
param(
    [switch]$SkipAddMigration
)

$ErrorActionPreference = "Continue"
$projectDir = Split-Path -Parent $PSScriptRoot
Set-Location $projectDir

if ($env:SkipEfMigrate -eq "true") {
    Write-Host "[EF] Skipped (SkipEfMigrate=true)."
    exit 0
}

Write-Host "[EF] Checking for model changes since last migration..."
dotnet ef migrations has-pending-model-changes 2>&1 | Out-Host
$hasModelChanges = $LASTEXITCODE -ne 0

if ($hasModelChanges -and -not $SkipAddMigration) {
    $name = "Auto_" + (Get-Date -Format "yyyyMMdd_HHmmss")
    Write-Host "[EF] Model changed — adding migration '$name'..."
    dotnet ef migrations add $name 2>&1 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "[EF] Could not add migration (is PostgreSQL running?)."
        exit 0
    }
    Write-Host "[EF] Rebuilding after new migration..."
    dotnet build -v q 2>&1 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "[EF] Rebuild failed after adding migration."
        exit 0
    }
}

Write-Host "[EF] Applying pending migrations to database..."
dotnet ef database update 2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) {
    Write-Warning "[EF] database update failed (is PostgreSQL running?). Migrations will run on next API startup."
    exit 0
}

Write-Host "[EF] Database is up to date."
exit 0
