using System.Reflection;
using System.Runtime.Loader;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: GameContractCheck <sts2-data-directory>");
    return 2;
}

var dataDirectory = Path.GetFullPath(args[0]);
var sts2Path = Path.Combine(dataDirectory, "sts2.dll");
if (!File.Exists(sts2Path))
{
    Console.Error.WriteLine($"sts2.dll was not found under '{dataDirectory}'.");
    return 2;
}

AssemblyLoadContext.Default.Resolving += (_, assemblyName) =>
{
    var simpleName = assemblyName.Name;
    if (simpleName is null)
    {
        return null;
    }

    var dependencyPath = Path.Combine(dataDirectory, $"{simpleName}.dll");
    return File.Exists(dependencyPath)
        ? AssemblyLoadContext.Default.LoadFromAssemblyPath(dependencyPath)
        : null;
};

try
{
    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(sts2Path);
    var netHostGameService = RequireType(assembly, "MegaCrit.Sts2.Core.Multiplayer.NetHostGameService");
    var startSteamHost = RequireSingleMethod(
        netHostGameService,
        "StartSteamHost",
        method => method.GetParameters() is [{ ParameterType: var parameterType }] && parameterType == typeof(int));
    if (!typeof(Task).IsAssignableFrom(startSteamHost.ReturnType))
    {
        throw new InvalidOperationException($"Unexpected StartSteamHost return type: {startSteamHost.ReturnType.FullName}");
    }

    var steamHost = RequireType(assembly, "MegaCrit.Sts2.Core.Multiplayer.Transport.Steam.SteamHost");
    if (steamHost.GetProperty("LobbyId", BindingFlags.Public | BindingFlags.Instance) is null)
    {
        throw new InvalidOperationException("Missing required SteamHost.LobbyId property.");
    }

    var startRunLobby = RequireType(assembly, "MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby");
    var expectedConstructorParameters = new[]
    {
        "MegaCrit.Sts2.Core.Runs.GameMode",
        "MegaCrit.Sts2.Core.Multiplayer.Game.INetGameService",
        "MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.IStartRunLobbyListener",
        "System.Int32"
    };
    var constructor = startRunLobby.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
        .SingleOrDefault(constructorInfo => constructorInfo.GetParameters().Select(parameter => parameter.ParameterType.FullName).SequenceEqual(expectedConstructorParameters));
    if (constructor is null)
    {
        throw new InvalidOperationException("Missing required StartRunLobby(GameMode, INetGameService, IStartRunLobbyListener, Int32) constructor.");
    }

    var beginRunLocally = RequireSingleMethod(
        startRunLobby,
        "BeginRunLocally",
        method => method.GetParameters() is [
            { ParameterType: var seedType },
            { ParameterType: var modifiersType }
        ] && seedType == typeof(string) &&
        modifiersType.IsGenericType &&
        modifiersType.GetGenericTypeDefinition() == typeof(List<>) &&
        modifiersType.GetGenericArguments() is [{ FullName: "MegaCrit.Sts2.Core.Models.ModifierModel" }]);

    var runLobby = RequireType(assembly, "MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.RunLobby");
    RequireSingleMethod(
        runLobby,
        "OnConnectedToClientAsHost",
        method => method.GetParameters() is [{ ParameterType: var playerIdType }] && playerIdType == typeof(ulong));
    RequireSingleMethod(
        runLobby,
        "HandleClientRejoinRequestMessage",
        method => method.GetParameters() is [
            { ParameterType.FullName: "MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby.ClientRejoinRequestMessage" },
            { ParameterType: var playerIdType }
        ] && playerIdType == typeof(ulong));

    var saveManager = RequireType(assembly, "MegaCrit.Sts2.Core.Saves.SaveManager");
    RequireSingleMethod(
        saveManager,
        "IncrementNumReloads",
        method => method.GetParameters() is [
            { ParameterType.FullName: "MegaCrit.Sts2.Core.Saves.SerializableRun" },
            { ParameterType.FullName: "MegaCrit.Sts2.Core.Multiplayer.Game.NetGameType" },
            { ParameterType: var forceInTestType }
        ] && forceInTestType == typeof(bool));

    var steamHostClose = RequireSingleMethod(
        steamHost,
        "SetHostIsClosed",
        method => method.GetParameters() is [{ ParameterType: var isClosedType }] && isClosedType == typeof(bool));
    if (steamHostClose.DeclaringType != steamHost)
    {
        throw new InvalidOperationException("SteamHost.SetHostIsClosed must be declared by SteamHost.");
    }

    var joinFriendScreen = RequireType(assembly, "MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NJoinFriendScreen");
    RequireSingleMethod(
        joinFriendScreen,
        "ShowFriends",
        method => method.GetParameters().Length == 0 && method.ReturnType == typeof(Task));
    RequireSingleMethod(
        joinFriendScreen,
        "JoinGame",
        method => method.GetParameters() is [{ ParameterType.FullName: "MegaCrit.Sts2.Core.Multiplayer.Connection.IClientConnectionInitializer" }]
            && method.ReturnType == typeof(void));
    RequireField(joinFriendScreen, "_buttonContainer");
    RequireField(joinFriendScreen, "_loadingFriendsIndicator");
    RequireField(joinFriendScreen, "_noFriendsLabel");

    var clickableControl = RequireType(assembly, "MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl");
    RequireSingleMethod(
        clickableControl,
        "OnPressHandler",
        method => method.GetParameters().Length == 0 && method.ReturnType == typeof(void));
    RequireSingleMethod(
        clickableControl,
        "ForceClick",
        method => method.GetParameters().Length == 0 && method.ReturnType == typeof(void));

    var playerHand = RequireType(assembly, "MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand");
    RequireSingleMethod(
        playerHand,
        "StartCardPlay",
        method => method.GetParameters() is [
            { ParameterType.FullName: "MegaCrit.Sts2.Core.Nodes.Cards.Holders.NHandCardHolder" },
            { ParameterType: var startedViaShortcutType }
        ] && startedViaShortcutType == typeof(bool) && method.ReturnType == typeof(void));
    RequireSingleMethod(
        playerHand,
        "SelectCardInSimpleMode",
        method => method.GetParameters() is [{ ParameterType.FullName: "MegaCrit.Sts2.Core.Nodes.Cards.Holders.NHandCardHolder" }] && method.ReturnType == typeof(void));
    RequireSingleMethod(
        playerHand,
        "SelectCardInUpgradeMode",
        method => method.GetParameters() is [{ ParameterType.FullName: "MegaCrit.Sts2.Core.Nodes.Cards.Holders.NHandCardHolder" }] && method.ReturnType == typeof(void));
    RequireSingleMethod(
        playerHand,
        "OnSelectModeConfirmButtonPressed",
        method => method.GetParameters() is [{ ParameterType.FullName: "MegaCrit.Sts2.Core.Nodes.GodotExtensions.NButton" }] && method.ReturnType == typeof(void));

    var actionQueueSynchronizer = RequireType(assembly, "MegaCrit.Sts2.Core.GameActions.Multiplayer.ActionQueueSynchronizer");
    RequireSingleMethod(
        actionQueueSynchronizer,
        "RequestEnqueue",
        method => method.GetParameters() is [{ ParameterType.FullName: "MegaCrit.Sts2.Core.GameActions.GameAction" }] && method.ReturnType == typeof(void));
    RequireSingleMethod(
        actionQueueSynchronizer,
        "RequestEnqueueHookAction",
        method => method.GetParameters() is [{ ParameterType.FullName: "MegaCrit.Sts2.Core.GameActions.GenericHookGameAction" }] && method.ReturnType == typeof(void));
    RequireSingleMethod(
        actionQueueSynchronizer,
        "RequestResumeActionAfterPlayerChoice",
        method => method.GetParameters() is [{ ParameterType.FullName: "MegaCrit.Sts2.Core.GameActions.GameAction" }] && method.ReturnType == typeof(void));

    var playerChoiceSynchronizer = RequireType(assembly, "MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceSynchronizer");
    RequireSingleMethod(
        playerChoiceSynchronizer,
        "SyncLocalChoice",
        method => method.GetParameters() is [
            { ParameterType.FullName: "MegaCrit.Sts2.Core.Entities.Players.Player" },
            { ParameterType: var choiceIdType },
            { ParameterType.FullName: "MegaCrit.Sts2.Core.GameActions.PlayerChoiceResult" }
        ] && choiceIdType == typeof(uint) && method.ReturnType == typeof(void));

    var eventSynchronizer = RequireType(assembly, "MegaCrit.Sts2.Core.Multiplayer.Game.EventSynchronizer");
    RequireSingleMethod(
        eventSynchronizer,
        "ChooseLocalOption",
        method => method.GetParameters() is [{ ParameterType: var optionIndexType }] && optionIndexType == typeof(int) && method.ReturnType == typeof(void));

    var restSiteSynchronizer = RequireType(assembly, "MegaCrit.Sts2.Core.Multiplayer.Game.RestSiteSynchronizer");
    RequireSingleMethod(
        restSiteSynchronizer,
        "ChooseLocalOption",
        method => method.GetParameters() is [{ ParameterType: var optionIndexType }] && optionIndexType == typeof(int) && method.ReturnType == typeof(Task<bool>));
    RequireSingleMethod(
        restSiteSynchronizer,
        "LocalOptionHovered",
        method => method.GetParameters() is [{ ParameterType.FullName: "MegaCrit.Sts2.Core.Entities.RestSite.RestSiteOption" }] && method.ReturnType == typeof(void));

    var peerInputSynchronizer = RequireType(assembly, "MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput.PeerInputSynchronizer");
    foreach (var methodName in new[]
             {
                 "SyncLocalMousePos",
                 "SyncLocalControllerFocus",
                 "SyncLocalIsUsingController",
                 "SyncLocalMouseDown",
                 "SyncLocalScreen",
                 "SyncLocalHoveredModel",
                 "SyncLocalIsTargeting"
             })
    {
        RequireSingleMethod(
            peerInputSynchronizer,
            methodName,
            method => method.ReturnType == typeof(void));
    }

    var rewardsSetSynchronizer = RequireType(assembly, "MegaCrit.Sts2.Core.Multiplayer.Game.RewardsSetSynchronizer");
    RequireSingleMethod(
        rewardsSetSynchronizer,
        "SelectLocalReward",
        method => method.GetParameters() is [{ ParameterType.FullName: "MegaCrit.Sts2.Core.Rewards.Reward" }] && method.ReturnType == typeof(Task<bool>));
    RequireSingleMethod(
        rewardsSetSynchronizer,
        "SkipLocalRewardsSet",
        method => method.GetParameters().Length == 0 && method.ReturnType == typeof(void));

    var rewardButton = RequireType(assembly, "MegaCrit.Sts2.Core.Nodes.Rewards.NRewardButton");
    RequireSingleMethod(
        rewardButton,
        "GetReward",
        method => method.GetParameters().Length == 0 && method.ReturnType == typeof(Task));

    var cardRewardSelectionScreen = RequireType(assembly, "MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCardRewardSelectionScreen");
    RequireSingleMethod(
        cardRewardSelectionScreen,
        "SelectCard",
        method => method.GetParameters().Length == 1 && method.ReturnType == typeof(void));
    RequireSingleMethod(
        cardRewardSelectionScreen,
        "OnAlternateRewardSelected",
        method => method.GetParameters() is [{ ParameterType: var indexType }] && indexType == typeof(int) && method.ReturnType == typeof(void));

    var cardSelectionMethodNames = new[]
    {
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NChooseACardSelectionScreen", "SelectHolder"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NChooseACardSelectionScreen", "OnSkipButtonReleased"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NChooseABundleSelectionScreen", "OnBundleClicked"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NChooseABundleSelectionScreen", "ConfirmSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCombatPileCardSelectScreen", "OnCardClicked"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCombatPileCardSelectScreen", "CompleteSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NSimpleCardSelectScreen", "OnCardClicked"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NSimpleCardSelectScreen", "CompleteSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckCardSelectScreen", "OnCardClicked"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckCardSelectScreen", "CloseSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckCardSelectScreen", "ConfirmSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckUpgradeSelectScreen", "OnCardClicked"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckUpgradeSelectScreen", "CloseSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckUpgradeSelectScreen", "ConfirmSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckEnchantSelectScreen", "OnCardClicked"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckEnchantSelectScreen", "CloseSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckEnchantSelectScreen", "ConfirmSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckTransformSelectScreen", "OnCardClicked"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckTransformSelectScreen", "CloseSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckTransformSelectScreen", "ConfirmSelection"),
        ("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NDeckTransformSelectScreen", "CompleteSelection")
    };
    foreach (var (typeName, methodName) in cardSelectionMethodNames)
    {
        RequireSingleMethod(
            RequireType(assembly, typeName),
            methodName,
            method => method.ReturnType == typeof(void));
    }

    Console.WriteLine("STS2 v0.109.0 host-lobby and spectator contract checks passed.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static Type RequireType(Assembly assembly, string fullName)
{
    return assembly.GetType(fullName, throwOnError: false)
        ?? throw new InvalidOperationException($"Missing required type: {fullName}");
}

static MethodInfo RequireSingleMethod(Type type, string name, Func<MethodInfo, bool> matches)
{
    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        .Where(method => method.Name == name && matches(method))
        .ToArray();
    return methods.Length switch
    {
        1 => methods[0],
        0 => throw new InvalidOperationException($"Missing required method: {type.FullName}.{name}"),
        _ => throw new InvalidOperationException($"More than one required method matched: {type.FullName}.{name}")
    };
}

static FieldInfo RequireField(Type type, string name)
{
    return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Missing required field: {type.FullName}.{name}");
}
