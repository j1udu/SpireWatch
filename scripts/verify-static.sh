#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

required=(README.md architecture.md runtime-validation.md research-sources.md SpireWatch.csproj SpireWatch.json src/ModEntry.cs)
for path in "${required[@]}"; do
  [[ -f "$path" ]] || { echo "Missing required file: $path" >&2; exit 1; }
done

if rg -n -i 'HttpListener|HttpServer|WebSocket|TcpListener|localhost|127\.0\.0\.1' src; then
  echo "Forbidden external transport found in src/." >&2
  exit 1
fi

rg -q '"id": "SpireWatch"' SpireWatch.json
rg -q 'SteamMatchmaking' src/Networking/SteamLobbyMetadataPublisher.cs
rg -q 'StartSteamHost' src/Patches/HostLobbyPatches.cs
rg -q 'BeginRunLocally' src/Patches/RunningLobbyLifecyclePatch.cs
echo "Static Phase 0/1 checks passed."
