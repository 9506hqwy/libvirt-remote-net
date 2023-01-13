namespace LibvirtRemote;

using Binding;
using Protocol;

public class VirtResponseReceiver : IDisposable
{
    private readonly Dictionary<int, VirtEventQueue> events;

    private readonly Task listener;

    private readonly CancellationTokenSource listening;

    private readonly Dictionary<uint, VirtResponseTask> requests;

    private readonly VirtSocket socket;

    private readonly Dictionary<uint, VirtStream> streams;

    private bool disposed;

    public VirtResponseReceiver(VirtSocket socket)
    {
        this.disposed = false;

        this.socket = socket;

        this.events = new Dictionary<int, VirtEventQueue>();
        this.requests = new Dictionary<uint, VirtResponseTask>();
        this.streams = new Dictionary<uint, VirtStream>();

        this.listening = new CancellationTokenSource();
        this.listener = Task.Run(this.Listen);
    }

    public bool IsStopped => this.listener.IsCompleted;

    public bool DeleteEventQueue(int callbackId)
    {
        VirtEventQueue? queue = null;

        lock (this.events)
        {
            if (this.events.TryGetValue(callbackId, out queue))
            {
                this.events.Remove(callbackId);
            }
        }

        queue?.Dispose();

        return queue is not null;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public VirtStream GetStream(uint serial)
    {
        if (this.TryGetStream(serial, out var stream))
        {
            return stream;
        }

        throw new InvalidOperationException();
    }

    public VirtEventQueue GetEventQueue(int callbackId)
    {
        return this.AddOrGetEventQueue(callbackId);
    }

    public VirtResponseTask Register(uint serial, bool isStream)
    {
        if (isStream)
        {
            lock (this.streams)
            {
                this.streams[serial] = new VirtStream(serial, this);
            }
        }

        var task = new VirtResponseTask();

        lock (this.requests)
        {
            this.requests[serial] = task;
        }

        return task;
    }

    public VirtStream? RemoveStream(uint serial)
    {
        lock (this.streams)
        {
            if (this.streams.TryGetValue(serial, out var stream))
            {
                this.streams.Remove(serial);
                return stream;
            }
        }

        return null;
    }

    public void ThrowIfReceiverIsStopped()
    {
        if (this.IsStopped)
        {
            throw new InvalidOperationException();
        }
    }

    public void Unregister(uint serial, bool isStream)
    {
        if (isStream)
        {
            if (this.TryGetStream(serial, out var stream))
            {
                stream.Dispose();
            }
        }

        if (this.TryRemoveRequest(serial, out var request))
        {
            request.SetCanceled();
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
            this.listening.Cancel(true);

            this.listener.Wait();
            this.listener.Dispose();

            this.listening.Dispose();

            lock (this.events)
            {
                foreach (var queue in this.events.Values)
                {
                    queue.Dispose();
                }

                this.events.Clear();
            }

            lock (this.streams)
            {
                foreach (var stream in this.streams.Values)
                {
                    // Dispose() is remove from streams.
                    // lock statement is twice.
                    stream.Dispose();
                }
            }
        }

        this.disposed = true;
    }

    private VirtEventQueue AddOrGetEventQueue(int callbackId)
    {
        VirtEventQueue? queue = null;

        lock (this.events)
        {
            if (!this.events.TryGetValue(callbackId, out queue))
            {
                queue = new VirtEventQueue();
                this.events.Add(callbackId, queue);
            }
        }

        return queue;
    }

    private async Task<bool> EnqueueEvent(VirtResponse response)
    {
        IVirtEvent? item = response.ConvertToEvent();
        if (item is null)
        {
            return false;
        }

        var callbackId = item.GetCallbackId();

        await this.AddOrGetEventQueue(callbackId).EnqueueAsync(item);

        return true;
    }

    private async Task Listen()
    {
        try
        {
            while (!this.listening.IsCancellationRequested)
            {
                var res = await this.socket.ReceiveAsync(this.listening.Token);
                if (res is null)
                {
                    continue;
                }

                if (await this.EnqueueEvent(res))
                {
                    continue;
                }

                if (this.TryHandleRequest(res))
                {
                    continue;
                }

                this.TryHandleStream(res);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            lock (this.requests)
            {
                foreach (var req in this.requests.Values)
                {
                    req.SetException(e);
                }

                this.requests.Clear();
            }

            lock (this.streams)
            {
                foreach (var stream in this.streams.Values)
                {
                    stream.WriteError(e);
                }
            }
        }
    }

    private bool TryGetStream(uint serial, out VirtStream stream)
    {
        lock (this.streams)
        {
            return this.streams.TryGetValue(serial, out stream);
        }
    }

    private bool TryHandleRequest(VirtResponse response)
    {
        if (this.TryRemoveRequest(response.Header.Serial, out var request))
        {
            request.SetResult(response);
            return true;
        }

        return false;
    }

    private bool TryHandleStream(VirtResponse response)
    {
        if (!this.TryGetStream(response.Header.Serial, out var stream))
        {
            return false;
        }

        if (response.Header.Status == VirNetMessageStatus.VirNetError)
        {
            var errMsg = Utility.ConvertFromBytes<VirNetMessageError>(response.Body!);
            stream.WriteError(errMsg);
            return true;
        }
        else if (response.Body is null)
        {
            stream.WriteCompleted();
            return true;
        }
        else if (response.Header.Type == VirNetMessageType.VirNetStream)
        {
            stream.Write(response.Body!);
            return true;
        }
        else if (response.Header.Type == VirNetMessageType.VirNetStreamHole)
        {
            var hole = response.ConvertTo<VirNetStreamHole>();
            stream.Write(hole!);
            return true;
        }

        return false;
    }

    private bool TryRemoveRequest(uint serial, out VirtResponseTask request)
    {
        lock (this.requests)
        {
            if (this.requests.TryGetValue(serial, out request))
            {
                this.requests.Remove(serial);
                return true;
            }
        }

        return false;
    }
}
