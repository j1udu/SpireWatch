using System;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Quality;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Platform;

namespace SpireWatch.Spectating;

internal sealed class ReadOnlySpectatorNetGameService : INetGameService
{
    private readonly INetGameService _inner;
    private bool _suppressNextDisconnect;

    internal ReadOnlySpectatorNetGameService(INetGameService inner, ulong projectedPlayerNetId)
    {
        _inner = inner;
        // The current vanilla LoadRunLobby API requires a player-facing NetId to construct its UI.
        // This bridge is isolated here so it can be replaced by an independent spectator view.
        NetId = projectedPlayerNetId;
    }

    public ulong NetId { get; }

    public bool IsConnected => _inner.IsConnected;

    public bool IsGameLoading => _inner.IsGameLoading;

    public NetGameType Type => _inner.Type;

    public PlatformType Platform => _inner.Platform;

    public event Action<NetErrorInfo>? Disconnected
    {
        add => _inner.Disconnected += value;
        remove => _inner.Disconnected -= value;
    }

    public void SendMessage<T>(T message, ulong playerId) where T : INetMessage
    {
        Log.Info($"[{ModInfo.Id}] Rejected spectator message {message.GetType().Name} to {playerId}.");
    }

    public void SendMessage<T>(T message) where T : INetMessage
    {
        Log.Info($"[{ModInfo.Id}] Rejected spectator message {message.GetType().Name}.");
    }

    public void RegisterMessageHandler<T>(MessageHandlerDelegate<T> messageHandlerDelegate) where T : INetMessage
    {
        _inner.RegisterMessageHandler(messageHandlerDelegate);
    }

    public void UnregisterMessageHandler<T>(MessageHandlerDelegate<T> messageHandlerDelegate) where T : INetMessage
    {
        _inner.UnregisterMessageHandler(messageHandlerDelegate);
    }

    public void Update()
    {
        _inner.Update();
    }

    public void Disconnect(NetError reason, bool now = false)
    {
        if (_suppressNextDisconnect)
        {
            _suppressNextDisconnect = false;
            Log.Info($"[{ModInfo.Id}] Preserved spectator transport during local snapshot reload.");
            return;
        }

        _inner.Disconnect(reason, now);
    }

    internal void SuppressNextDisconnect()
    {
        _suppressNextDisconnect = true;
    }

    public ConnectionStats? GetStatsForPeer(ulong peerId)
    {
        return _inner.GetStatsForPeer(peerId);
    }

    public void SetGameLoading(bool isLoading)
    {
        _inner.SetGameLoading(isLoading);
    }

    public void SetBufferMessages(bool bufferMessages)
    {
        _inner.SetBufferMessages(bufferMessages);
    }

    public string? GetRawLobbyIdentifier()
    {
        return _inner.GetRawLobbyIdentifier();
    }
}
