using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpireWatch.Spectating;

internal sealed class SpectatorJoinPanel : PanelContainer
{
    private readonly LineEdit _hostSteamIdInput = new()
    {
        PlaceholderText = "Host Steam64 ID"
    };

    internal static void InstallOn(NJoinFriendScreen screen)
    {
        if (screen.GetNodeOrNull<SpectatorJoinPanel>("SpireWatchSpectatorJoinPanel") is not null)
        {
            return;
        }

        screen.AddChild(new SpectatorJoinPanel());
    }

    internal SpectatorJoinPanel()
    {
        Name = "SpireWatchSpectatorJoinPanel";
        Position = new Vector2(32f, 520f);
        Size = new Vector2(360f, 112f);

        var layout = new VBoxContainer();
        layout.AddChild(new Label { Text = "SpireWatch 只读观战" });
        layout.AddChild(_hostSteamIdInput);

        var joinButton = new Button { Text = "通过 Steam64 ID 观战" };
        joinButton.Pressed += OnJoinPressed;
        layout.AddChild(joinButton);
        AddChild(layout);
    }

    private void OnJoinPressed()
    {
        if (!ulong.TryParse(_hostSteamIdInput.Text, out var hostSteamId))
        {
            Log.Warn($"[{ModInfo.Id}] The spectator Steam64 ID is invalid.");
            return;
        }

        TaskHelper.RunSafely(Join(hostSteamId));
    }

    private async Task Join(ulong hostSteamId)
    {
        try
        {
            await SpectatorJoinFlow.JoinAsync(hostSteamId, GetTree());
        }
        catch (Exception exception)
        {
            Log.Error($"[{ModInfo.Id}] Spectator join failed: {exception}");
        }
    }
}
