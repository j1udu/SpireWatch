using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace SpireWatch;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        Log.Info($"[{ModInfo.Id}] Initializing protocol v{ModInfo.ProtocolVersion}.");

        var harmony = new Harmony(ModInfo.HarmonyId);
        harmony.PatchAll(typeof(ModEntry).Assembly);
        Patches.RunningLobbyLifecyclePatch.TryInstall(harmony);

        Log.Info($"[{ModInfo.Id}] Phase 1 lobby metadata publisher is ready.");
    }
}
