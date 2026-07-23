using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Nodes;
using SpireWatch.Rooms;

namespace SpireWatch.Networking;

/// <summary>Small wire representation of a room member. Names are looked up locally by platform ID.</summary>
public struct RoomMemberWire : IPacketSerializable
{
    public ulong NetId;
    public RoomMemberRole Role;
    public bool HasCharacter;
    public string CharacterCategory;
    public string CharacterEntry;
    public bool HasObservedPlayer;
    public ulong ObservedPlayerNetId;
    public bool IsConnected;
    public bool IsReady;

    public readonly void Serialize(PacketWriter writer)
    {
        writer.WriteULong(NetId);
        writer.WriteInt((int)Role, 2);
        writer.WriteBool(HasCharacter);
        if (HasCharacter)
        {
            writer.WriteString(CharacterCategory);
            writer.WriteString(CharacterEntry);
        }

        writer.WriteBool(HasObservedPlayer);
        if (HasObservedPlayer)
        {
            writer.WriteULong(ObservedPlayerNetId);
        }

        writer.WriteBool(IsConnected);
        writer.WriteBool(IsReady);
    }

    public void Deserialize(PacketReader reader)
    {
        NetId = reader.ReadULong();
        Role = (RoomMemberRole)reader.ReadInt(2);
        HasCharacter = reader.ReadBool();
        CharacterCategory = HasCharacter ? reader.ReadString() : string.Empty;
        CharacterEntry = HasCharacter ? reader.ReadString() : string.Empty;
        HasObservedPlayer = reader.ReadBool();
        ObservedPlayerNetId = HasObservedPlayer ? reader.ReadULong() : 0;
        IsConnected = reader.ReadBool();
        IsReady = reader.ReadBool();
    }
}

/// <summary>Host-to-peer full roster snapshot used by the room section in the native settings UI.</summary>
public struct RoomRosterSnapshotMessage : INetMessage, IPacketSerializable
{
    public bool IsRunning;
    public List<RoomMemberWire> Members;

    public readonly bool ShouldBroadcast => true;
    public readonly NetTransferMode Mode => NetTransferMode.Reliable;
    public readonly LogLevel LogLevel => LogLevel.Info;

    public readonly void Serialize(PacketWriter writer)
    {
        writer.WriteBool(IsRunning);
        writer.WriteList(Members ?? new List<RoomMemberWire>(), 6);
    }

    public void Deserialize(PacketReader reader)
    {
        IsRunning = reader.ReadBool();
        Members = reader.ReadList<RoomMemberWire>(6);
    }
}

public struct SpectatorSwitchTargetRequestMessage : INetMessage, IPacketSerializable
{
    public ulong RequestedPlayerNetId;

    public readonly bool ShouldBroadcast => false;
    public readonly NetTransferMode Mode => NetTransferMode.Reliable;
    public readonly LogLevel LogLevel => LogLevel.Info;

    public readonly void Serialize(PacketWriter writer) => writer.WriteULong(RequestedPlayerNetId);

    public void Deserialize(PacketReader reader) => RequestedPlayerNetId = reader.ReadULong();
}

public struct SpectatorSwitchTargetResponseMessage : INetMessage, IPacketSerializable
{
    public bool Accepted;
    public string FailureReason;
    public ulong NewObservedPlayerNetId;
    public MegaCrit.Sts2.Core.Saves.SerializableRun SerializableRun;

    public readonly bool ShouldBroadcast => false;
    public readonly NetTransferMode Mode => NetTransferMode.Reliable;
    public readonly LogLevel LogLevel => LogLevel.Info;

    public readonly void Serialize(PacketWriter writer)
    {
        writer.WriteBool(Accepted);
        if (!Accepted)
        {
            writer.WriteString(FailureReason ?? string.Empty);
            return;
        }

        writer.WriteULong(NewObservedPlayerNetId);
        writer.Write(SerializableRun);
    }

    public void Deserialize(PacketReader reader)
    {
        Accepted = reader.ReadBool();
        if (!Accepted)
        {
            FailureReason = reader.ReadString();
            return;
        }

        NewObservedPlayerNetId = reader.ReadULong();
        SerializableRun = reader.Read<MegaCrit.Sts2.Core.Saves.SerializableRun>();
        FailureReason = string.Empty;
    }
}

