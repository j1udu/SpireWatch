# SpireWatch

SpireWatch is a Slay the Spire 2 multiplayer mod for safely exposing the lifecycle of the existing Steam multiplayer lobby. Its final goal is read-only spectators through the vanilla `Multiplayer -> Join Game` flow.

## Status

This repository currently implements **Stage 0/1** for local game `v0.109.0`: the host marks its existing Steam lobby as `lobby`, `running`, or `closed`, and suppresses vanilla lobby closure only during an active host run. It does not yet display running rooms in the join list or admit spectators.

The prior snapshot path reused an existing player's NetId on the spectator client. That conflicts with the project's requirement that a Spectator is not a vanilla Player, so it was removed rather than exposed as an experimental feature. Stage 2 must add a versioned custom handshake and a mod-owned `SpectatorSession`; Stage 3 needs a view recovery path that never impersonates a Player. See [architecture.md](architecture.md) and [runtime-validation.md](runtime-validation.md).

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
| `phase=lobby|running|closed` | `running` will later be the only non-vanilla state listed/joinable; `closed` prevents stale running metadata during cleanup. |
| `protocol=1` | SpireWatch wire compatibility version. |
| `mod_version` | Host mod assembly version. |
| `spectator_count` | Current mod-owned spectator count; it does not affect vanilla player capacity. |

## Repository Boundaries

- Steam Lobby and vanilla `INetGameService` remain the only transport.
- No spectator connection or snapshot recovery is enabled until the protocol can maintain a separate `SpectatorSession` without a vanilla Player identity.
- Future game-version, mod-version, protocol, RitsuLib, and dependency validation will be explicit host-side admission checks; metadata alone is not an admission decision.
- Any code derived from references will respect their licenses. STS2-Agent is used only as a build-layout reference and is not copied.
