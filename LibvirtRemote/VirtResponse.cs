namespace LibvirtRemote;

using Binding;
using Protocol;
using System.Reflection;

public class VirtResponse(VirNetMessageHeader header, byte[]? body)
{
    private static readonly IDictionary<uint, Dictionary<int, MethodInfo>> bytesToEvent;

    static VirtResponse()
    {
        bytesToEvent = VirtEventAttribute.Attrs
            .GroupBy(a => a.Prog)
            .ToDictionary(a => a.Key, a => a.ToDictionary(b => b.Proc, GetBytesToEvent));
    }

    public VirNetMessageHeader Header { get; } = header;

    public byte[]? Body { get; } = body;

    public T? ConvertTo<T>()
    {
        if (this.Header.Status == VirNetMessageStatus.VirNetError)
        {
            var errMsg = Utility.ConvertFromBytes<VirNetMessageError>(this.Body!);
            throw new VirtException(errMsg);
        }

        return this.Body is null
            ? default
            : this.Header.Type is VirNetMessageType.VirNetReply or
            VirNetMessageType.VirNetMessage or
            VirNetMessageType.VirNetStream or
            VirNetMessageType.VirNetStreamHole
            ? Utility.ConvertFromBytes<T>(this.Body!)
            : throw new NotSupportedException();
    }

    public IVirtEvent? ConvertToEvent()
    {
        if (bytesToEvent.TryGetValue(this.Header.Prog, out var methods))
        {
            if (methods.TryGetValue(this.Header.Proc, out var method))
            {
                return (IVirtEvent)method.Invoke(this, null);
            }
        }

        return null;
    }

    private static MethodInfo GetBytesToEvent(VirtEventAttribute attr)
    {
        return typeof(VirtResponse)
            .GetMethod(nameof(ConvertTo))
            .MakeGenericMethod(attr.Type);
    }
}
