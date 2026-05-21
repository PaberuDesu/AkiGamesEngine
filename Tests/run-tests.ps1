$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$testProject = Join-Path $PSScriptRoot 'AkiGames.Tests\AkiGames.Tests.csproj'
$nugetConfig = Join-Path $PSScriptRoot 'NuGet.Config'
$assetsFile = Join-Path $PSScriptRoot 'AkiGames.Tests\obj\project.assets.json'

$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$userPackages = Join-Path $HOME '.nuget\packages'
$env:NUGET_PACKAGES = if (Test-Path $userPackages) {
    $userPackages
} else {
    Join-Path $env:DOTNET_CLI_HOME '.nuget\packages'
}

New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME | Out-Null

$needsRestore = -not (Test-Path $assetsFile)
if (-not $needsRestore) {
    $assetsText = Get-Content -Raw $assetsFile
    $needsRestore = $assetsText.Contains('\.dotnet-home\')
}

if ($needsRestore) {
    dotnet restore $testProject --configfile $nugetConfig -v:minimal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

dotnet run --project $testProject --no-restore -- $args
exit $LASTEXITCODE
