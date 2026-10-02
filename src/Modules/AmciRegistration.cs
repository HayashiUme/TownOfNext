using System.Security.Cryptography;
using System.Text;
using TONX.Attributes;

namespace TONX.Modules;

/// <summary>
/// 这个东西原本是我写的，但是被mini借鉴走了，我估摸着Reactor更新后也会是这个版本
/// </summary>
public static class AmciRegistration
{
    public const int HeaderVersion = 1;

    private static readonly (string Id, string Version)[] RequiredOnAllClients =
    {
        (Main.PluginGuid, Main.PluginVersion),
    };

    public static string GetHeader()
    {
        var builder = new StringBuilder();
        builder.Append(HeaderVersion);
        builder.Append(';');
        builder.Append(RequiredOnAllClients.Length);

        foreach (var (id, version) in RequiredOnAllClients)
        {
            builder.Append(';');
            builder.Append(id);
            builder.Append('=');
            builder.Append(version);
        }

        return builder.ToString();
    }

    public static Guid GetCompositeGuid()
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(GetHeader()));
        var guidBytes = new byte[16];
        Array.Copy(hash, 0, guidBytes, 0, 16);
        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }

    [PluginModuleInitializer]
    public static void Register()
    {
        if (CurrentModRegistration.ModRegistrationGuidString != string.Empty)
            Logger.Warn($"Another mod already registered an AMCI GUID: {CurrentModRegistration.ModRegistrationGuidString}", "AMCI");

        var guid = GetCompositeGuid();
        CurrentModRegistration.ModRegistrationGuidString = guid.ToString();
        Logger.Info($"Registered AMCI GUID {guid} from header {GetHeader()}", "AMCI");
    }
}
