using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using SpireWatch.Rooms;

namespace SpireWatch.Rooms.UI;

internal sealed class RoomMemberRow : HBoxContainer
{
    private readonly TextureRect _characterIcon;
    private readonly RoomSpectatorIcon _spectatorIcon;
    private readonly Label _nameLabel;
    private readonly Label _statusLabel;
    private readonly Action<ulong>? _onPlayingMemberSelected;
    private ulong _memberNetId;

    internal RoomMemberRow(RoomMember member, Action<ulong>? onPlayingMemberSelected = null)
    {
        CustomMinimumSize = new Vector2(0f, 42f);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", 12);
        _onPlayingMemberSelected = onPlayingMemberSelected;
        MouseFilter = onPlayingMemberSelected is null ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        if (onPlayingMemberSelected is not null)
        {
            GuiInput += OnGuiInput;
        }

        _characterIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(34f, 34f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChildSafely(_characterIcon);

        _spectatorIcon = new RoomSpectatorIcon
        {
            CustomMinimumSize = new Vector2(34f, 34f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChildSafely(_spectatorIcon);

        _nameLabel = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChildSafely(_nameLabel);

        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChildSafely(_statusLabel);

        UpdateMember(member);
    }

    internal void UpdateMember(RoomMember member)
    {
        _memberNetId = member.NetId;
        var isSpectator = member.Role == RoomMemberRole.Spectating;
        _characterIcon.Visible = !isSpectator;
        _characterIcon.Texture = isSpectator ? null : member.Character?.IconTexture;
        _spectatorIcon.Visible = isSpectator;
        _nameLabel.Text = member.DisplayName;
        _statusLabel.Text = GetStatus(member);
    }

    private void OnGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
        {
            _onPlayingMemberSelected?.Invoke(_memberNetId);
        }
    }

    private static string GetStatus(RoomMember member)
    {
        if (!member.IsConnected)
        {
            return "断线重连中";
        }

        return member.Role == RoomMemberRole.Spectating
            ? "观战中"
            : member.IsReady ? "已准备" : "未准备";
    }
}

/// <summary>Draws an eye directly so the room UI does not depend on an unverified PCK asset path.</summary>
internal sealed class RoomSpectatorIcon : Control
{
    public override void _Draw()
    {
        var center = Size / 2f;
        var color = Colors.White;
        DrawArc(center, 11f, 0f, Mathf.Pi, 20, color, 2f);
        DrawArc(center, 11f, Mathf.Pi, Mathf.Tau, 20, color, 2f);
        DrawCircle(center, 4f, color);
    }
}
