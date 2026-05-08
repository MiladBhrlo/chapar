using System.Reflection;

namespace Chapar.Core.Utilities;

public static class ChaparVersion
{
    private static readonly Lazy<string> _version = new(() =>
    {
        var assembly = typeof(ChaparVersion).Assembly;
        return assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "1.0.0";
    });

    public static string Current => _version.Value;
}
