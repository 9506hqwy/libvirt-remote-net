namespace LibvirtRemote;

using Xdr.Serialization;

internal static class Utility
{
    internal static T ConvertFromBytes<T>(byte[] source)
    {
        var obj = (T)XdrDeserializer.Deserialize<T>(source, out var remain);
        return remain.Any() ? throw new InvalidOperationException() : obj;
    }

    internal static byte[] ConvertToBytes<T>(T source)
    {
        return XdrSerializer.Serialize(source);
    }
}
