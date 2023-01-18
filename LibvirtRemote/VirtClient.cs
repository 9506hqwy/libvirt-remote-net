namespace LibvirtRemote;

using Binding;
using Protocol;

public partial class VirtClient : IDisposable
{
    private readonly VirtResponseReceiver receiver;

    private readonly object serialLock;

    private uint serial;

    private bool disposed;

    public VirtClient(Stream stream)
    {
        this.disposed = false;

        this.Socket = new VirtSocket(stream);
        this.serial = 0;
        this.serialLock = new object();

        this.receiver = new VirtResponseReceiver(this.Socket);
    }

    internal VirtSocket Socket { get; }

    public bool DeleteEventStream(int callbackId)
    {
        return this.receiver.DeleteEventQueue(callbackId);
    }

    public bool DeleteEventStream(VirtEventStream stream)
    {
        return this.DeleteEventStream(stream.CallbackId);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public VirtEventStream GetEventStream(int callbackId)
    {
        this.receiver.ThrowIfReceiverIsStopped();

        var queue = this.receiver.GetEventQueue(callbackId);
        if (queue is null)
        {
            throw new InvalidOperationException();
        }

        return new VirtEventStream(callbackId, queue);
    }

    internal async Task<T?> CallAsync<T>(
        RemoteProcedure proc,
        object? request,
        CancellationToken cancellationToken)
    {
        var res = await this.CallAsync(false, proc, request, cancellationToken);
        return res.ConvertTo<T>();
    }

    internal async Task<VirtNetStream> CallWithStreamAsync<T>(
        RemoteProcedure proc,
        object? request,
        CancellationToken cancellationToken)
    {
        var res = await this.CallAsync(true, proc, request, cancellationToken);
        res.ConvertTo<T>();

        var stream = this.receiver.GetStream(res.Header.Serial);
        return new VirtNetStream(this, stream, res.Header);
    }

    internal VirtResponse Request(
        bool isStream,
        uint serial,
        int proc,
        VirNetMessageType type,
        VirNetMessageStatus status,
        object? request)
    {
        return this.RequestAsync(isStream, serial, proc, type, status, request, default).Result;
    }

    internal async Task<VirtResponse> RequestAsync(
        bool isStream,
        uint serial,
        int proc,
        VirNetMessageType type,
        VirNetMessageStatus status,
        object? request,
        CancellationToken cancellationToken)
    {
        var task = this.receiver.Register(serial, isStream);

        cancellationToken.Register(() =>
        {
            if (!task.IsCompleted)
            {
                task.SetCanceled();
            }
        });

        try
        {
            await this.Socket.SendAsync(
                serial,
                proc,
                type,
                status,
                request,
                cancellationToken);
        }
        catch
        {
            this.receiver.Unregister(serial, isStream);
            throw;
        }

        return await task.GetResultAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        if (disposing)
        {
            this.receiver.Dispose();
            this.Socket.Dispose();
        }

        this.disposed = true;
    }

    private Task<VirtResponse> CallAsync(
        bool isStream,
        RemoteProcedure proc,
        object? request,
        CancellationToken cancellationToken)
    {
        this.receiver.ThrowIfReceiverIsStopped();

        var serial = this.IncrementSerial();

        return this.RequestAsync(
            isStream,
            serial,
            (int)proc,
            VirNetMessageType.VirNetCall,
            VirNetMessageStatus.VirNetOk,
            request,
            cancellationToken);
    }

    private uint IncrementSerial()
    {
        lock (this.serialLock)
        {
            this.serial += 1;
            return this.serial;
        }
    }
}
