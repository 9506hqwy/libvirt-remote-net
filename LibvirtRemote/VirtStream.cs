namespace LibvirtRemote;

using Protocol;

public class VirtStream : IDisposable
{
    private readonly ManualResetEventSlim hasItem;

    private readonly Queue<byte> inner;

    private readonly VirtResponseReceiver receiver;

    private readonly uint serial;

    private bool disposed;

    private VirNetMessageError? error;

    public VirtStream(uint serial, VirtResponseReceiver receiver)
    {
        this.serial = serial;
        this.receiver = receiver;

        this.hasItem = new ManualResetEventSlim();
        this.inner = new Queue<byte>();

        this.error = null;
        this.IsWriteCompleted = false;
    }

    public bool IsWriteCompleted { get; private set; }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        this.ThrowIfError();
        this.receiver.ThrowIfReceiverIsStopped();

        if (!this.IsWriteCompleted)
        {
            this.hasItem.Wait();
            this.ThrowIfError();
            this.receiver.ThrowIfReceiverIsStopped();
        }

        lock (this.inner)
        {
            int index = offset;
            for (; index < offset + count; index++)
            {
                try
                {
                    buffer[index] = this.inner.Dequeue();
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            }

            if (this.inner.Count == 0)
            {
                this.hasItem.Reset();
            }

            return index - offset;
        }
    }

    public void ThrowIfError()
    {
        if (this.error is not null)
        {
            throw new VirtException(this.error);
        }
    }

    public void Write(byte[] buffer)
    {
        this.receiver.ThrowIfReceiverIsStopped();

        lock (this.inner)
        {
            Array.ForEach(buffer, this.inner.Enqueue);
            this.hasItem.Set();
        }
    }

    public void Write(VirNetStreamHole hole)
    {
        var zeroed = new byte[hole.Length];
        this.Write(zeroed);
    }

    public void WriteCompleted()
    {
        lock (this.inner)
        {
            this.IsWriteCompleted = true;
            this.hasItem.Set();
        }
    }

    public void WriteError(Exception exception)
    {
        lock (this.inner)
        {
            this.error = new VirNetMessageError
            {
                Code = exception.HResult,
                Message = new Xdr.XdrOption<string>(exception.Message),
            };
            this.hasItem.Set();
        }
    }

    public void WriteError(VirNetMessageError error)
    {
        lock (this.inner)
        {
            this.error = error;
            this.hasItem.Set();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        if (disposing)
        {
            this.inner.Clear();
            this.receiver.RemoveStream(this.serial);
            this.hasItem.Dispose();
        }

        this.disposed = true;
    }
}
