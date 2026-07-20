using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using SpireWatch.Networking;

namespace SpireWatch.Patches;

/// <summary>Marks the vanilla Steam lobby after vanilla hosting and lobby setup have completed.</summary>
[HarmonyPatch(typeof(NetHostGameService), nameof(NetHostGameService.StartSteamHost))]
internal static class StartSteamHostMetadataPatch
{
    private static void Postfix(NetHostGameService __instance, Task __result)
    {
        _ = PublishAfterHostStarts(__instance, __result);
    }

    private static async Task PublishAfterHostStarts(NetHostGameService hostService, Task startTask)
    {
        try
        {
            await startTask;
            SteamLobbyMetadataPublisher.TryPublish(hostService, LobbyPhase.Lobby);
        }
        catch (Exception exception)
        {
            MegaCrit.Sts2.Core.Logging.Log.Warn($"[{ModInfo.Id}] Steam host metadata publication failed after host startup: {exception.Message}");
        }
    }
}

[HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor,
    typeof(GameMode), typeof(INetGameService), typeof(IStartRunLobbyListener), typeof(int))]
internal static class StartRunLobbyMetadataPatch
{
    private static void Postfix(INetGameService netService)
    {
        SteamLobbyMetadataPublisher.TryPublish(netService, LobbyPhase.Lobby);
    }
}
