using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using SpireWatch.Spectating;

namespace SpireWatch.Patches;

[HarmonyPatch(typeof(NClickableControl), nameof(NClickableControl.ForceClick))]
internal static class SpectatorForcedClickPatch
{
    private static bool Prefix()
    {
        return !SpectatorRegistry.IsLocalSpectator;
    }
}

[HarmonyPatch(typeof(NPlayerHand), "StartCardPlay")]
internal static class SpectatorHandCardPlayPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("hand card drag");
    }
}

[HarmonyPatch(typeof(NPlayerHand), "SelectCardInSimpleMode")]
internal static class SpectatorHandSimpleSelectionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("hand card selection");
    }
}

[HarmonyPatch(typeof(NPlayerHand), "SelectCardInUpgradeMode")]
internal static class SpectatorHandUpgradeSelectionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("hand card upgrade selection");
    }
}

[HarmonyPatch(typeof(NPlayerHand), "OnSelectModeConfirmButtonPressed")]
internal static class SpectatorHandSelectionConfirmPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("hand card selection confirmation");
    }
}

[HarmonyPatch(typeof(ActionQueueSynchronizer), nameof(ActionQueueSynchronizer.RequestEnqueue))]
internal static class SpectatorActionRequestPatch
{
    private static bool Prefix()
    {
        return RejectLocalAction("action request");
    }

    internal static bool RejectLocalAction(string action)
    {
        if (!SpectatorRegistry.IsLocalSpectator)
        {
            return true;
        }

        Log.Info($"[{ModInfo.Id}] Rejected spectator {action}.");
        return false;
    }
}

[HarmonyPatch(typeof(ActionQueueSynchronizer), nameof(ActionQueueSynchronizer.RequestEnqueueHookAction))]
internal static class SpectatorHookActionRequestPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("hook action request");
    }
}

[HarmonyPatch(typeof(ActionQueueSynchronizer), nameof(ActionQueueSynchronizer.RequestResumeActionAfterPlayerChoice))]
internal static class SpectatorActionResumeRequestPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("action resume request");
    }
}

[HarmonyPatch(typeof(PlayerChoiceSynchronizer), nameof(PlayerChoiceSynchronizer.SyncLocalChoice))]
internal static class SpectatorPlayerChoicePatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("player choice");
    }
}

[HarmonyPatch(typeof(EventSynchronizer), nameof(EventSynchronizer.ChooseLocalOption))]
internal static class SpectatorEventChoicePatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("event option");
    }
}

[HarmonyPatch(typeof(RestSiteSynchronizer), nameof(RestSiteSynchronizer.ChooseLocalOption))]
internal static class SpectatorRestSiteChoicePatch
{
    private static bool Prefix(ref Task<bool> __result)
    {
        if (!SpectatorRegistry.IsLocalSpectator)
        {
            return true;
        }

        __result = Task.FromResult(false);
        return SpectatorActionRequestPatch.RejectLocalAction("rest site option");
    }
}

[HarmonyPatch(typeof(RestSiteSynchronizer), nameof(RestSiteSynchronizer.LocalOptionHovered))]
internal static class SpectatorRestSiteHoverPatch
{
    private static bool Prefix()
    {
        return !SpectatorRegistry.IsLocalSpectator;
    }
}

[HarmonyPatch(typeof(RewardsSetSynchronizer), nameof(RewardsSetSynchronizer.SelectLocalReward))]
internal static class SpectatorRewardSelectionPatch
{
    private static bool Prefix(ref Task<bool> __result)
    {
        if (!SpectatorRegistry.IsLocalSpectator)
        {
            return true;
        }

        __result = Task.FromResult(false);
        return SpectatorActionRequestPatch.RejectLocalAction("reward selection");
    }
}

[HarmonyPatch(typeof(RewardsSetSynchronizer), nameof(RewardsSetSynchronizer.SkipLocalRewardsSet))]
internal static class SpectatorRewardSkipPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("reward skip");
    }
}

[HarmonyPatch(typeof(NRewardButton), "GetReward")]
internal static class SpectatorRewardButtonPatch
{
    private static bool Prefix(ref Task __result)
    {
        if (!SpectatorRegistry.IsLocalSpectator)
        {
            return true;
        }

        __result = Task.CompletedTask;
        return SpectatorActionRequestPatch.RejectLocalAction("reward button");
    }
}

[HarmonyPatch(typeof(NCardRewardSelectionScreen), "SelectCard")]
internal static class SpectatorCardRewardChoicePatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("card reward selection");
    }
}

[HarmonyPatch(typeof(NCardRewardSelectionScreen), "OnAlternateRewardSelected")]
internal static class SpectatorCardRewardAlternativePatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("card reward alternative");
    }
}

[HarmonyPatch(typeof(NChooseACardSelectionScreen), "SelectHolder")]
internal static class SpectatorChooseOneCardPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("single card selection");
    }
}

[HarmonyPatch(typeof(NChooseACardSelectionScreen), "OnSkipButtonReleased")]
internal static class SpectatorChooseOneCardSkipPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("single card skip");
    }
}

[HarmonyPatch(typeof(NChooseABundleSelectionScreen), "OnBundleClicked")]
internal static class SpectatorChooseCardBundlePatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("card bundle selection");
    }
}

[HarmonyPatch(typeof(NChooseABundleSelectionScreen), "ConfirmSelection")]
internal static class SpectatorChooseCardBundleConfirmPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("card bundle confirmation");
    }
}

