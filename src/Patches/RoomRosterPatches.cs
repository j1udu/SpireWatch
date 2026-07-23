using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using SpireWatch.Rooms;

namespace SpireWatch.Patches;

[HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor,
    typeof(MegaCrit.Sts2.Core.Runs.GameMode),
    typeof(MegaCrit.Sts2.Core.Multiplayer.Game.INetGameService),
    typeof(IStartRunLobbyListener), typeof(int))]
internal static class StartRunLobbyRoomRosterPatch
{
    private static void Postfix(StartRunLobby __instance)
    {
        RoomRoster.BindWaitingLobby(__instance);
    }
}

/// <summary>
/// StartRunLobby has no roster-changed event for ready and character updates. These postfixes only
/// refresh the local UI mirror after the original handler has changed its authoritative Players list.
/// </summary>
[HarmonyPatch(typeof(StartRunLobby), "ChangeCharacter")]
internal static class StartRunLobbyCharacterRosterPatch
{
    private static void Postfix()
    {
        RoomRoster.RefreshWaitingLobby();
    }
}

[HarmonyPatch(typeof(StartRunLobby), "HandlePlayerReadyMessage")]
internal static class StartRunLobbyReadyRosterPatch
{
    private static void Postfix()
    {
        RoomRoster.RefreshWaitingLobby();
    }
}

[HarmonyPatch(typeof(StartRunLobby), "AddLocalHostPlayerInternal")]
internal static class StartRunLobbyHostPlayerRosterPatch
{
    private static void Postfix()
    {
        RoomRoster.RefreshWaitingLobby();
    }
}

[HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.CleanUp))]
internal static class StartRunLobbyRoomRosterCleanupPatch
{
    private static void Postfix()
    {
        RoomRoster.Clear();
    }
}
