using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using SpireWatch.Networking;

namespace SpireWatch.Patches;

/// <summary>
/// Marks the host's existing Steam lobby as running at the exact v0.109.0 start-run transition.
/// </summary>
[HarmonyPatch(typeof(StartRunLobby), "BeginRunLocally")]
internal static class RunningLobbyLifecyclePatch
{
    private static void Postfix(StartRunLobby __instance)
    {
        if (__instance.NetService.Type != NetGameType.Host)
        {
            return;
        }

        SteamLobbyMetadataPublisher.TryPublish(__instance.NetService, LobbyPhase.Running);
    }
}
