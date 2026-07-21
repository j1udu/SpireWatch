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

    var steamHostClose = RequireSingleMethod(
        steamHost,
        "SetHostIsClosed",
        method => method.GetParameters() is [{ ParameterType: var isClosedType }] && isClosedType == typeof(bool));
    if (steamHostClose.DeclaringType != steamHost)
    {
        throw new InvalidOperationException("SteamHost.SetHostIsClosed must be declared by SteamHost.");
    }

    var runManager = RequireType(assembly, "MegaCrit.Sts2.Core.Runs.RunManager");
    RequireSingleMethod(
        runManager,
        "CleanUp",
        method => method.GetParameters() is [{ ParameterType: var gracefulType }] && gracefulType == typeof(bool));

    Console.WriteLine("STS2 v0.109.0 Stage 1 lobby lifecycle contract checks passed.");
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