[HarmonyPatch(typeof(NCombatPileCardSelectScreen), "OnCardClicked")]
internal static class SpectatorCombatPileCardSelectionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("combat pile card selection");
    }
}

[HarmonyPatch(typeof(NCombatPileCardSelectScreen), "CompleteSelection")]
internal static class SpectatorCombatPileCardCompletionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("combat pile card confirmation");
    }
}

[HarmonyPatch(typeof(NSimpleCardSelectScreen), "OnCardClicked")]
internal static class SpectatorSimpleCardSelectionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("card selection");
    }
}

[HarmonyPatch(typeof(NSimpleCardSelectScreen), "CompleteSelection")]
internal static class SpectatorSimpleCardCompletionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("card selection confirmation");
    }
}

[HarmonyPatch(typeof(NDeckCardSelectScreen), "OnCardClicked")]
internal static class SpectatorDeckCardSelectionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck card selection");
    }
}

[HarmonyPatch(typeof(NDeckCardSelectScreen), "CloseSelection")]
internal static class SpectatorDeckCardClosePatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck card completion");
    }
}

[HarmonyPatch(typeof(NDeckCardSelectScreen), "ConfirmSelection")]
internal static class SpectatorDeckCardConfirmPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck card confirmation");
    }
}

[HarmonyPatch(typeof(NDeckUpgradeSelectScreen), "OnCardClicked")]
internal static class SpectatorDeckUpgradeSelectionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck upgrade selection");
    }
}

[HarmonyPatch(typeof(NDeckUpgradeSelectScreen), "CloseSelection")]
internal static class SpectatorDeckUpgradeClosePatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck upgrade completion");
    }
}

[HarmonyPatch(typeof(NDeckUpgradeSelectScreen), "ConfirmSelection")]
internal static class SpectatorDeckUpgradeConfirmPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck upgrade confirmation");
    }
}

[HarmonyPatch(typeof(NDeckEnchantSelectScreen), "OnCardClicked")]
internal static class SpectatorDeckEnchantSelectionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck enchantment selection");
    }
}

[HarmonyPatch(typeof(NDeckEnchantSelectScreen), "CloseSelection")]
internal static class SpectatorDeckEnchantClosePatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck enchantment completion");
    }
}

[HarmonyPatch(typeof(NDeckEnchantSelectScreen), "ConfirmSelection")]
internal static class SpectatorDeckEnchantConfirmPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck enchantment confirmation");
    }
}

[HarmonyPatch(typeof(NDeckTransformSelectScreen), "OnCardClicked")]
internal static class SpectatorDeckTransformSelectionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck transformation selection");
    }
}

[HarmonyPatch(typeof(NDeckTransformSelectScreen), "CloseSelection")]
internal static class SpectatorDeckTransformClosePatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck transformation completion");
    }
}

[HarmonyPatch(typeof(NDeckTransformSelectScreen), "ConfirmSelection")]
internal static class SpectatorDeckTransformConfirmPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck transformation confirmation");
    }
}

[HarmonyPatch(typeof(NDeckTransformSelectScreen), "CompleteSelection")]
internal static class SpectatorDeckTransformCompletionPatch
{
    private static bool Prefix()
    {
        return SpectatorActionRequestPatch.RejectLocalAction("deck transformation finalization");
    }
}

[HarmonyPatch(typeof(PeerInputSynchronizer), nameof(PeerInputSynchronizer.SyncLocalMousePos))]
internal static class SpectatorMousePositionPatch
{
    private static bool Prefix()
    {
        return !SpectatorRegistry.IsLocalSpectator;
    }
}

[HarmonyPatch(typeof(PeerInputSynchronizer), nameof(PeerInputSynchronizer.SyncLocalControllerFocus))]
internal static class SpectatorControllerFocusPatch
{
    private static bool Prefix()
    {
        return !SpectatorRegistry.IsLocalSpectator;
    }
}

[HarmonyPatch(typeof(PeerInputSynchronizer), nameof(PeerInputSynchronizer.SyncLocalIsUsingController))]
internal static class SpectatorControllerModePatch
{
    private static bool Prefix()
    {
        return !SpectatorRegistry.IsLocalSpectator;
    }
}

[HarmonyPatch(typeof(PeerInputSynchronizer), nameof(PeerInputSynchronizer.SyncLocalMouseDown))]
internal static class SpectatorMouseDownPatch
{
    private static bool Prefix()
    {
        return !SpectatorRegistry.IsLocalSpectator;
    }
}

[HarmonyPatch(typeof(PeerInputSynchronizer), nameof(PeerInputSynchronizer.SyncLocalScreen))]
internal static class SpectatorScreenPatch
{
    private static bool Prefix()
    {
        return !SpectatorRegistry.IsLocalSpectator;
    }
}

[HarmonyPatch(typeof(PeerInputSynchronizer), nameof(PeerInputSynchronizer.SyncLocalHoveredModel))]
internal static class SpectatorHoveredModelPatch
{
    private static bool Prefix()
    {
        return !SpectatorRegistry.IsLocalSpectator;
    }
}

[HarmonyPatch(typeof(PeerInputSynchronizer), nameof(PeerInputSynchronizer.SyncLocalIsTargeting))]
internal static class SpectatorTargetingPatch
{
    private static bool Prefix()
    {
        return !SpectatorRegistry.IsLocalSpectator;
    }
}
