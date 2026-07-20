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
    'src/Spectating/ReadOnlySpectatorNetGameService.cs',
    'src/Spectating/SpectatorJoinFlow.cs',
    'src/Spectating/SteamFriendLobbyMetadata.cs',
    'src/Patches/SpectatorPatches.cs',
    'src/Patches/SpectatorReadOnlyInputPatches.cs',
    'src/Patches/SpectatorFriendListPatch.cs',
    'src/Patches/SpectatorSavePatch.cs'
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

if (-not (Select-String -Path 'src/Patches/SpectatorFriendListPatch.cs' -Pattern 'ShowFriends' -Quiet)) {
    throw 'Vanilla friend-list spectator patch is missing.'
}

if (-not (Select-String -Path 'src/Patches/HostLobbyPatches.cs' -Pattern 'StartSteamHost' -Quiet)) {
    throw 'Steam host lifecycle patch is missing.'
}

if (-not (Select-String -Path 'src/Patches/RunningLobbyLifecyclePatch.cs' -Pattern 'BeginRunLocally' -Quiet)) {
    throw 'Running lobby lifecycle patch is missing.'
}

if (-not (Select-String -Path 'src/Patches/SpectatorReadOnlyInputPatches.cs' -Pattern 'RequestEnqueue' -Quiet)) {
    throw 'Spectator action-input patch is missing.'
}

Write-Output 'Static host and spectator checks passed.'
