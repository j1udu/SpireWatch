$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$required = @(
    'README.md',
    'architecture.md',
    'runtime-validation.md',
    'research-sources.md',
    'SpireWatch.csproj',
    'SpireWatch.json',
    'src/ModEntry.cs',
    'src/Patches/LobbyLifecycleSafetyPatches.cs',
    'src/Patches/SpectatorFriendListPatch.cs',
    'src/Patches/SpectatorPatches.cs',
    'src/Patches/SpectatorReadOnlyInputPatches.cs',
    'src/Spectating/SpectatorJoinFlow.cs',
    'src/Spectating/SpectatorProtocol.cs',
    'src/Spectating/SpectatorJoinSafety.cs'
)

foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing required file: $path"
    }
}

if (rg -n -i 'HttpListener|HttpServer|WebSocket|TcpListener|localhost|127\.0\.0\.1' src) {
    throw 'Forbidden external transport found in src/.'
}

if (-not (Select-String -Path 'SpireWatch.json' -Pattern '"id": "SpireWatch"' -Quiet)) {
    throw 'SpireWatch.json does not declare the expected Mod ID.'
}

if (-not (Select-String -Path 'src/Networking/SteamLobbyMetadataPublisher.cs' -Pattern 'SteamMatchmaking' -Quiet)) {
    throw 'SteamMatchmaking metadata publisher is missing.'
}

if (-not (Select-String -Path 'src/Patches/HostLobbyPatches.cs' -Pattern 'StartSteamHost' -Quiet)) {
    throw 'Steam host lifecycle patch is missing.'
}

if (-not (Select-String -Path 'src/Patches/RunningLobbyLifecyclePatch.cs' -Pattern 'BeginRunLocally' -Quiet)) {
    throw 'Running lobby lifecycle patch is missing.'
}

if (-not (Select-String -Path 'src/Patches/LobbyLifecycleSafetyPatches.cs' -Pattern 'CleanUp' -Quiet)) {
    throw 'Running lobby cleanup patch is missing.'
}

if (-not (Select-String -Path 'src/Patches/SpectatorPatches.cs' -Pattern 'TryAuthorizeHostPeer' -Quiet)) {
    throw 'Host-side spectator protocol gate is missing.'
}

if (-not (Select-String -Path 'src/Patches/SpectatorPatches.cs' -Pattern 'IsSafeHostJoinPoint' -Quiet)) {
    throw 'Host-side spectator safety-point gate is missing.'
}

if (rg -n 'SpectatorJoinPanel|SpectatorBootstrap|spirewatch-spectate' src) {
    throw 'Forbidden standalone spectator entry point found in src/.'
}

Write-Output 'Static spectator checks passed.'
