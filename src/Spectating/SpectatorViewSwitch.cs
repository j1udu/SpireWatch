using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using SpireWatch.Networking;

namespace SpireWatch.Spectating;

/// <summary>
/// A view switch reloads the spectator projection from a fresh host snapshot. It deliberately keeps
/// the existing Steam connection and never changes the host's real player collection.
/// </summary>
internal static class SpectatorViewSwitch
{
    private static readonly TimeSpan SwitchCooldown = TimeSpan.FromSeconds(3);
    private static INetGameService? _hostService;
    private static INetGameService? _clientService;
    private static ulong _hostSteamId;
    private static DateTimeOffset _nextSwitchAllowedAt;
    private static bool _switchInFlight;

    internal static bool CanRequest => SpectatorRegistry.IsLocalSpectator &&
                                       _clientService is not null &&
                                       !_switchInFlight &&
                                       DateTimeOffset.UtcNow >= _nextSwitchAllowedAt;

    internal static void EnsureHostBound(INetGameService service)
    {
        if (ReferenceEquals(_hostService, service))
        {
            return;
        }

        if (_hostService is not null)
        {
            _hostService.UnregisterMessageHandler<SpectatorSwitchTargetRequestMessage>(HandleSwitchRequest);
        }

        _hostService = service;
        service.RegisterMessageHandler<SpectatorSwitchTargetRequestMessage>(HandleSwitchRequest);
    }

    internal static void BindClient(INetGameService service, ulong hostSteamId)
    {
        if (!ReferenceEquals(_clientService, service))
        {
            if (_clientService is not null)
            {
                _clientService.UnregisterMessageHandler<SpectatorSwitchTargetResponseMessage>(HandleSwitchResponse);
            }

            _clientService = service;
            service.RegisterMessageHandler<SpectatorSwitchTargetResponseMessage>(HandleSwitchResponse);
        }

        _hostSteamId = hostSteamId;
    }

    internal static void EndLocalSession()
    {
        if (_clientService is not null)
        {
            _clientService.UnregisterMessageHandler<SpectatorSwitchTargetResponseMessage>(HandleSwitchResponse);
        }

        _clientService = null;
        _hostSteamId = 0;
        _switchInFlight = false;
    }

    internal static bool Request(ulong playerNetId)
    {
        if (!CanRequest || _clientService is null)
        {
            return false;
        }

        _switchInFlight = true;
        _nextSwitchAllowedAt = DateTimeOffset.UtcNow + SwitchCooldown;
        _clientService.SendMessage(new SpectatorSwitchTargetRequestMessage { RequestedPlayerNetId = playerNetId });
        return true;
    }

    private static void HandleSwitchRequest(SpectatorSwitchTargetRequestMessage message, ulong senderId)
    {
        if (_hostService is not NetHostGameService hostService || !SpectatorRegistry.IsHostSpectator(senderId))
        {
            return;
        }

        var state = RunManager.Instance.DebugOnlyGetState();
        if (state?.Players.FirstOrDefault(player => player.NetId == message.RequestedPlayerNetId) is null)
        {
            SendFailure(hostService, senderId, "目标玩家已不在当前房间中。");
            return;
        }

        if (!SpectatorJoinSafety.IsSafeHostJoinPoint(out var safetyFailure))
        {
            SendFailure(hostService, senderId, $"当前不可切换：{safetyFailure}。");
            return;
        }

        SpectatorRegistry.AddHostSpectator(senderId, message.RequestedPlayerNetId);
        RoomRosterCoordinator.RefreshHostRunningRoster();
        hostService.SendMessage(new SpectatorSwitchTargetResponseMessage
        {
            Accepted = true,
            NewObservedPlayerNetId = message.RequestedPlayerNetId,
            SerializableRun = RunManager.Instance.ToSave(state.CurrentRoom)
        }, senderId);
    }

    private static void SendFailure(NetHostGameService hostService, ulong senderId, string reason)
    {
        hostService.SendMessage(new SpectatorSwitchTargetResponseMessage
        {
            Accepted = false,
            FailureReason = reason
        }, senderId);
    }

    private static void HandleSwitchResponse(SpectatorSwitchTargetResponseMessage message, ulong senderId)
    {
        if (senderId != _hostSteamId)
        {
            Log.Warn($"[{ModInfo.Id}] Ignored spectator switch response from unexpected peer {senderId}.");
            return;
        }

        _switchInFlight = false;
        if (!message.Accepted)
        {
            Log.Warn($"[{ModInfo.Id}] Spectator view switch refused: {message.FailureReason}");
            return;
        }

        TaskHelper.RunSafely(ReloadAsync(message.SerializableRun, message.NewObservedPlayerNetId));
    }

    private static async Task ReloadAsync(MegaCrit.Sts2.Core.Saves.SerializableRun snapshot, ulong observedPlayerNetId)
    {
        if (_clientService is null)
        {
            return;
        }

        try
        {
            if (RunManager.Instance.IsInProgress)
            {
                if (RunManager.Instance.NetService is ReadOnlySpectatorNetGameService readOnlyService)
                {
                    readOnlyService.SuppressNextDisconnect();
                }

                RunManager.Instance.CleanUp(graceful: false);
            }

            await SpectatorJoinFlow.LoadSnapshotAsync(_clientService, _hostSteamId, snapshot, observedPlayerNetId);
            Log.Info($"[{ModInfo.Id}] Spectator view switched to {observedPlayerNetId}.");
        }
        catch (Exception exception)
        {
            Log.Error($"[{ModInfo.Id}] Spectator view switch failed: {exception}");
            EndLocalSession();
            SpectatorRegistry.EndLocalSpectating();
        }
    }
}
