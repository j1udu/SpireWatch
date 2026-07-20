using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using SpireWatch.Spectating;

namespace SpireWatch;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        Log.Info($"[{ModInfo.Id}] Initializing protocol v{ModInfo.ProtocolVersion}.");

        var harmony = new Harmony(ModInfo.HarmonyId);
        harmony.PatchAll(typeof(ModEntry).Assembly);

        SpectatorBootstrap.InstallOn(NGame.Instance);

        Log.Info($"[{ModInfo.Id}] Lobby metadata publisher is ready for STS2 v0.109.0.");
    }
}
