using System.Reflection;

namespace SpireWatch;

internal static class ModInfo
{
    internal const string Id = "SpireWatch";
    internal const string HarmonyId = "spirewatch.sts2";
    internal const int ProtocolVersion = 1;

    internal static readonly string Version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+')[0]
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "0.0.0";
}
