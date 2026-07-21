using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Logging;
using Steamworks;

namespace SpireWatch.Spectating;

internal static class SteamFriendLobbyMetadata
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);

    internal static async Task<bool> IsRunningCompatibleSession(ulong friendSteamId)
    {
        var friendId = new CSteamID(friendSteamId);
        if (!SteamFriends.GetFriendGamePlayed(friendId, out var gameInfo) || !gameInfo.m_steamIDLobby.IsValid())
        {
            Log.Info($"[{ModInfo.Id}] Friend {friendSteamId} has no valid Steam lobby metadata.");
            return false;
        }

        var lobbyId = gameInfo.m_steamIDLobby;
        if (!HasRunningCompatibleMetadata(lobbyId))
        {
            await RequestLobbyData(lobbyId);
        }

        var enabled = SteamMatchmaking.GetLobbyData(lobbyId, Networking.LobbyMetadata.EnabledKey);
        var phase = SteamMatchmaking.GetLobbyData(lobbyId, Networking.LobbyMetadata.PhaseKey);
        var protocol = SteamMatchmaking.GetLobbyData(lobbyId, Networking.LobbyMetadata.ProtocolKey);
        var modVersion = SteamMatchmaking.GetLobbyData(lobbyId, Networking.LobbyMetadata.ModVersionKey);
        var isCompatible = enabled == "1" &&
                           phase == "running" &&
                           protocol == ModInfo.ProtocolVersion.ToString() &&
                           modVersion == ModInfo.Version;
        Log.Info($"[{ModInfo.Id}] Friend {friendSteamId} lobby {lobbyId.m_SteamID}: spirewatch='{enabled}', phase='{phase}', protocol='{protocol}', modVersion='{modVersion}', spectator={isCompatible}.");
        return isCompatible;
    }

    private static bool HasRunningCompatibleMetadata(CSteamID lobbyId)
    {
        return SteamMatchmaking.GetLobbyData(lobbyId, Networking.LobbyMetadata.EnabledKey) == "1"
            && SteamMatchmaking.GetLobbyData(lobbyId, Networking.LobbyMetadata.PhaseKey) == "running"
            && SteamMatchmaking.GetLobbyData(lobbyId, Networking.LobbyMetadata.ProtocolKey) == ModInfo.ProtocolVersion.ToString()
            && SteamMatchmaking.GetLobbyData(lobbyId, Networking.LobbyMetadata.ModVersionKey) == ModInfo.Version;
    }

    private static async Task RequestLobbyData(CSteamID lobbyId)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var callback = Callback<LobbyDataUpdate_t>.Create(update =>
        {
            if (update.m_ulSteamIDLobby == lobbyId.m_SteamID)
            {
                completion.TrySetResult(update.m_bSuccess != 0);
            }
        });

        if (!SteamMatchmaking.RequestLobbyData(lobbyId))
        {
            return;
        }

        if (await Task.WhenAny(completion.Task, Task.Delay(RequestTimeout)) != completion.Task)
        {
            Log.Warn($"[{ModInfo.Id}] Timed out reading Steam lobby metadata for {lobbyId.m_SteamID}.");
        }
    }
}
