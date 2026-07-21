using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace SpireWatch.Spectating;

internal static class SpectatorJoinSafety
{
    internal static bool IsSafeHostJoinPoint(out string reason)
    {
        if (!RunManager.Instance.IsInProgress || RunManager.Instance.NetService?.Type != NetGameType.Host)
        {
            reason = "host run is not active";
            return false;
        }

        if (CombatManager.Instance.IsInProgress)
        {
            reason = "combat is active";
            return false;
        }

        if (RunManager.Instance.DebugOnlyGetState()?.CurrentRoom is null)
        {
            reason = "run has no serializable room state";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