/// <summary>Owns the custom roster message handler for the active vanilla network service.</summary>
internal static class RoomRosterProtocol
{
    private static INetGameService? _service;
    private static bool _broadcastScheduled;

    internal static void Bind(INetGameService service)
    {
        if (ReferenceEquals(_service, service))
        {
            return;
        }

        Unbind();
        _service = service;
        service.RegisterMessageHandler<RoomRosterSnapshotMessage>(HandleSnapshot);
        RoomRoster.Changed += BroadcastIfHost;
    }

    internal static void Unbind(INetGameService? service = null)
    {
        if (_service is null || (service is not null && !ReferenceEquals(_service, service)))
        {
            return;
        }

        _service.UnregisterMessageHandler<RoomRosterSnapshotMessage>(HandleSnapshot);
        RoomRoster.Changed -= BroadcastIfHost;
        _service = null;
        _broadcastScheduled = false;
    }

    internal static void UnbindActive()
    {
        Unbind();
    }

    internal static void BroadcastHostRoster()
    {
        if (_service?.Type != NetGameType.Host)
        {
            return;
        }

        _service.SendMessage(CreateSnapshot());
    }

    internal static void SendHostRosterTo(ulong peerId)
    {
        if (_service?.Type != NetGameType.Host)
        {
            return;
        }

        _service.SendMessage(CreateSnapshot(), peerId);
    }

    private static void BroadcastIfHost()
    {
        if (_service?.Type != NetGameType.Host || _broadcastScheduled)
        {
            return;
        }

        _broadcastScheduled = true;
        _ = BroadcastAtNextFrameAsync(_service);
    }

    private static async System.Threading.Tasks.Task BroadcastAtNextFrameAsync(INetGameService service)
    {
        try
        {
            if (NGame.Instance is not null)
            {
                await ((Node)NGame.Instance).AwaitProcessFrame();
            }

            if (ReferenceEquals(_service, service))
            {
                BroadcastHostRoster();
            }
        }
        catch (Exception exception)
        {
            Log.Warn($"[{ModInfo.Id}] Room roster broadcast debounce failed: {exception.Message}");
        }
        finally
        {
            _broadcastScheduled = false;
        }
    }

    private static RoomRosterSnapshotMessage CreateSnapshot()
    {
        return new RoomRosterSnapshotMessage
        {
            IsRunning = RoomRoster.IsRunning,
            Members = RoomRoster.Members.Select(ToWire).ToList()
        };
    }

    private static void HandleSnapshot(RoomRosterSnapshotMessage message, ulong senderId)
    {
        if (_service?.Type == NetGameType.Host)
        {
            Log.Warn($"[{ModInfo.Id}] Ignored unexpected room roster snapshot from {senderId} on host.");
            return;
        }

        RoomRoster.ApplyNetworkSnapshot(message.Members.Select(ToRoomMember), message.IsRunning);
    }

    private static RoomMemberWire ToWire(RoomMember member)
    {
        return new RoomMemberWire
        {
            NetId = member.NetId,
            Role = member.Role,
            HasCharacter = member.Character is not null,
            CharacterCategory = member.Character?.Id.Category ?? string.Empty,
            CharacterEntry = member.Character?.Id.Entry ?? string.Empty,
            HasObservedPlayer = member.ObservedPlayerNetId.HasValue,
            ObservedPlayerNetId = member.ObservedPlayerNetId ?? 0,
            IsConnected = member.IsConnected,
            IsReady = member.IsReady
        };
    }

    private static RoomMember ToRoomMember(RoomMemberWire member)
    {
        return new RoomMember(
            member.NetId,
            RoomRoster.GetDisplayName(member.NetId),
            member.Role,
            ResolveCharacter(member),
            member.HasObservedPlayer ? member.ObservedPlayerNetId : null,
            member.IsConnected,
            member.IsReady);
    }

    private static CharacterModel? ResolveCharacter(RoomMemberWire member)
    {
        if (!member.HasCharacter)
        {
            return null;
        }

        try
        {
            return ModelDb.GetByIdOrNull<CharacterModel>(new ModelId(member.CharacterCategory, member.CharacterEntry));
        }
        catch (Exception exception)
        {
            Log.Warn($"[{ModInfo.Id}] Could not resolve roster character {member.CharacterCategory}.{member.CharacterEntry}: {exception.Message}");
            return null;
        }
    }
}
