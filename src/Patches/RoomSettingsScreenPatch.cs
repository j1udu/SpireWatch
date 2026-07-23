using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SpireWatch.Rooms.UI;

namespace SpireWatch.Patches;

/// <summary>Installs the room tab only after the original settings manager has registered its tabs.</summary>
[HarmonyPatch(typeof(NSettingsTabManager), nameof(NSettingsTabManager._Ready))]
internal static class RoomSettingsScreenPatch
{
    private static void Postfix(NSettingsTabManager __instance)
    {
        RoomSettingsSection.Install(__instance);
    }
}
