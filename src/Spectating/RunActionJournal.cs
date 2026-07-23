using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using SpireWatch.Networking;
using SpireWatch.Rooms;

namespace SpireWatch.Spectating;

/// <summary>
/// Captures the original reliable action messages while a spectator is loading. The journal is not a
/// second action protocol: replay uses the game's ActionEnqueued/Hook/Resume message types directly.
/// </summary>
internal static class RunActionJournal
{
    private const int MaxEntries = 2048;
    private static readonly List<JournalEntry> Entries = new();
    private static readonly Dictionary<ulong, long> PendingReplayCheckpoints = new();
    private static INetGameService? _hostService;
    private static long _nextSequence;

    internal static long CaptureCheckpoint()
    {
        return _nextSequence;
    }

    internal static void AwaitSpectatorReplay(ulong spectatorNetId, long checkpoint)
    {
        PendingReplayCheckpoints[spectatorNetId] = checkpoint;
    }

    internal static void ForgetSpectator(ulong spectatorNetId)
    {
        PendingReplayCheckpoints.Remove(spectatorNetId);
    }

    internal static void EnsureHostBound(INetGameService service)
    {
        if (ReferenceEquals(_hostService, service))
        {
            return;
        }

        if (_hostService is not null)
        {
            _hostService.UnregisterMessageHandler<SpectatorActionReplayReadyMessage>(HandleReplayReady);
        }

        _hostService = service;
        service.RegisterMessageHandler<SpectatorActionReplayReadyMessage>(HandleReplayReady);
    }

    internal static void NotifySpectatorReady(INetGameService service)
    {
        service.SendMessage(new SpectatorActionReplayReadyMessage());
    }

    internal static void Clear()
    {
        PendingReplayCheckpoints.Clear();
        Entries.Clear();
        _nextSequence = 0;
    }

    internal static void Record(ActionEnqueuedMessage message)
    {
        Add(JournalEntry.For(Clone(message)));
    }

    internal static void Record(HookActionEnqueuedMessage message)
    {
        Add(JournalEntry.For(Clone(message)));
    }

    internal static void Record(ResumeActionAfterPlayerChoiceMessage message)
    {
        Add(JournalEntry.For(Clone(message)));
    }

    private static void HandleReplayReady(SpectatorActionReplayReadyMessage _, ulong senderId)
    {
        if (_hostService is not NetHostGameService hostService ||
            !SpectatorRegistry.IsHostSpectator(senderId) ||
            !PendingReplayCheckpoints.Remove(senderId, out var checkpoint))
        {
            return;
        }

        if (!TryReplaySince(checkpoint, hostService, senderId))
        {
            hostService.DisconnectClient(senderId, NetError.InvalidJoin);
            Log.Warn($"[{ModInfo.Id}] Action journal overflow while admitting spectator {senderId}.");
            return;
        }

        hostService.SetPeerReadyForBroadcasting(senderId);
        RoomRosterProtocol.SendHostRosterTo(senderId);
        Log.Info($"[{ModInfo.Id}] Replayed action journal to spectator {senderId} from checkpoint {checkpoint}.");
    }

    private static bool TryReplaySince(long checkpoint, NetHostGameService service, ulong spectatorNetId)
    {
        if (Entries.Count > 0 && checkpoint < Entries[0].Sequence)
        {
            return false;
        }

        foreach (var entry in Entries)
        {
            if (entry.Sequence < checkpoint)
            {
                continue;
            }

            entry.SendTo(service, spectatorNetId);
        }

        return true;
    }

    private static void Add(JournalEntry entry)
    {
        if (RunManager.Instance.NetService?.Type != NetGameType.Host)
        {
            return;
        }

        Entries.Add(entry with { Sequence = _nextSequence++ });
        if (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(0);
        }
    }

    private static T Clone<T>(T message) where T : IPacketSerializable, new()
    {
        var writer = new PacketWriter();
        writer.Write(message);
        writer.ZeroByteRemainder();
        var reader = new PacketReader();
        reader.Reset(writer.Buffer);
        return reader.Read<T>();
    }

    private sealed record JournalEntry(
        long Sequence,
        ActionEnqueuedMessage? Action,
        HookActionEnqueuedMessage? Hook,
        ResumeActionAfterPlayerChoiceMessage? Resume)
    {
        internal static JournalEntry For(ActionEnqueuedMessage message) => new(0, message, null, null);
        internal static JournalEntry For(HookActionEnqueuedMessage message) => new(0, null, message, null);
        internal static JournalEntry For(ResumeActionAfterPlayerChoiceMessage message) => new(0, null, null, message);

        internal void SendTo(NetHostGameService service, ulong spectatorNetId)
        {
            if (Action.HasValue)
            {
                service.SendMessage(Action.Value, spectatorNetId);
            }
            else if (Hook.HasValue)
            {
                service.SendMessage(Hook.Value, spectatorNetId);
            }
            else if (Resume.HasValue)
            {
                service.SendMessage(Resume.Value, spectatorNetId);
            }
        }
    }
}

[HarmonyPatch(typeof(ActionQueueSynchronizer), "EnqueueAction")]
internal static class HostActionJournalPatch
{
    private static readonly FieldInfo MessageBufferField = AccessTools.Field(typeof(ActionQueueSynchronizer), "_messageBuffer");

    private static void Prefix(ActionQueueSynchronizer __instance, GameAction action, ulong actionOwnerId)
    {
        if (RunManager.Instance.NetService?.Type != NetGameType.Host)
        {
            return;
        }

        var messageBuffer = MessageBufferField.GetValue(__instance) as RunLocationTargetedMessageBuffer;
        if (messageBuffer is null)
        {
            return;
        }

        RunActionJournal.Record(new ActionEnqueuedMessage
        {
            playerId = actionOwnerId,
            location = messageBuffer.CurrentLocation,
            action = action.ToNetAction()
        });
    }
}

[HarmonyPatch(typeof(ActionQueueSynchronizer), "EnqueueHookAction")]
internal static class HostHookActionJournalPatch
{
    private static readonly FieldInfo MessageBufferField = AccessTools.Field(typeof(ActionQueueSynchronizer), "_messageBuffer");

    private static void Prefix(ActionQueueSynchronizer __instance, GenericHookGameAction gameAction)
    {
        if (RunManager.Instance.NetService?.Type != NetGameType.Host)
        {
            return;
        }

        var messageBuffer = MessageBufferField.GetValue(__instance) as RunLocationTargetedMessageBuffer;
        if (messageBuffer is null)
        {
            return;
        }

        RunActionJournal.Record(new HookActionEnqueuedMessage
        {
            hookActionId = gameAction.HookId,
            ownerId = gameAction.OwnerId,
            location = messageBuffer.CurrentLocation,
            gameActionType = gameAction.ActionType
        });
    }
}

[HarmonyPatch(typeof(ActionQueueSynchronizer), "ResumeActionAfterPlayerChoice")]
internal static class HostResumeActionJournalPatch
{
    private static readonly FieldInfo MessageBufferField = AccessTools.Field(typeof(ActionQueueSynchronizer), "_messageBuffer");

    private static void Prefix(ActionQueueSynchronizer __instance, uint id)
    {
        if (RunManager.Instance.NetService?.Type != NetGameType.Host)
        {
            return;
        }

        var messageBuffer = MessageBufferField.GetValue(__instance) as RunLocationTargetedMessageBuffer;
        if (messageBuffer is null)
        {
            return;
        }

        RunActionJournal.Record(new ResumeActionAfterPlayerChoiceMessage
        {
            actionId = id,
            location = messageBuffer.CurrentLocation
        });
    }
}
