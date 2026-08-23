using System;
using System.Linq;
using System.Reflection;

namespace TONX.Attributes;

// https://github.com/HayashiUme/FilterAPI/blob/main/FilterAPI/FilterAPI/Attributes/AmciModGuid.cs
[AttributeUsage(AttributeTargets.Class)]
public sealed class AmciModGuidAttribute : Attribute
{
    public AmciModGuidAttribute(string guid)
    {
        Guid = Guid.Parse(guid);
    }

    public Guid Guid { get; }

    public static Guid? GetGuid(Type type)
    {
        var attribute = type.GetCustomAttribute<AmciModGuidAttribute>();
        if (attribute != null)
        {
            return attribute.Guid;
        }

        var metadataAttribute = type.Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(x => x.Key == "TONX");
        if (metadataAttribute is { Value: not null } && Guid.TryParse(metadataAttribute.Value, out var guid))
        {
            return guid;
        }

        return null;
    }
}
