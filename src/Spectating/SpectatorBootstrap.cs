using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;

namespace SpireWatch.Spectating;

internal static class SpectatorBootstrap
{
    internal static void InstallOn(NGame? game)
    {
        if (game is null || game.GetNodeOrNull<SpectatorBootstrapNode>("SpireWatchSpectatorBootstrap") is not null)
        {
            return;
        }

        game.AddChild(new SpectatorBootstrapNode());
    }
}

internal sealed class SpectatorBootstrapNode : Node
{
    private bool _started;

    internal SpectatorBootstrapNode()
    {
        Name = "SpireWatchSpectatorBootstrap";
    }

    public override void _Process(double delta)
    {
        if (_started || !CommandLineHelper.TryGetValue("spirewatch-spectate", out var hostSteamIdText))
        {
            return;
        }

        _started = true;
        if (!ulong.TryParse(hostSteamIdText, out var hostSteamId))
        {
            Log.Error($"[{ModInfo.Id}] spirewatch-spectate must contain a Steam64 ID.");
            return;
        }

        TaskHelper.RunSafely(StartSpectating(hostSteamId, GetTree()));
    }

    private static async Task StartSpectating(ulong hostSteamId, SceneTree tree)
    {
        try
        {
            await SpectatorJoinFlow.JoinAsync(hostSteamId, tree);
        }
        catch (Exception exception)
        {
            Log.Error($"[{ModInfo.Id}] Spectator join failed: {exception}");
        }
    }
}
