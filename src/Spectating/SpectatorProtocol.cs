using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace SpireWatch.Spectating;

/// <summary>Small protocol challenge that proves the peer speaks the current SpireWatch wire version.</summary>
internal static class SpectatorProtocol
{
    private static readonly HashSet<ulong> ValidatedHostPeers = new();
    private static INetGameService? _hostService;
    private static INetGameService? _clientService;

    internal static void EnsureHostBound(INetGameService service)
    {
        if (ReferenceEquals(_hostService, service))
        {
            return;
        }

        if (_hostService is not null)
        {
            _hostService.UnregisterMessageHandler<SpectatorHelloMessage>(HandleHello);
        }

        _hostService = service;
        ValidatedHostPeers.Clear();
        service.RegisterMessageHandler<SpectatorHelloMessage>(HandleHello);
    }

    internal static void BindClient(INetGameService service)
    {
        if (ReferenceEquals(_clientService, service))
        {
            return;
        }

        if (_clientService is not null)
        {
            _clientService.UnregisterMessageHandler<SpectatorChallengeMessage>(HandleChallenge);
        }

        _clientService = service;
        service.RegisterMessageHandler<SpectatorChallengeMessage>(HandleChallenge);
    }

    internal static void UnbindClient(INetGameService service)
    {
        if (!ReferenceEquals(_clientService, service))
        {
            return;
        }

        service.UnregisterMessageHandler<SpectatorChallengeMessage>(HandleChallenge);
        _clientService = null;
    }

    internal static bool TryAuthorizeHostPeer(ulong peerId, out string failure)
    {
        if (ValidatedHostPeers.Remove(peerId))
        {
            failure = string.Empty;
            return true;
        }

        failure = "missing or incompatible SpireWatch protocol handshake";
        return false;
    }

    internal static void ForgetHostPeer(ulong peerId)
    {
        ValidatedHostPeers.Remove(peerId);
    }

    private static void HandleChallenge(SpectatorChallengeMessage message, ulong senderId)
    {
        _clientService?.SendMessage(new SpectatorHelloMessage { ProtocolVersion = ModInfo.ProtocolVersion });
    }

    private static void HandleHello(SpectatorHelloMessage message, ulong senderId)
    {
        if (message.ProtocolVersion != ModInfo.ProtocolVersion)
        {
            Log.Warn($"[{ModInfo.Id}] Protocol mismatch from {senderId}: remote={message.ProtocolVersion}, local={ModInfo.ProtocolVersion}.");
            ValidatedHostPeers.Remove(senderId);
            return;
        }

        ValidatedHostPeers.Add(senderId);
    }
}

public struct SpectatorChallengeMessage : INetMessage, IPacketSerializable
{
    public readonly bool ShouldBroadcast => false;
    public readonly NetTransferMode Mode => NetTransferMode.Reliable;
    public readonly LogLevel LogLevel => LogLevel.Info;
    public readonly void Serialize(PacketWriter writer) { }
    public void Deserialize(PacketReader reader) { }
}

public struct SpectatorHelloMessage : INetMessage, IPacketSerializable
{
    public int ProtocolVersion;
    public readonly bool ShouldBroadcast => false;
    public readonly NetTransferMode Mode => NetTransferMode.Reliable;
    public readonly LogLevel LogLevel => LogLevel.Info;
    public readonly void Serialize(PacketWriter writer) => writer.WriteInt(ProtocolVersion, 8);
    public void Deserialize(PacketReader reader) => ProtocolVersion = reader.ReadInt(8);
}
