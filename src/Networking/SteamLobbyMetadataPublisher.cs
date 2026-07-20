using System;
using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace SpireWatch.Networking;

/// <summary>
/// Writes metadata to the Steam lobby owned by the vanilla NetHostGameService.
/// Steamworks is deliberately reached by reflection so this mod does not ship or bind a second Steam transport.
/// </summary>
internal static class SteamLobbyMetadataPublisher
{
    private static readonly string ModVersion = typeof(SteamLobbyMetadataPublisher).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    internal static bool TryPublish(INetGameService netService, LobbyPhase phase, int spectatorCount = 0)
    {
        if (netService is not NetHostGameService hostService)
        {
            return false;
        }

        try
        {
            var lobbyId = AccessTools.Property(hostService.NetHost?.GetType(), "LobbyId")?.GetValue(hostService.NetHost);
            if (lobbyId is null)
            {
                Log.Warn($"[{ModInfo.Id}] Steam LobbyId is not available; metadata was not published.");
                return false;
            }

            var matchmakingType = lobbyId.GetType().Assembly.GetType("Steamworks.SteamMatchmaking");
            var setLobbyData = matchmakingType?.GetMethod("SetLobbyData", BindingFlags.Public | BindingFlags.Static);
            if (setLobbyData is null)
            {
                Log.Warn($"[{ModInfo.Id}] SteamMatchmaking.SetLobbyData was not found; metadata was not published.");
                return false;
            }

            foreach (var entry in LobbyMetadata.Create(phase, ModVersion, spectatorCount))
            {
                setLobbyData.Invoke(null, new[] { lobbyId, entry.Key, entry.Value });
            }

            Log.Info($"[{ModInfo.Id}] Published lobby metadata: phase={phase.ToString().ToLowerInvariant()}, spectators={spectatorCount}.");
            return true;
        }
        catch (Exception exception)
        {
            Log.Warn($"[{ModInfo.Id}] Failed to publish Steam lobby metadata: {exception.Message}");
            return false;
        }
    }
}
