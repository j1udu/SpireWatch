# SpireWatch

SpireWatch is a Slay the Spire 2 multiplayer mod that lets compatible players discover an already-running **SpireWatch** Steam lobby through the vanilla `Multiplayer -> Join Game` path and enter it as a read-only spectator.

## Status

This repository contains an experimental spectator implementation for local game `v0.109.0`. The host keeps its existing Steam friends lobby open after the run starts, accepts a compatible running-session rejoin as a spectator, and broadcasts the existing game messages to that connection. The spectator loads the host snapshot through a read-only network service that impersonates no new player and rejects all game-message sends.

Open `Multiplayer -> Join Game` and select the host's running SpireWatch room directly from the original friend-room list. SpireWatch reads that room's Steam lobby metadata before wiring its button: a compatible `phase=running` room starts the read-only spectator flow, while every other room keeps the original join flow. The Steam64-ID panel and `--spirewatch-spectate=<Steam64ID>` remain available as fallback entry points.

Combat-state reconstruction has no public apply API in the local game assembly. Mid-combat visual restoration therefore remains unverified and must not be relied upon until a live two-account test confirms it. See [architecture.md](architecture.md) and [runtime-validation.md](runtime-validation.md).

The design references and their licensing boundaries are recorded in [research-sources.md](research-sources.md).

## Build Prerequisites

- Slay the Spire 2 managed data directory containing `sts2.dll`, `0Harmony.dll`, `GodotSharp.dll`, and `Steamworks.NET.dll`
- .NET SDK 9

Copy `local.props.example` to `local.props`, set `Sts2DataDir`, then run:

```bash
C:\Users\jiudu\.local\dotnet9\dotnet.exe build SpireWatch.csproj -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-static.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-game-contract.ps1 -DotnetExecutable C:\Users\jiudu\.local\dotnet9\dotnet.exe
```

To copy the DLL and manifest to the game Mods directory during build, add `/p:DeployToGame=true`. The repository does not include game DLLs or Steamworks DLLs.

## Protocol Metadata

The host writes these keys using `SteamMatchmaking.SetLobbyData` against `NetHostGameService.NetHost.LobbyId`:

| Key | Meaning |
| --- | --- |
| `spirewatch=1` | This lobby supports the SpireWatch protocol. |
| `phase=lobby|running` | `running` is the only non-vanilla state that will later be listed/joinable. |
| `protocol=1` | SpireWatch wire compatibility version. |
| `mod_version` | Host mod assembly version. |
| `spectator_count` | Current mod-owned spectator count; it does not affect vanilla player capacity. |

## Repository Boundaries

- Steam Lobby and vanilla `INetGameService` remain the only transport.
- Spectators are keyed by their real Steam NetId on the host but never enter host `RunState.Players`.
- The game-version and gameplay-Mod compatibility checks remain owned by vanilla `JoinFlow`; SpireWatch is declared gameplay-affecting so it participates in that check.
- The read-only service rejects all gameplay messages after the rejoin handshake, and replay/save writing is disabled for the spectator session.
- Any code derived from references will respect their licenses. STS2-Agent is used only as a build-layout reference and is not copied.
