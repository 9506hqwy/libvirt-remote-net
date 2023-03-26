namespace LibvirtRemote;

using Binding;
using Protocol;
using System.Reflection;

public class VirtResponse
{
    private static IDictionary<uint, Dictionary<int, MethodInfo>> bytesToEvent;

    static VirtResponse()
    {
        VirtResponse.bytesToEvent = VirtEventAttribute.Attrs
            .GroupBy(a => a.Prog)
            .ToDictionary(a => a.Key, a => a.ToDictionary(b => b.Proc, VirtResponse.GetBytesToEvent));
    }

    public VirtResponse(VirNetMessageHeader header, byte[]? body)
    {
        this.Header = header;
        this.Body = body;
    }

    public VirNetMessageHeader Header { get; }

    public byte[]? Body { get; }

    public T? ConvertTo<T>()
    {
        if (this.Header.Status == VirNetMessageStatus.VirNetError)
        {
            var errMsg = Utility.ConvertFromBytes<VirNetMessageError>(this.Body!);
            throw new VirtException(errMsg);
        }

        if (this.Body is null)
        {
            return default;
        }

        if (this.Header.Type == VirNetMessageType.VirNetReply ||
            this.Header.Type == VirNetMessageType.VirNetMessage ||
            this.Header.Type == VirNetMessageType.VirNetStream ||
            this.Header.Type == VirNetMessageType.VirNetStreamHole)
        {
            return Utility.ConvertFromBytes<T>(this.Body!);
        }

        throw new NotSupportedException();
    }

    public IVirtEvent? ConvertToEvent()
    {
        if (VirtResponse.bytesToEvent.TryGetValue(this.Header.Prog, out var methods))
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
            .GetMethod(nameof(VirtResponse.ConvertTo))
            .MakeGenericMethod(attr.Type);
    }
}
