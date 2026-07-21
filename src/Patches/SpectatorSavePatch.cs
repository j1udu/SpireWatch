using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using SpireWatch.Spectating;

namespace SpireWatch.Patches;

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.IncrementNumReloads))]
internal static class SpectatorSavePatch
{
    private static bool Prefix(ref Task __result)
    {
        if (!SpectatorRegistry.IsLocalSpectator)
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }
}
