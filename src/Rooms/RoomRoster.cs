using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Platform;

namespace SpireWatch.Rooms;

public enum RoomMemberRole
{
    Playing,
    Spectating
}

/// <summary>
/// UI-facing room member state. Network serialization is deliberately deferred to the room protocol
/// milestone; this first version mirrors the authoritative waiting lobby only.
/// </summary>
internal sealed record RoomMember(
    ulong NetId,
    string DisplayName,
    RoomMemberRole Role,
    CharacterModel? Character,
    ulong? ObservedPlayerNetId,
    bool IsConnected,
    bool IsReady);

/// <summary>
/// A local, read-only projection of the current room roster. It never owns membership and never
/// changes StartRunLobby.Players; the original lobby remains authoritative.
/// </summary>
internal static class RoomRoster
{
    private static readonly List<RoomMember> MembersStorage = new();
    private static StartRunLobby? _waitingLobby;
    private static bool _isRunning;

    internal static IReadOnlyList<RoomMember> Members => MembersStorage;
    internal static bool IsRunning => _isRunning;

    internal static event Action? Changed;

    internal static void BindWaitingLobby(StartRunLobby lobby)
    {
        if (ReferenceEquals(_waitingLobby, lobby))
        {
            RefreshWaitingLobby();
            return;
        }

        UnbindWaitingLobby();
        _waitingLobby = lobby;
        SetRunning(false);
        lobby.PlayerConnected += OnWaitingLobbyChanged;
        lobby.PlayerDisconnected += OnWaitingLobbyChanged;
        RefreshWaitingLobby();
    }

    internal static void RefreshWaitingLobby()
    {
        if (_waitingLobby is null)
        {
            return;
        }

        ReplaceMembers(_waitingLobby.Players.Select(ToRoomMember));
    }

    internal static void Clear()
    {
        UnbindWaitingLobby();
        SetRunning(false);
        ReplaceMembers(Array.Empty<RoomMember>());
    }

    internal static void SetRunningHostRoster(IEnumerable<RoomMember> members)
    {
        UnbindWaitingLobby();
        SetRunning(true);
        ReplaceMembers(members);
    }

    internal static void ApplyNetworkSnapshot(IEnumerable<RoomMember> members, bool isRunning)
    {
        UnbindWaitingLobby();
        SetRunning(isRunning);
        ReplaceMembers(members);
    }

    private static void UnbindWaitingLobby()
    {
        if (_waitingLobby is null)
        {
            return;
        }

        _waitingLobby.PlayerConnected -= OnWaitingLobbyChanged;
        _waitingLobby.PlayerDisconnected -= OnWaitingLobbyChanged;
        _waitingLobby = null;
    }

    private static void OnWaitingLobbyChanged(LobbyPlayer _)
    {
        RefreshWaitingLobby();
    }

    private static RoomMember ToRoomMember(LobbyPlayer player)
    {
        return new RoomMember(
            player.id,
            GetDisplayName(player.id),
            RoomMemberRole.Playing,
            player.character,
            ObservedPlayerNetId: null,
            IsConnected: true,
            player.isReady);
    }

    internal static string GetDisplayName(ulong playerId)
    {
        try
        {
            return PlatformUtil.GetPlayerName(PlatformType.Steam, playerId);
        }
        catch
        {
            return playerId.ToString();
        }
    }

    private static void ReplaceMembers(IEnumerable<RoomMember> members)
    {
        var nextMembers = members.ToList();
        if (MembersStorage.SequenceEqual(nextMembers))
        {
            return;
        }

        MembersStorage.Clear();
        MembersStorage.AddRange(nextMembers);
        Changed?.Invoke();
    }

    private static void SetRunning(bool value)
    {
        if (_isRunning == value)
        {
            return;
        }

        _isRunning = value;
        Changed?.Invoke();
    }
}
