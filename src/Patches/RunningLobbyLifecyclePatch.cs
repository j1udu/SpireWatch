using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using SpireWatch.Networking;

namespace SpireWatch.Patches;

/// <summary>
/// Resolves the run-start method at runtime because its exact name is version-dependent.
/// It intentionally installs nothing when the signature cannot be proven on the target game build.
/// </summary>
internal static class RunningLobbyLifecyclePatch
{
    internal static void TryInstall(Harmony harmony)
    {
        var target = AccessTools.Method(typeof(RunLobby), "BeginRun") ??
                     AccessTools.Method(typeof(RunLobby), "BeginRunLocally");

        if (target is null)
        {
            Log.Warn($"[{ModInfo.Id}] Could not resolve a RunLobby run-start method. Lobby phase stays 'lobby'; see runtime-validation.md.");
            return;
        }

        harmony.Patch(target, postfix: new HarmonyMethod(typeof(RunningLobbyLifecyclePatch), nameof(AfterRunStarts)));
        Log.Info($"[{ModInfo.Id}] Bound running-lobby metadata to {target.DeclaringType?.Name}.{target.Name}.");
    }

    private static void AfterRunStarts()
    {
        if (RunManager.Instance?.NetService is not { Type: NetGameType.Host } netService)
        {
            return;
        }

        SteamLobbyMetadataPublisher.TryPublish(netService, LobbyPhase.Running);
    }
}
