namespace LibvirtRemote;

using Protocol;
using Xdr;

public class VirtNetStream : Stream, IDisposable
{
    private readonly VirtClient client;

    private readonly VirNetMessageHeader header;

    private readonly VirtStream downStream;

    private bool disposed;

    internal VirtNetStream(
        VirtClient client,
        VirtStream downStream,
        VirNetMessageHeader header)
    {
        this.client = client;
        this.downStream = downStream;
        this.header = header;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return this.downStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        this.client.Socket.Send(
            this.header.Serial,
            this.header.Proc,
            VirNetMessageType.VirNetStream,
            VirNetMessageStatus.VirNetContinue,
            buffer.Skip(offset).Take(count).ToArray());
    }

    public async Task WriteCompletedAsync(CancellationToken cancellationToken)
    {
        var res = await this.client.RequestAsync(
            false,
            this.header.Serial,
            this.header.Proc,
            VirNetMessageType.VirNetStream,
            VirNetMessageStatus.VirNetOk,
            null,
            cancellationToken);
        res.ConvertTo<XdrVoid>();
    }

    protected override void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        if (disposing)
        {
            this.downStream.Dispose();
            base.Dispose(disposing);
        }

        this.disposed = true;
    }
}
