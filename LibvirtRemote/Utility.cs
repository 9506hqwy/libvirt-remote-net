namespace LibvirtRemote;

using Xdr.Serialization;

internal static class Utility
{
    internal static T ConvertFromBytes<T>(byte[] source)
    {
        var obj = (T)XdrDeserializer.Deserialize<T>(source, out var remain);
        if (remain.Any())
        {
            throw new InvalidOperationException();
        }

        return obj;
    }

    internal static byte[] ConvertToBytes<T>(T source)
    {
        return XdrSerializer.Serialize(source);
    }
}
