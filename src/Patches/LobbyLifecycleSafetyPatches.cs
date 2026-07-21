using System.Reflection;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using SpireWatch.Networking;

namespace SpireWatch.Patches;

/// <summary>
/// Keeps the vanilla Steam lobby open only while the local host has an active run.
/// Outside that narrow window SteamHost retains its original close behavior.
/// </summary>
[HarmonyPatch]
internal static class SteamHostCloseDuringRunPatch
{
    private static int _cleanupInProgress;

    private static MethodBase? TargetMethod()
    {
        return AccessTools.Method("MegaCrit.Sts2.Core.Multiplayer.Transport.Steam.SteamHost:SetHostIsClosed");
    }

    private static bool Prefix(bool isClosed)
    {
        return !isClosed || IsCleanupInProgress || !IsActiveHostRun();
    }

    internal static void EnterCleanup() => Interlocked.Increment(ref _cleanupInProgress);

    internal static void ExitCleanup() => Interlocked.Decrement(ref _cleanupInProgress);

    private static bool IsCleanupInProgress => Volatile.Read(ref _cleanupInProgress) > 0;

    private static bool IsActiveHostRun()
    {
        try
        {
            return RunManager.Instance.IsInProgress &&
                   RunManager.Instance.NetService?.Type == NetGameType.Host;
        }
        catch
        {
            // Preserve vanilla cleanup if the run manager is unavailable or changes across a game version.
            return false;
        }
    }
}

/// <summary>Removes the running marker before vanilla run cleanup can leave a stale friends lobby behind.</summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp), typeof(bool))]
internal static class ClearRunningLobbyMetadataPatch
{
    private static void Prefix(RunManager __instance)
    {
        SteamHostCloseDuringRunPatch.EnterCleanup();
        if (__instance.NetService?.Type == NetGameType.Host)
        {
            SteamLobbyMetadataPublisher.TryPublish(__instance.NetService, LobbyPhase.Closed);
        }
    }

    private static System.Exception? Finalizer(System.Exception? __exception)
    {
        SteamHostCloseDuringRunPatch.ExitCleanup();
        return __exception;
    }
}
