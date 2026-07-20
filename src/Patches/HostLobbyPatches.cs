using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using SpireWatch.Networking;

namespace SpireWatch.Patches;

/// <summary>Marks the vanilla Steam lobby after vanilla hosting and lobby setup have completed.</summary>
[HarmonyPatch(typeof(NetHostGameService), nameof(NetHostGameService.StartSteamHost))]
internal static class StartSteamHostMetadataPatch
{
    private static void Postfix(NetHostGameService __instance)
    {
        SteamLobbyMetadataPublisher.TryPublish(__instance, LobbyPhase.Lobby);
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
