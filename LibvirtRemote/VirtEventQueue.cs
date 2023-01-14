namespace LibvirtRemote;

using Binding;

public class VirtEventQueue : IDisposable
{
    private const int DefaultMaxCapacity = 1000;

    private readonly Queue<IVirtEvent> inner;

    private readonly SemaphoreSlim semaphore;

    private TaskCompletionSource<bool> hasItem;

    private bool disposed;

    public VirtEventQueue()
        : this(VirtEventQueue.DefaultMaxCapacity)
    {
    }

    public VirtEventQueue(int capacity)
    {
        this.disposed = false;

        this.Capacity = capacity > 0 ? capacity : VirtEventQueue.DefaultMaxCapacity;
        this.inner = new Queue<IVirtEvent>();

        this.hasItem = this.ResetHasItem();
        this.semaphore = new SemaphoreSlim(1, 1);
    }

    public int Capacity { get; }

    public int Count => this.inner.Count;

    public Task ClearAsync()
    {
        return this.LockAsync(() =>
        {
            this.inner.Clear();
            this.ReleaseWait(false);
            this.ResetHasItem();
            return true;
        });
    }

    public Task<bool> ContainsAsync(IVirtEvent item)
    {
        return this.LockAsync(() =>
        {
            return this.inner.Contains(item);
        });
    }

    public async Task<IVirtEvent> DequeueAsync()
    {
        await this.hasItem.Task;

        return await this.LockAsync(() =>
        {
            if (this.inner.Count == 1)
            {
                this.ResetHasItem();
            }

            return this.inner.Dequeue();
        });
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public Task EnqueueAsync(IVirtEvent item)
    {
        return this.LockAsync(() =>
        {
            while (this.inner.Count >= this.Capacity)
            {
                this.inner.Dequeue();
            }

            this.inner.Enqueue(item);

            this.ReleaseWait(false);

            return true;
        });
    }

    public Task<IVirtEvent> PeekAsync()
    {
        return this.LockAsync(() =>
        {
            return this.inner.Peek();
        });
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
            this.ReleaseWait(true);
            this.semaphore.Dispose();
        }

        this.disposed = true;
    }

    private async Task<T> LockAsync<T>(Func<T> action)
    {
        await this.semaphore.WaitAsync();
        try
        {
            return action();
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    private void ReleaseWait(bool cancel)
    {
        if (!this.hasItem.Task.IsCompleted)
        {
            if (cancel)
            {
                this.hasItem.SetCanceled();
            }
            else
            {
                this.hasItem.SetResult(true);
            }
        }
    }

    private TaskCompletionSource<bool> ResetHasItem()
    {
        // https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        this.hasItem = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        return this.hasItem;
    }
}
