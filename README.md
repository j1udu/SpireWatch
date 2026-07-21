# SpireWatch

SpireWatch 是《杀戮尖塔 2》多人 Mod，用于安全地扩展原版 Steam 多人 Lobby 的生命周期。最终目标是在原版“多人游戏 -> 加入房间”流程中，让兼容玩家以只读观战者身份加入已开始的 SpireWatch 房间。

## 当前状态

本仓库当前实现 **阶段 0/1**，面向本地已分析的游戏版本 `v0.109.0`：房主会在原版 Steam Lobby 中写入 `lobby`、`running` 或 `closed` 状态；仅在房主局实际进行期间阻止原版关闭该 Lobby。当前版本**不会**在加入房间列表中展示进行中的房间，也不会接纳观战者。

此前的快照恢复方案在观战端复用了已有原版玩家的 `NetId`。这违反了 Spectator 不是原版 Player 的项目约束，因此该路径已被移除，而非作为实验功能保留。阶段 2 需要实现带版本校验的自定义握手和 Mod 自己维护的 `SpectatorSession`；阶段 3 需要实现绝不冒用 Player 身份的局面恢复路径。详见 [architecture.md](architecture.md) 与 [runtime-validation.md](runtime-validation.md)。

参考仓库、版本和许可证边界记录在 [research-sources.md](research-sources.md)。

## 构建前提

- 《杀戮尖塔 2》的托管程序集目录，其中应包含 `sts2.dll`、`0Harmony.dll`、`GodotSharp.dll` 与 `Steamworks.NET.dll`
- .NET SDK 9

将 `local.props.example` 复制为 `local.props`，设置 `Sts2DataDir` 后执行：

```bash
C:\Users\jiudu\.local\dotnet9\dotnet.exe build SpireWatch.csproj -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-static.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-game-contract.ps1 -DotnetExecutable C:\Users\jiudu\.local\dotnet9\dotnet.exe
```

构建时附加 `/p:DeployToGame=true` 可将 DLL 和 Manifest 复制到游戏的 Mods 目录。仓库不包含任何游戏 DLL 或 Steamworks DLL。

## Lobby 协议元数据

房主通过 `NetHostGameService.NetHost.LobbyId` 定位原版 Steam Lobby，并使用 `SteamMatchmaking.SetLobbyData` 写入以下键：

| 键 | 含义 |
| --- | --- |
| `spirewatch=1` | 表示该 Lobby 支持 SpireWatch 协议。 |
| `phase=lobby|running|closed` | 未来仅 `running` 会作为可观战状态展示和加入；`closed` 用于在清理期间消除陈旧的运行中标记。 |
| `protocol=1` | SpireWatch 网络协议兼容版本。 |
| `mod_version` | 房主的 Mod 程序集版本。 |
| `spectator_count` | Mod 维护的观战者数量，不影响原版玩家容量。 |

## 工程边界

- Steam Lobby 与原版 `INetGameService` 是唯一的联机传输方式。
- 在协议可以维护独立 `SpectatorSession` 且不使用任何原版 Player 身份前，不会启用观战连接或快照恢复。
- 未来会在房主侧显式校验游戏版本、Mod 版本、协议、RitsuLib 和依赖；Lobby 元数据本身不是入场许可。
- 对参考仓库的使用遵守其许可证边界；STS2-Agent 仅用于构建布局参考，未复制其源代码。
