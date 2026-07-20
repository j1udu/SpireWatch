# Research Sources

This document records the source-only research used during Stage 0. It does not import source code from any referenced repository.

| Repository | Revision inspected | Used for | License / boundary |
| --- | --- | --- | --- |
| [Rain156/sts2-RMP-Mods](https://github.com/Rain156/sts2-RMP-Mods) | `7c977a1e30dea2c48035dd78291949cdc0f7daf1` | `NetHostGameService.StartSteamHost`, `StartRunLobby`, `SteamHost.LobbyId`, custom `INetMessage` registration | Its current README states CC0; this project independently implements only the documented integration shape. |
| [TasteSteak/sts2-DirectConnectIP-Mods](https://github.com/TasteSteak/sts2-DirectConnectIP-Mods) | `4e945f58d34ad74b87bfccd9f9d7618b20a41f77` | `JoinFlow`, `RunSessionState.Running`, `ClientRejoinResponseMessage`, `SerializableRun`, `SetUpSavedMultiplayer` call chain | Direct-IP transport and its source are not reused. |
| [BAKAOLC/STS2-RitsuLib](https://github.com/BAKAOLC/STS2-RitsuLib) | `314f93cb4e54d5333ccec6415b6f46aca0241a64` | Manifest dependency and recommended lifecycle/patching framework | MIT; declared as a runtime dependency. |
| [CharTyr/STS2-Agent](https://github.com/CharTyr/STS2-Agent) | `6957e274f5b6ef4028172297523b227e6c6a96eb` | Cross-platform project/build and validation layout | AGPL-3.0; no source code, HTTP/MCP architecture, or network implementation is copied. |

The source facts relied on by `architecture.md` are intentionally narrow. Every target-game method still needs revalidation against the exact installed `sts2.dll` before it becomes a fixed Harmony patch.
