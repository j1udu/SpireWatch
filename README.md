# SpireWatch

SpireWatch is a Slay the Spire 2 multiplayer mod that lets compatible players discover an already-running **SpireWatch** Steam lobby through the vanilla `Multiplayer -> Join Game` path and enter it as a read-only spectator.

## Status

This repository contains the Stage 0 investigation and the host-side portion of Stage 1. It writes SpireWatch metadata into the **existing vanilla Steam lobby**. It does not create a spectator menu, an HTTP/WebSocket service, a second lobby, a fake player, or any moderation feature.

The game assemblies, .NET SDK, Godot executable, and a live Steam session were unavailable while this initial work was produced. The vanilla room-list interception has therefore not been implemented: its exact target must be recovered from the target game's `sts2.dll` first. See [architecture.md](architecture.md) and [runtime-validation.md](runtime-validation.md).

The design references and their licensing boundaries are recorded in [research-sources.md](research-sources.md).

## Build Prerequisites

- Slay the Spire 2 managed data directory containing `sts2.dll`, `0Harmony.dll`, and `GodotSharp.dll`
- .NET SDK 9
- The game-bundled Godot executable for PCK packaging
- Matching STS2-RitsuLib runtime installed alongside the mod

Copy `local.props.example` to `local.props`, set `Sts2DataDir`, then run:

```bash
dotnet build SpireWatch.csproj -c Release
./scripts/verify-static.sh
```

PCK packaging and live Steam validation remain intentionally blocked until a game installation is available. The repository does not include game DLLs or Steamworks DLLs.

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
- Future spectators are separate `SpectatorSession` records keyed by SteamId/NetId and will never enter `RunState.Players`.
- Stage 2 will add handshake/version checks; Stage 3 will add snapshot recovery and dual UI/command read-only enforcement.
- Any code derived from references will respect their licenses. STS2-Agent is used only as a build-layout reference and is not copied.
