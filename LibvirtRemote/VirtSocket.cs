namespace LibvirtRemote;

using Protocol;

public class VirtSocket : IDisposable
{
    private readonly Stream stream;

    private readonly SemaphoreSlim writeLock;

    private bool disposed;

    public VirtSocket(Stream stream)
    {
        this.disposed = false;

        this.stream = stream;

        this.writeLock = new SemaphoreSlim(1, 1);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public VirtResponse? Receive()
    {
        return this.ReceiveAsync(default).Result;
    }

    public async Task<VirtResponse?> ReceiveAsync(CancellationToken cancellationToken)
    {
        var pktLengthBytes = await this.ReadByteAsync(4, cancellationToken);
        var pktLen = Utility.ConvertFromBytes<uint>(pktLengthBytes);
        if (pktLen < 28)
        {
            return null;
        }

        var resHeaderBytes = await this.ReadByteAsync(24, cancellationToken);
        var resHeader = Utility.ConvertFromBytes<VirNetMessageHeader>(resHeaderBytes);
        if (resHeader.Prog != Binding.Constants.QemuProgram &&
            resHeader.Prog != Binding.Constants.RemoteProgram)
        {
            throw new VirtException($"Invalid program number: {resHeader.Prog}");
        }
        else if (
            resHeader.Vers != Binding.Constants.QemuProtocolVersion &&
            resHeader.Vers != Binding.Constants.RemoteProtocolVersion)
        {
            throw new VirtException($"Invalid protocol version: {resHeader.Vers}");
        }

        var resBodyLength = pktLen - 28;
        if (resBodyLength == 0)
        {
            return new VirtResponse(resHeader, null);
        }

        var resBodyBytes = await this.ReadByteAsync(resBodyLength, cancellationToken);

        return new VirtResponse(resHeader, resBodyBytes);
    }

    public uint Send(
        uint prog,
        uint protoVersion,
        uint serial,
        int proc,
        VirNetMessageType msgType,
        VirNetMessageStatus msgStatus,
        object? request)
    {
        return this.SendAsync(prog, protoVersion, serial, proc, msgType, msgStatus, request, default).Result;
    }

    public async Task<uint> SendAsync(
        uint prog,
        uint protoVersion,
        uint serial,
        int proc,
        VirNetMessageType msgType,
        VirNetMessageStatus msgStatus,
        object? request,
        CancellationToken cancellationToken)
    {
        uint pktLength = 4;

        var reqHeader = new VirNetMessageHeader
        {
            Prog = prog,
            Vers = protoVersion,
            Proc = proc,
            Type = msgType,
            Serial = serial,
            Status = msgStatus,
        };
        var reqHeaderBytes = Utility.ConvertToBytes(reqHeader);
        pktLength += (uint)reqHeaderBytes.Length;

        byte[]? requestBytes = null;
        if (request is not null)
        {
            requestBytes = Utility.ConvertToBytes(request);
            pktLength += (uint)requestBytes.Length;
        }

        var pktLengthBytes = Utility.ConvertToBytes(pktLength);

        await this.WriteAsync(pktLengthBytes, reqHeaderBytes, requestBytes, cancellationToken);

        return pktLength;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        if (disposing)
        {
            this.writeLock.Dispose();
        }

        this.disposed = true;
    }

    private async Task<byte[]> ReadByteAsync(uint count, CancellationToken cancellationToken)
    {
        var bytes = new byte[count];
        int read = 0;
        int remain = bytes.Length;

        while (read != count)
        {
            var tmp = await this.stream.ReadAsync(bytes, read, remain, cancellationToken);
            if (tmp == 0)
            {
                throw new TaskCanceledException();
            }

            read += tmp;
            remain -= tmp;
        }

        return bytes;
    }

    private async Task WriteAsync(
        byte[] len,
        byte[] header,
        byte[]? body,
        CancellationToken cancellationToken)
    {
        await this.writeLock.WaitAsync(cancellationToken);
        try
        {
            await this.stream.WriteAsync(len, 0, len.Length, cancellationToken);

            await this.stream.WriteAsync(header, 0, header.Length, cancellationToken);

            if (body is not null)
            {
                await this.stream.WriteAsync(body, 0, body.Length, cancellationToken);
            }

            await this.stream.FlushAsync(cancellationToken);
        }
        finally
        {
            this.writeLock.Release();
        }
    }
}
