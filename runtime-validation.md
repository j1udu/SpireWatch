# Runtime Validation Plan

## Current Evidence Status

| Claim | Evidence | Status |
| --- | --- | --- |
| Vanilla host exposes a Steam lobby ID | RMP source reads `NetHostGameService.NetHost.LobbyId` | Source-confirmed, target build pending |
| Existing Steam lobby accepts metadata | Steamworks.NET `SteamMatchmaking.SetLobbyData` API; publisher uses reflection | Source/API-shaped, live pending |
| `StartRunLobby` constructor signature | RMP Harmony patch | Source-confirmed, target build pending |
| Running connection can carry `SerializableRun` | DirectConnectIP `JoinFlow` and rejoin source | Source-confirmed, spectator adaptation pending |
| Vanilla join-room list can include running entries | No target assembly/UI source available | Unverified; not implemented |
| Host can retain the lobby after run start | No live Steam session available | Unverified |

## Required Assembly Investigation

With `STS2_DATA_DIR` set, inspect the exact target build before adding list or join patches:

```bash
dotnet build SpireWatch.csproj -c Debug
rg -a -n "JoinFlow|RunLobby|LobbyList|RequestLobbyList|SetLobbyData|ClientRejoinResponseMessage" "$STS2_DATA_DIR/sts2.dll"
```

Use a .NET decompiler or an API signature dump to record, with full parameter types:

1. The class/method that requests the vanilla join-room list and its running-state filter.
2. The room-row view model and renderer used by that screen.
3. The room-row Join button callback and the lobby ID it supplies to `JoinFlow`.
4. The host-side method that handles a running rejoin/admission request.
5. The precise `RunLobby` method that transitions host state to running.
6. The disconnect/cleanup method where lobby keys must be cleared or updated.

Do not implement a Harmony attribute for any of these until the target build supplies the exact signature.

## Stage 1 Live Test Matrix

Run with Steam online and two compatible mod installations:

1. Host a normal multiplayer lobby. Confirm the original lobby receives `spirewatch=1`, `phase=lobby`, protocol, mod version, and `spectator_count=0`.
2. Add/remove a vanilla player before start. Confirm normal joining is unchanged and no player limit changes.
3. Start the run. Confirm the same Steam `LobbyId` remains valid and its phase changes to `running`.
4. Confirm an unmodded normal running lobby is neither marked nor admitted by SpireWatch.
5. Confirm host quit/disconnect does not leave a joinable stale `phase=running` lobby.

## Stage 2-4 Acceptance Gates

- All endpoints refuse game, mod protocol, RitsuLib, and dependency mismatches with a user-visible reason.
- A spectator causes no `RunState.Players` mutation, no character creation, and no player-slot use.
- Safe-point `SerializableRun` recovery displays the current map/combat/shop/reward/event state.
- UI actions and low-level command/action dispatch both reject state-changing spectator inputs.
- Combat animation/chain-resolution joins remain rejected until a later safe point.

## Current Local Verification

`./scripts/verify-static.sh` can run without game files. Compilation, PCK packaging, and Steam multiplayer testing are blocked until the prerequisites in `README.md` are installed.
