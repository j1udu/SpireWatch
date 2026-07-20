using System.Collections.Generic;

namespace SpireWatch.Spectating;

internal static class SpectatorRegistry
{
    private static readonly Dictionary<ulong, ulong> ObservedPlayersBySpectator = new();

    internal static bool IsLocalSpectator { get; private set; }

    internal static void BeginLocalSpectating()
    {
        IsLocalSpectator = true;
    }

    internal static void AddHostSpectator(ulong spectatorNetId, ulong observedPlayerNetId)
    {
        ObservedPlayersBySpectator[spectatorNetId] = observedPlayerNetId;
    }

    internal static bool IsHostSpectator(ulong spectatorNetId)
    {
        return ObservedPlayersBySpectator.ContainsKey(spectatorNetId);
    }

    internal static void RemoveHostSpectator(ulong spectatorNetId)
    {
        ObservedPlayersBySpectator.Remove(spectatorNetId);
    }

    internal static int HostSpectatorCount => ObservedPlayersBySpectator.Count;
}
