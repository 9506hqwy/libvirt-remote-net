namespace LibvirtRemote;

using Binding;

public class VirtEventStream
{
    private readonly VirtEventQueue queue;

    internal VirtEventStream(int callbackId, VirtEventQueue queue)
    {
        this.CallbackId = callbackId;
        this.queue = queue;
    }

    public int CallbackId { get; }

    public int Length => this.queue.Count;

    public async Task<IVirtEvent> ReadAsync()
    {
        return await this.queue.DequeueAsync();
    }

    internal async Task WriteAsync(IVirtEvent item)
    {
        await this.queue.EnqueueAsync(item);
    }
}
