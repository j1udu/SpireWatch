# SpireWatch

SpireWatch 是《杀戮尖塔 2》多人 Mod，用于安全地扩展原版 Steam 多人 Lobby 的生命周期。最终目标是在原版“多人游戏 -> 加入房间”流程中，让兼容玩家以只读观战者身份加入已开始的 SpireWatch 房间。

## 当前状态

本仓库当前包含实验性的观战加入实现，面向已分析的游戏版本 `v0.109.0`。房主会在原版 Steam Lobby 中写入 `lobby`、`running` 或 `closed` 状态，并仅在房主局实际进行期间保留 Lobby。原版好友房间列表中的兼容 running Lobby 会显示“进行中 · 观战”，点击后进入只读观战流程；普通等待房间仍走原版加入流程。

观战加入前，客户端会校验 Lobby 的 `protocol` 和 `mod_version`；host 会发送 SpireWatch 协议挑战，并在原版重连请求前验证回应。host 只允许非战斗状态且当前房间可序列化时发送快照。观战端在 UI、动作请求和所有 `INetGameService.SendMessage` 路径上拦截可变操作，并在失败、断线和 `RunManager.CleanUp` 时清除本地观战状态。

当前局面恢复仍通过一个隔离的只读“玩家投影”桥接原版 `LoadRunLobby` UI；它不会在 host `RunState.Players` 中创建 Player，但尚不等同于最终独立的 Spectator 视图。后续阶段必须替换这一桥接，彻底移除对 Player NetId 的依赖。详见 [architecture.md](architecture.md) 与 [runtime-validation.md](runtime-validation.md)。

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

## 玩家安装

当前仓库尚未发布可直接下载的 Release 包。发布后，解压并将以下两个文件放到游戏的 `mods/SpireWatch/` 目录：

```text
SpireWatch.dll
SpireWatch.json
```

目录应如下所示：

```text
<Slay the Spire 2>/
  mods/
    SpireWatch/
      SpireWatch.dll
      SpireWatch.json
```

- Windows / Linux：`<Slay the Spire 2>/mods/SpireWatch/`
- macOS：`<SlayTheSpire2.app>/Contents/MacOS/mods/SpireWatch/`

如果暂时从源码安装，在已配置 `local.props` 后运行：

```powershell
dotnet build SpireWatch.csproj -c Release /p:DeployToGame=true
```

这会把生成的 DLL 与 Manifest 复制到上述目录。不要复制 `sts2.dll`、`Steamworks.NET.dll` 或其他游戏程序集。

联机时，房主、普通玩家和观战者都必须使用兼容的游戏版本，并安装**完全相同版本**的 SpireWatch；所有影响游戏性的其他 Mod 也应保持一致。启动游戏后由房主按原版方式创建多人房间，开局后观战者从原版“多人游戏 -> 加入房间”的好友房间列表点击标有“进行中 · 观战”的房间。

> 当前观战功能仍为实验性实现。首次联机请优先在非战斗安全点测试，并保留游戏日志以便排查问题。

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
- host 以 Mod 自己维护的 `SpectatorSession` 记录观战者，不会把观战者插入 `RunState.Players`。
- 当前会显式校验协议和 Lobby Mod 版本；游戏版本、游戏性 Mod 和依赖兼容性仍由原版 `JoinFlow` 及 `affects_gameplay` 清单参与校验。RitsuLib 的显式版本拒绝仍待加入。
- 对参考仓库的使用遵守其许可证边界；STS2-Agent 仅用于构建布局参考，未复制其源代码。
