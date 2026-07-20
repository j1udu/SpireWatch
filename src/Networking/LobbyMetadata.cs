using System.Collections.Generic;

namespace SpireWatch.Networking;

internal enum LobbyPhase
{
    Lobby,
    Running
}

/// <summary>Stable keys written to the existing Steam lobby; no parallel lobby is created.</summary>
internal static class LobbyMetadata
{
    internal const string EnabledKey = "spirewatch";
    internal const string PhaseKey = "phase";
    internal const string ProtocolKey = "protocol";
    internal const string ModVersionKey = "mod_version";
    internal const string SpectatorCountKey = "spectator_count";

    internal static IReadOnlyDictionary<string, string> Create(LobbyPhase phase, string modVersion, int spectatorCount)
    {
        return new Dictionary<string, string>
        {
            [EnabledKey] = "1",
            [PhaseKey] = phase == LobbyPhase.Running ? "running" : "lobby",
            [ProtocolKey] = ModInfo.ProtocolVersion.ToString(),
            [ModVersionKey] = modVersion,
            [SpectatorCountKey] = spectatorCount.ToString()
        };
    }
}
