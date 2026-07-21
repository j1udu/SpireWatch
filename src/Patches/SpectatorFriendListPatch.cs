using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Platform.Steam;
using SpireWatch.Spectating;

namespace SpireWatch.Patches;

[HarmonyPatch(typeof(NJoinFriendScreen), "ShowFriends")]
internal static class SpectatorFriendListPatch
{
    private static readonly FieldInfo ButtonContainerField = AccessTools.Field(typeof(NJoinFriendScreen), "_buttonContainer");
    private static readonly FieldInfo LoadingFriendsIndicatorField = AccessTools.Field(typeof(NJoinFriendScreen), "_loadingFriendsIndicator");
    private static readonly FieldInfo NoFriendsLabelField = AccessTools.Field(typeof(NJoinFriendScreen), "_noFriendsLabel");
    private static readonly MethodInfo JoinGameMethod = AccessTools.Method(typeof(NJoinFriendScreen), "JoinGame");

    private static bool Prefix(NJoinFriendScreen __instance, ref Task __result)
    {
        __result = ShowFriends(__instance);
        return false;
    }

    private static async Task ShowFriends(NJoinFriendScreen screen)
    {
        var buttonContainer = (Control?)ButtonContainerField.GetValue(screen)
            ?? throw new InvalidOperationException("NJoinFriendScreen._buttonContainer is not available.");
        var loadingFriendsIndicator = (Control?)LoadingFriendsIndicatorField.GetValue(screen)
            ?? throw new InvalidOperationException("NJoinFriendScreen._loadingFriendsIndicator is not available.");
        var noFriendsLabel = (Control?)NoFriendsLabelField.GetValue(screen)
            ?? throw new InvalidOperationException("NJoinFriendScreen._noFriendsLabel is not available.");

        loadingFriendsIndicator.Visible = true;
        try
        {
            foreach (Node child in buttonContainer.GetChildren())
            {
                child.QueueFreeSafely();
            }

            if (SteamInitializer.Initialized)
            {
                var friendIds = await PlatformUtil.GetFriendsWithOpenLobbies(PlatformType.Steam);
                var rooms = await ReadRooms(friendIds);
                foreach (var room in rooms)
                {
                    var button = NJoinFriendButton.Create(room.FriendSteamId);
                    buttonContainer.AddChildSafely(button);
                    if (room.IsSpectatable)
                    {
                        AddSpectatorStatus(button);
                    }
                    BindButton(screen, button, room);
                }
            }

            ActiveScreenContext.Instance.Update();
            noFriendsLabel.Visible = buttonContainer.GetChildCount() == 0;
        }
        finally
        {
            loadingFriendsIndicator.Visible = false;
        }
    }

    private static async Task<List<FriendRoom>> ReadRooms(IEnumerable<ulong> friendIds)
    {
        var tasks = new List<Task<FriendRoom>>();
        foreach (var friendSteamId in friendIds)
        {
            tasks.Add(ReadRoom(friendSteamId));
        }

        return new List<FriendRoom>(await Task.WhenAll(tasks));
    }

    private static async Task<FriendRoom> ReadRoom(ulong friendSteamId)
    {
        try
        {
            return new FriendRoom(friendSteamId, await SteamFriendLobbyMetadata.IsRunningCompatibleSession(friendSteamId));
        }
        catch (Exception exception)
        {
            Log.Warn($"[{ModInfo.Id}] Failed to read Steam lobby metadata for friend {friendSteamId}: {exception.Message}");
            return new FriendRoom(friendSteamId, false);
        }
    }

    private static void BindButton(NJoinFriendScreen screen, NJoinFriendButton button, FriendRoom room)
    {
        button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            TaskHelper.RunSafely(JoinRoom(screen, room));
        }));
    }

    private static void AddSpectatorStatus(NJoinFriendButton button)
    {
        var status = new Label
        {
            Text = "进行中 · 观战",
            Position = new Vector2(12f, 42f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        button.AddChildSafely(status);
        button.TooltipText = "进行中 · 观战";
    }

    private static async Task JoinRoom(NJoinFriendScreen screen, FriendRoom room)
    {
        if (room.IsSpectatable || await SteamFriendLobbyMetadata.IsRunningCompatibleSession(room.FriendSteamId))
        {
            Log.Info($"[{ModInfo.Id}] Starting spectator flow from friend-room button for {room.FriendSteamId}.");
            await BeginSpectating(screen, room.FriendSteamId);
            return;
        }

        Log.Info($"[{ModInfo.Id}] Starting vanilla join flow from friend-room button for {room.FriendSteamId}.");
        var initializer = SteamClientConnectionInitializer.FromPlayer(room.FriendSteamId);
        JoinGameMethod.Invoke(screen, new object[] { initializer });
    }

    private static async Task BeginSpectating(NJoinFriendScreen screen, ulong hostSteamId)
    {
        var loadingOverlay = screen.GetNode<Control>("%LoadingOverlay");
        loadingOverlay.Visible = true;
        try
        {
            await SpectatorJoinFlow.JoinAsync(hostSteamId, screen.GetTree());
        }
        finally
        {
            if (GodotObject.IsInstanceValid(screen))
            {
                loadingOverlay.Visible = false;
            }
        }
    }

    private sealed record FriendRoom(ulong FriendSteamId, bool IsSpectatable);
}
