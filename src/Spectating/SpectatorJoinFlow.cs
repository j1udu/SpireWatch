using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace SpireWatch.Spectating;

internal static class SpectatorJoinFlow
{
    internal static async Task JoinAsync(ulong hostSteamId, SceneTree tree)
    {
        if (RunManager.Instance.IsInProgress)
        {
            throw new InvalidOperationException("Cannot begin spectating while a run is already active.");
        }

        var networkService = new NetClientGameService();
        var joinFlow = new JoinFlow(networkService);
        var joinResult = await joinFlow.Begin(SteamClientConnectionInitializer.FromPlayer(hostSteamId), tree);
        if (joinResult.sessionState != RunSessionState.Running || joinResult.rejoinResponse is null)
        {
            networkService.Disconnect(NetError.InvalidJoin);
            throw new InvalidOperationException("The selected Steam friend is not in a running SpireWatch session.");
        }

        var rejoinResponse = joinResult.rejoinResponse.Value;
        var observedPlayer = rejoinResponse.serializableRun.Players.FirstOrDefault();
        if (observedPlayer is null)
        {
            networkService.Disconnect(NetError.InvalidJoin);
            throw new InvalidOperationException("The host sent a run snapshot without players.");
        }

        SpectatorRegistry.BeginLocalSpectating();
        var spectatorService = new ReadOnlySpectatorNetGameService(networkService, observedPlayer.NetId);
        var loadLobby = new LoadRunLobby(spectatorService, new SpectatorLoadRunLobbyListener(), rejoinResponse.serializableRun);
        var runWasLoaded = false;
        var setupStarted = false;
        var stage = "deserializing snapshot";
        try
        {
            Log.Info($"[{ModInfo.Id}] Received spectator snapshot from {hostSteamId}; loading as player {observedPlayer.NetId}.");
            var runState = RunState.FromSerializable(rejoinResponse.serializableRun);
            stage = "setting up multiplayer state";
            setupStarted = true;
            Log.Info($"[{ModInfo.Id}] Spectator stage: {stage}.");
            await RunManager.Instance.SetUpSavedMultiplayer(runState, loadLobby);
            stage = "configuring read-only state";
            Log.Info($"[{ModInfo.Id}] Spectator stage: {stage}.");
            AccessTools.PropertySetter(typeof(RunManager), nameof(RunManager.ShouldSave))?.Invoke(RunManager.Instance, new object[] { false });
            RunManager.Instance.CombatStateSynchronizer.IsDisabled = true;
            RunManager.Instance.CombatReplayWriter.IsEnabled = false;
            var game = NGame.Instance ?? throw new InvalidOperationException("NGame.Instance is not available.");
            stage = "loading run scene";
            Log.Info($"[{ModInfo.Id}] Spectator stage: {stage}.");
            await game.LoadRun(runState, rejoinResponse.serializableRun.PreFinishedRoom);
            runWasLoaded = true;
            Log.Info($"[{ModInfo.Id}] Spectating host {hostSteamId} as player {observedPlayer.NetId}.");
        }
        catch (Exception exception)
        {
            Log.Error($"[{ModInfo.Id}] Spectator failed while {stage}: {exception}");
            if (setupStarted && RunManager.Instance.IsInProgress)
            {
                try
                {
                    RunManager.Instance.CleanUp(graceful: false);
                }
                catch (Exception cleanupException)
                {
                    Log.Error($"[{ModInfo.Id}] Spectator cleanup failed: {cleanupException}");
                }
            }
            throw;
        }
        finally
        {
            loadLobby.CleanUp(disconnectSession: !runWasLoaded, error: NetError.InternalError);
        }
    }
}

internal sealed class SpectatorLoadRunLobbyListener : ILoadRunLobbyListener
{
    public void PlayerConnected(ulong playerId)
    {
    }

    public void RemotePlayerDisconnected(ulong playerId)
    {
    }

    public Task<bool> ShouldAllowRunToBegin()
    {
        return Task.FromResult(false);
    }

    public void BeginRun()
    {
    }

    public void PlayerReadyChanged(ulong playerId)
    {
    }

    public void LocalPlayerDisconnected(MegaCrit.Sts2.Core.Entities.Multiplayer.NetErrorInfo info)
    {
    }
}
