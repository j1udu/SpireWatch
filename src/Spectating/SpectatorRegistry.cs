using System.Collections.Generic;
using System;

namespace SpireWatch.Spectating;

internal static class SpectatorRegistry
{
    private static readonly Dictionary<ulong, SpectatorSession> SessionsBySpectator = new();

    internal static bool IsLocalSpectator { get; private set; }

    internal static void BeginLocalSpectating()
    {
        IsLocalSpectator = true;
    }

    internal static void EndLocalSpectating()
    {
        IsLocalSpectator = false;
    }

    internal static void AddHostSpectator(ulong spectatorNetId, ulong observedPlayerNetId)
    {
        SessionsBySpectator[spectatorNetId] = new SpectatorSession(
            spectatorNetId,
            observedPlayerNetId,
            DateTimeOffset.UtcNow);
    }

    internal static bool IsHostSpectator(ulong spectatorNetId)
    {
        return SessionsBySpectator.ContainsKey(spectatorNetId);
    }

    internal static void RemoveHostSpectator(ulong spectatorNetId)
    {
        SessionsBySpectator.Remove(spectatorNetId);
    }

    internal static void ClearHostSpectators()
    {
        SessionsBySpectator.Clear();
    }

    internal static int HostSpectatorCount => SessionsBySpectator.Count;
}

/// <summary>
/// Mod-owned host record. ProjectedPlayerNetId exists only for the current vanilla UI recovery bridge;
/// it is not inserted into RunState.Players and must be removed when an independent spectator view exists.
/// </summary>
internal sealed record SpectatorSession(
    ulong SpectatorNetId,
    ulong ProjectedPlayerNetId,
    DateTimeOffset JoinedAt);
