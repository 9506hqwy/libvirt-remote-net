namespace LibvirtRemote;

using Protocol;
using System.Runtime.Serialization;

[Serializable]
public class VirtException : Exception
{
    public VirtException(string message)
        : base(message)
    {
        this.Error = new VirNetMessageError
        {
            Message = new Xdr.XdrOption<string>(message),
        };
    }

    public VirtException(VirNetMessageError error)
        : base(error.Message?.Value)
    {
        this.Error = error;
    }

    public VirtException(string message, VirNetMessageError error)
        : base(message)
    {
        this.Error = error;
    }

    public VirtException(
        string message,
        Exception innerException,
        VirNetMessageError error)
        : base(message, innerException)
    {
        this.Error = error;
    }

    protected VirtException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        this.Error = (VirNetMessageError)info.GetValue(
            nameof(this.Error),
            typeof(VirNetMessageError));
    }

    public VirNetMessageError Error { get; }

    public override void GetObjectData(
        SerializationInfo info,
        StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(this.Error), this.Error);
    }
}
