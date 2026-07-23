using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using SpireWatch.Networking;
using SpireWatch.Rooms;
using SpireWatch.Spectating;

namespace SpireWatch.Rooms.UI;

/// <summary>
/// Adds one native-style tab to the existing settings screen. The implementation clones the shipped
/// General tab and panel because NSettingsTab has scene-wired fields that cannot be safely new'ed.
/// </summary>
internal sealed class RoomSettingsSection : VBoxContainer
{
    private const string TabName = "SpireWatchRoomTab";
    private const string PanelName = "SpireWatchRoomPanel";

    private static readonly FieldInfo TabsField = AccessTools.Field(typeof(NSettingsTabManager), "_tabs");
    private static readonly MethodInfo SwitchTabMethod = AccessTools.Method(typeof(NSettingsTabManager), "SwitchTabTo");

    private readonly VBoxContainer _memberList = new()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill
    };

    private readonly Button _leaveRoomButton = new()
    {
        Text = "离开房间",
        Visible = false,
        SizeFlagsHorizontal = SizeFlags.ShrinkEnd
    };

    private NSettingsPanel? _panel;

    private RoomSettingsSection()
    {
        Name = "SpireWatchRoomSection";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", 10);

        var title = new Label
        {
            Text = "房间成员",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        title.AddThemeFontSizeOverride("font_size", 22);
        AddChildSafely(title);

        AddChildSafely(_memberList);
        _leaveRoomButton.Pressed += LeaveRoom;
        AddChildSafely(_leaveRoomButton);
    }

    internal static void Install(NSettingsTabManager manager)
    {
        if (manager.GetNodeOrNull<Node>(TabName) is not null ||
            manager.GetNodeOrNull<Node>($"%{PanelName}") is not null)
        {
            return;
        }

        try
        {
            var templateTab = manager.GetNode<NSettingsTab>("General");
            var templatePanel = manager.GetNode<NSettingsPanel>("%GeneralSettings");
            var roomTab = templateTab.Duplicate() as NSettingsTab
                ?? throw new InvalidOperationException("Could not clone the settings tab template.");
            var roomPanel = templatePanel.Duplicate() as NSettingsPanel
                ?? throw new InvalidOperationException("Could not clone the settings panel template.");

            roomTab.Name = TabName;
            roomPanel.Name = PanelName;
            templateTab.GetParent().AddChildSafely(roomTab);
            templatePanel.GetParent().AddChildSafely(roomPanel);

            // The duplicate is added to the live tree before these scene-bound methods are used.
            roomTab.SetLabel("房间");
            var content = roomPanel.GetNode<VBoxContainer>("VBoxContainer");
            ClearPanel(content);
            var section = new RoomSettingsSection { _panel = roomPanel };
            content.AddChildSafely(section);

            var tabs = TabsField.GetValue(manager) as Dictionary<NSettingsTab, NSettingsPanel>
                ?? throw new InvalidOperationException("NSettingsTabManager._tabs is unavailable.");
            tabs.Add(roomTab, roomPanel);
            roomTab.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
            {
                SwitchTabMethod.Invoke(manager, new object[] { roomTab });
            }));

            section.Refresh();
        }
        catch (Exception exception)
        {
            MegaCrit.Sts2.Core.Logging.Log.Warn($"[{ModInfo.Id}] Could not install the settings room section: {exception.Message}");
        }
    }

    public override void _Ready()
    {
        RoomRoster.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        RoomRoster.Changed -= Refresh;
        base._ExitTree();
    }

    private static void ClearPanel(VBoxContainer content)
    {
        foreach (Node child in content.GetChildren())
        {
            content.RemoveChild(child);
            child.QueueFreeSafely();
        }
    }

    private void Refresh()
    {
        if (!IsInsideTree())
        {
            return;
        }

        foreach (Node child in _memberList.GetChildren())
        {
            _memberList.RemoveChild(child);
            child.QueueFreeSafely();
        }

        foreach (var member in RoomRoster.Members)
        {
            var onSelected = SpectatorRegistry.IsLocalSpectator && member.Role == RoomMemberRole.Playing
                ? new Action<ulong>(playerId => _ = SpectatorViewSwitch.Request(playerId))
                : null;
            _memberList.AddChildSafely(new RoomMemberRow(member, onSelected));
        }

        if (RoomRoster.Members.Count == 0)
        {
            _memberList.AddChildSafely(new Label { Text = "当前不在多人房间中。" });
        }

        _leaveRoomButton.Visible = SpectatorRegistry.IsLocalSpectator;
        _panel?.CallDeferred("RefreshSize");
    }

    private void LeaveRoom()
    {
        if (!SpectatorRegistry.IsLocalSpectator)
        {
            return;
        }

        _leaveRoomButton.Disabled = true;
        try
        {
            RunManager.Instance.NetService?.Disconnect(NetError.Quit);
        }
        finally
        {
            SpectatorRegistry.EndLocalSpectating();
            SpectatorViewSwitch.EndLocalSession();
            RoomRosterProtocol.UnbindActive();
            RoomRoster.Clear();
        }
    }
}
