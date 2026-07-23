using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using SpireWatch.Networking;
using SpireWatch.Spectating;

namespace SpireWatch.Rooms;

/// <summary>Builds host-authoritative running-room roster snapshots without changing vanilla player state.</summary>
internal static class RoomRosterCoordinator
{
    private static readonly HashSet<ulong> DisconnectedPlayers = new();
    private static RunLobby? _runningLobby;

    internal static void BindRunningLobby(RunLobby lobby)
    {
        if (ReferenceEquals(_runningLobby, lobby))
        {
            RefreshHostRunningRoster();
            return;
        }

        UnbindRunningLobby();
        _runningLobby = lobby;
        lobby.PlayerRejoined += OnPlayerRejoined;
        lobby.RemotePlayerDisconnected += OnRemotePlayerDisconnected;
        RefreshHostRunningRoster();
    }

    internal static void RefreshHostRunningRoster()
    {
        var runManager = RunManager.Instance;
        var state = runManager.DebugOnlyGetState();
        if (state is null || runManager.NetService?.Type != NetGameType.Host)
        {
            return;
        }

        var members = state.Players.Select(ToPlayingMember).Concat(SpectatorRegistry.HostSessions.Select(ToSpectatorMember));
        RoomRoster.SetRunningHostRoster(members);
    }

    internal static void Clear()
    {
        UnbindRunningLobby();
        DisconnectedPlayers.Clear();
    }

    private static void UnbindRunningLobby()
    {
        if (_runningLobby is null)
        {
            return;
        }

        _runningLobby.PlayerRejoined -= OnPlayerRejoined;
        _runningLobby.RemotePlayerDisconnected -= OnRemotePlayerDisconnected;
        _runningLobby = null;
    }

    private static void OnPlayerRejoined(ulong playerId)
    {
        DisconnectedPlayers.Remove(playerId);
        RefreshHostRunningRoster();
    }

    private static void OnRemotePlayerDisconnected(ulong playerId)
    {
        DisconnectedPlayers.Add(playerId);
        RefreshHostRunningRoster();
    }

    private static RoomMember ToPlayingMember(Player player)
    {
        return new RoomMember(
            player.NetId,
            RoomRoster.GetDisplayName(player.NetId),
            RoomMemberRole.Playing,
            player.Character,
            ObservedPlayerNetId: null,
            IsConnected: !DisconnectedPlayers.Contains(player.NetId),
            IsReady: false);
    }

    private static RoomMember ToSpectatorMember(SpectatorSession session)
    {
        return new RoomMember(
            session.SpectatorNetId,
            RoomRoster.GetDisplayName(session.SpectatorNetId),
            RoomMemberRole.Spectating,
            Character: null,
            session.ProjectedPlayerNetId,
            IsConnected: true,
            IsReady: false);
    }
}
