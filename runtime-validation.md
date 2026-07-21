# Runtime Validation Plan

## Current Evidence Status

| Claim | Evidence | Status |
| --- | --- | --- |
| Vanilla host exposes a Steam lobby ID | Local `v0.109.0` `SteamHost.LobbyId` inspection | Target-confirmed, live pending |
| Existing Steam lobby accepts metadata | Local `v0.109.0` `SteamMatchmaking.SetLobbyData` inspection; publisher uses reflection | Target-confirmed, live pending |
| `StartRunLobby` constructor signature | Local `v0.109.0` inspection | Target-confirmed |
| `StartRunLobby.BeginRunLocally` is the host run-start transition | Local `v0.109.0` inspection | Target-confirmed, live pending |
| Running connection can carry `SerializableRun` | Local `JoinFlow`, `RunLobby`, and `RunManager.GetRejoinMessage` inspection | Target-confirmed, live pending |
| Vanilla join-room list can include running friend rooms | Local `NJoinFriendScreen.ShowFriends`, `SteamPlatformUtilStrategy.GetFriendsWithOpenLobbies`, and Steamworks `RequestLobbyData` inspection | Target-confirmed, live pending |
| Host can retain the lobby after run start | No live Steam session available | Unverified |

## Required Assembly Investigation

With `STS2_DATA_DIR` set, inspect the exact target build before adding list or join patches:

```bash
dotnet build SpireWatch.csproj -c Debug
rg -a -n "JoinFlow|RunLobby|LobbyList|RequestLobbyList|SetLobbyData|ClientRejoinResponseMessage" "$STS2_DATA_DIR/sts2.dll"
```

For the host metadata bindings already implemented, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-game-contract.ps1 -DotnetExecutable C:\Users\jiudu\.local\dotnet9\dotnet.exe
```

The friend-list binding uses these inspected `v0.109.0` members:

The friend-list patch reads `spirewatch=1`, `phase=running`, matching `protocol`, and matching `mod_version` before sending a row through `SpectatorJoinFlow`. The host independently requires `SpectatorChallengeMessage` / `SpectatorHelloMessage` before it accepts the vanilla rejoin request.

## Spectator Live Test Matrix

Run with Steam online and two compatible mod installations:

1. Host a normal multiplayer lobby. Confirm the original lobby receives `spirewatch=1`, `phase=lobby`, protocol, mod version, and `spectator_count=0`.
2. Add/remove a vanilla player before start. Confirm normal joining is unchanged and no player limit changes.
3. Start the run. Confirm the same Steam `LobbyId` remains friends-joinable and its phase changes to `running`.
4. On the second account, open `Multiplayer -> Join Game`, refresh the original friend-room list, and confirm the running room carries `进行中 · 观战`.
5. Confirm a mismatched protocol or `mod_version` does not enter the spectator branch, and the host rejects a missing protocol challenge response.
6. Confirm an active combat rejects a new spectator; map, shop, reward, event, rest and chest joins must be tested separately.
7. Attempt cards, end turn, map selection, rewards, event options, purchases and campfire actions on the spectator. Confirm host state is unchanged.
8. Disconnect the spectator and end the run. Confirm local controls are restored, host `RunState.Players` is unchanged, and the Lobby phase becomes `closed` during cleanup.

## Stage 2-4 Acceptance Gates

- All endpoints refuse game, mod protocol, RitsuLib, and dependency mismatches with a user-visible reason.
- A spectator causes no `RunState.Players` mutation, no character creation, and no player-slot use.
- Safe-point `SerializableRun` recovery displays the current map/combat/shop/reward/event state.
- UI actions and low-level command/action dispatch both reject state-changing spectator inputs.
- Combat animation/chain-resolution joins remain rejected until a later safe point.

## Current Local Verification

`./scripts/verify-static.sh` can run without game files. Compilation, PCK packaging, and Steam multiplayer testing are blocked until the prerequisites in `README.md` are installed.
