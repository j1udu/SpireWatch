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
using MegaCrit.Sts2.Core.Runs;
using SpireWatch.Networking;
using SpireWatch.Rooms;
using SpireWatch.Spectating;

namespace SpireWatch.Patches;

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

        SpectatorProtocol.EnsureHostBound(hostService);
        SpectatorViewSwitch.EnsureHostBound(hostService);
        RunActionJournal.EnsureHostBound(hostService);
        hostService.SendMessage(new SpectatorChallengeMessage(), playerId);
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

        if (!SpectatorProtocol.TryAuthorizeHostPeer(senderId, out var protocolFailure))
        {
            service.DisconnectClient(senderId, NetError.InvalidJoin);
            Log.Warn($"[{ModInfo.Id}] Rejected spectator {senderId}: {protocolFailure}.");
            return false;
        }

        if (!SpectatorJoinSafety.IsSafeHostJoinPoint(out var safetyFailure))
        {
            service.DisconnectClient(senderId, NetError.InvalidJoin);
            Log.Warn($"[{ModInfo.Id}] Rejected spectator {senderId}: {safetyFailure}.");
            return false;
        }

        var actionCheckpoint = RunActionJournal.CaptureCheckpoint();
        SpectatorRegistry.AddHostSpectator(senderId, observedPlayer.NetId);
        RoomRosterCoordinator.RefreshHostRunningRoster();
        var rejoinMessage = RunManager.Instance.GetRejoinMessage();
        rejoinMessage.serializableRun = RunManager.Instance.ToSave(runState.CurrentRoom);
        service.SendMessage(rejoinMessage, senderId);
        RunActionJournal.AwaitSpectatorReplay(senderId, actionCheckpoint);
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
        SpectatorProtocol.ForgetHostPeer(playerId);
        RunActionJournal.ForgetSpectator(playerId);
        RoomRosterCoordinator.RefreshHostRunningRoster();
        if (RunManager.Instance.NetService is not null)
        {
            SteamLobbyMetadataPublisher.TryPublish(RunManager.Instance.NetService, LobbyPhase.Running, SpectatorRegistry.HostSpectatorCount);
        }
        Log.Info($"[{ModInfo.Id}] Spectator {playerId} disconnected; total spectators={SpectatorRegistry.HostSpectatorCount}.");
    }
}
