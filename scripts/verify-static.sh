#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

required=(README.md architecture.md runtime-validation.md research-sources.md SpireWatch.csproj SpireWatch.json src/ModEntry.cs src/Patches/LobbyLifecycleSafetyPatches.cs src/Patches/SpectatorFriendListPatch.cs src/Patches/SpectatorPatches.cs src/Spectating/RunActionJournal.cs src/Spectating/SpectatorProtocol.cs src/Spectating/SpectatorJoinSafety.cs)
for path in "${required[@]}"; do
  [[ -f "$path" ]] || { echo "Missing required file: $path" >&2; exit 1; }
done

if rg -n -i -w 'HttpListener|HttpServer|WebSocket|TcpListener|localhost|127\.0\.0\.1' src; then
  echo "Forbidden external transport found in src/." >&2
  exit 1
fi

rg -q '"id": "SpireWatch"' SpireWatch.json
rg -q 'SteamMatchmaking' src/Networking/SteamLobbyMetadataPublisher.cs
rg -q 'StartSteamHost' src/Patches/HostLobbyPatches.cs
rg -q 'BeginRunLocally' src/Patches/RunningLobbyLifecyclePatch.cs
rg -q 'CleanUp' src/Patches/LobbyLifecycleSafetyPatches.cs
rg -q 'SpectatorChallengeMessage' src/Spectating/SpectatorProtocol.cs
rg -q 'TryAuthorizeHostPeer' src/Patches/SpectatorPatches.cs
rg -q 'IsSafeHostJoinPoint' src/Patches/SpectatorPatches.cs
rg -q 'SpectatorActionReplayReadyMessage' src/Spectating/RunActionJournal.cs
rg -q 'ActionEnqueuedMessage' src/Spectating/RunActionJournal.cs
if rg -n 'SpectatorJoinPanel|SpectatorBootstrap|spirewatch-spectate' src; then
  echo "Forbidden standalone spectator entry point found in src/." >&2
  exit 1
fi
echo "Static spectator checks passed."
