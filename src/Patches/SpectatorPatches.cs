using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using SpireWatch.Networking;
using SpireWatch.Spectating;

namespace SpireWatch.Patches;

[HarmonyPatch(typeof(NGame), "_Ready")]
internal static class SpectatorBootstrapPatch
{
    private static void Postfix(NGame __instance)
    {
        SpectatorBootstrap.InstallOn(__instance);
    }
}

[HarmonyPatch(typeof(NJoinFriendScreen), nameof(NJoinFriendScreen.OnSubmenuOpened))]
internal static class SpectatorJoinPanelPatch
{
    private static void Postfix(NJoinFriendScreen __instance)
    {
        SpectatorJoinPanel.InstallOn(__instance);
    }
}

[HarmonyPatch]
internal static class SteamHostClosePatch
{
    private static MethodBase? TargetMethod()
    {
        return AccessTools.Method("MegaCrit.Sts2.Core.Multiplayer.Transport.Steam.SteamHost:SetHostIsClosed");
    }

    private static bool Prefix(bool isClosed)
    {
        if (!isClosed)
        {
            return true;
        }

        Log.Info($"[{ModInfo.Id}] Keeping the Steam lobby friends-only for spectator connections.");
        return false;
    }
}

[HarmonyPatch(typeof(RunLobby), "OnConnectedToClientAsHost")]
internal static class RunLobbySpectatorConnectionPatch
{
    private static bool Prefix(RunLobby __instance, ulong playerId)
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState is null || runState.Players.Any(player => player.NetId == playerId))
        {
            return true;
        }

        var service = AccessTools.Field(typeof(RunLobby), "_netService")?.GetValue(__instance) as INetGameService;
        if (service is not NetHostGameService hostService)
        {
            return true;
        }

        var message = InitialGameInfoMessage.Basic();
        message.sessionState = RunSessionState.Running;
        message.gameMode = __instance.GameMode;
        hostService.SendMessage(message, playerId);
        Log.Info($"[{ModInfo.Id}] Awaiting spectator rejoin handshake from {playerId}.");
        return false;
    }
}

[HarmonyPatch(typeof(RunLobby), "HandleClientRejoinRequestMessage")]
internal static class RunLobbySpectatorRejoinPatch
{
    private static bool Prefix(RunLobby __instance, ulong senderId)
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState is null || runState.Players.Any(player => player.NetId == senderId))
        {
            return true;
        }

        var service = AccessTools.Field(typeof(RunLobby), "_netService")?.GetValue(__instance) as NetHostGameService;
        var observedPlayer = runState.Players.FirstOrDefault();
        if (service is null || observedPlayer is null)
        {
            return true;
        }

        SpectatorRegistry.AddHostSpectator(senderId, observedPlayer.NetId);
        var rejoinMessage = RunManager.Instance.GetRejoinMessage();
        rejoinMessage.serializableRun = RunManager.Instance.ToSave(runState.CurrentRoom);
        service.SendMessage(rejoinMessage, senderId);
        service.SetPeerReadyForBroadcasting(senderId);
        SteamLobbyMetadataPublisher.TryPublish(service, LobbyPhase.Running, SpectatorRegistry.HostSpectatorCount);
        Log.Info($"[{ModInfo.Id}] Accepted spectator {senderId}; observing {observedPlayer.NetId}; total spectators={SpectatorRegistry.HostSpectatorCount}.");
        return false;
    }
}

[HarmonyPatch(typeof(RunLobby), "OnDisconnectedFromClientAsHost")]
internal static class RunLobbySpectatorDisconnectPatch
{
    private static void Prefix(ulong playerId)
    {
        if (!SpectatorRegistry.IsHostSpectator(playerId))
        {
            return;
        }

        SpectatorRegistry.RemoveHostSpectator(playerId);
        if (RunManager.Instance.NetService is not null)
        {
            SteamLobbyMetadataPublisher.TryPublish(RunManager.Instance.NetService, LobbyPhase.Running, SpectatorRegistry.HostSpectatorCount);
        }
        Log.Info($"[{ModInfo.Id}] Spectator {playerId} disconnected; total spectators={SpectatorRegistry.HostSpectatorCount}.");
    }
}
