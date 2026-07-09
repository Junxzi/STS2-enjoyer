param(
    [string]$Sts2InstallPath = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2',
    [string]$Configuration = 'Debug',
    [switch]$LaunchSteam,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$partyRaceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$modProject = Join-Path $partyRaceRoot 'src\PartyRace.Mod\PartyRace.Mod.csproj'
$manifestSource = Join-Path $partyRaceRoot 'src\PartyRace.Mod\mod_manifest.json'
$modOutput = Join-Path $partyRaceRoot "src\PartyRace.Mod\bin\$Configuration\net9.0"
$modDestination = Join-Path $Sts2InstallPath 'mods\party_race'

if (-not (Test-Path $Sts2InstallPath)) {
    throw "STS2 install path was not found: $Sts2InstallPath"
}

if (-not $SkipBuild) {
    $sts2InstallProperty = "/p:Sts2InstallPath=$Sts2InstallPath"
    dotnet build $modProject --configuration $Configuration $sts2InstallProperty
}

if (-not (Test-Path $modOutput)) {
    throw "Mod build output was not found: $modOutput"
}

New-Item -ItemType Directory -Force -Path $modDestination | Out-Null

Copy-Item -Force -Path $manifestSource -Destination (Join-Path $modDestination 'mod_manifest.json')
Copy-Item -Force -Path (Join-Path $modOutput 'PartyRace.Mod.dll') -Destination (Join-Path $modDestination 'party_race.dll')

$modPdb = Join-Path $modOutput 'PartyRace.Mod.pdb'
if (Test-Path $modPdb) {
    Copy-Item -Force -Path $modPdb -Destination (Join-Path $modDestination 'party_race.pdb')
}

$requiredDependencies = @(
    'PartyRace.Core.dll',
    'PartyRace.Sts2Adapter.dll'
)

foreach ($dependency in $requiredDependencies) {
    $dependencyPath = Join-Path $modOutput $dependency
    if (-not (Test-Path $dependencyPath)) {
        throw "Required dependency was not found in mod output: $dependencyPath"
    }

    Copy-Item -Force -Path $dependencyPath -Destination (Join-Path $modDestination $dependency)

    $dependencyPdb = [System.IO.Path]::ChangeExtension($dependencyPath, '.pdb')
    if (Test-Path $dependencyPdb) {
        Copy-Item -Force -Path $dependencyPdb -Destination (Join-Path $modDestination ([System.IO.Path]::GetFileName($dependencyPdb)))
    }
}

Write-Host "Deployed Party Race to $modDestination"
Write-Host 'Check after launch: Test-Path "$env:LOCALAPPDATA\PartyRace\party_race_mod_loaded.log"'

if ($LaunchSteam) {
    Start-Process 'steam://rungameid/2868840'
}
