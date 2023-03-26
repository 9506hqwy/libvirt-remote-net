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

    internal async Task<TRet?> CallAsync<TRet, TProc>(
        TProc proc,
        object? request,
        CancellationToken cancellationToken)
    {
        var res = await this.CallAsync(false, proc, request, cancellationToken);
        return res.ConvertTo<TRet>();
    }

    internal async Task<Tuple<VirtNetStream, TRet?>> CallWithStreamAsync<TRet, TProc>(
        TProc proc,
        object? request,
        CancellationToken cancellationToken)
    {
        var res = await this.CallAsync(true, proc, request, cancellationToken);
        var val = res.ConvertTo<TRet>();

        var stream = this.receiver.GetStream(res.Header.Serial);
        var virStream = new VirtNetStream(this, stream, res.Header);
        return new Tuple<VirtNetStream, TRet?>(virStream, val);
    }

    internal VirtResponse Request<TProc>(
        bool isStream,
        uint serial,
        TProc proc,
        VirNetMessageType type,
        VirNetMessageStatus status,
        object? request)
    {
        return this.RequestAsync(isStream, serial, proc, type, status, request, default).Result;
    }

    internal async Task<VirtResponse> RequestAsync<TProc>(
        bool isStream,
        uint serial,
        TProc proc,
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
                this.GetProg(proc),
                this.GetProtoVersion(proc),
                serial,
                this.GetProcNumber(proc),
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

    private Task<VirtResponse> CallAsync<TProc>(
        bool isStream,
        TProc proc,
        object? request,
        CancellationToken cancellationToken)
    {
        this.receiver.ThrowIfReceiverIsStopped();

        var serial = this.IncrementSerial();

        return this.RequestAsync(
            isStream,
            serial,
            proc,
            VirNetMessageType.VirNetCall,
            VirNetMessageStatus.VirNetOk,
            request,
            cancellationToken);
    }

    private int GetProcNumber<TProc>(TProc proc)
    {
        return proc switch
        {
            LxcProcedure lxc => (int)lxc,
            QemuProcedure qemu => (int)qemu,
            RemoteProcedure remote => (int)remote,
            _ => throw new InvalidProgramException(),
        };
    }

    private uint GetProtoVersion<TProc>(TProc proc)
    {
        return proc switch
        {
            LxcProcedure _ => Binding.Constants.LxcProtocolVersion,
            QemuProcedure _ => Binding.Constants.QemuProtocolVersion,
            RemoteProcedure _ => Binding.Constants.RemoteProtocolVersion,
            _ => throw new InvalidProgramException(),
        };
    }

    private uint GetProg<TProc>(TProc proc)
    {
        return proc switch
        {
            LxcProcedure _ => Binding.Constants.LxcProgram,
            QemuProcedure _ => Binding.Constants.QemuProgram,
            RemoteProcedure _ => Binding.Constants.RemoteProgram,
            _ => throw new InvalidProgramException(),
        };
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
