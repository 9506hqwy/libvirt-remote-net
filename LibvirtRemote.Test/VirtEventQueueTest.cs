namespace LibvirtRemote.Test;

[TestClass]
public class VirtEventQueueTest
{
    [TestMethod]
    public void ClearAsync()
    {
        using var queue = new VirtEventQueue();
        queue.EnqueueAsync(new Event(), default).Wait();
        queue.ClearAsync(default).Wait();
        Assert.AreEqual(0, queue.Count);

        var task = Task.Run(() =>
        {
            Thread.Sleep(100);
            queue.ClearAsync(default).Wait();
            Thread.Sleep(100);
            queue.EnqueueAsync(new Event(), default).Wait();
        });

        /*
           FIXED ME:
           var e = queue.DequeueAsync().Result;
           Assert.AreEqual(0, queue.Count);
         */

        task.Wait();
    }

    [TestMethod]
    public void ContainsAsync()
    {
        var e1 = new Event();
        var e2 = new Event();

        using var queue = new VirtEventQueue();

        Assert.IsFalse(queue.ContainsAsync(e1, default).Result);

        queue.EnqueueAsync(e1, default).Wait();

        Assert.IsTrue(queue.ContainsAsync(e1, default).Result);
        Assert.IsFalse(queue.ContainsAsync(e2, default).Result);
    }

    [TestMethod]
    public void DequeueAsync()
    {
        var e1 = new Event();

        using var queue = new VirtEventQueue();
        queue.EnqueueAsync(e1, default).Wait();

        var e2 = queue.DequeueAsync(default).Result;
        Assert.AreEqual(e1, e2);
    }

    [TestMethod]
    public void Dispose()
    {
        var queue = new VirtEventQueue();
        queue.EnqueueAsync(new Event(), default).Wait();
        queue.Dispose();
        Assert.AreEqual(0, queue.Count);
        queue.Dispose();

        queue = new VirtEventQueue();

        var task = Task.Run(() =>
        {
            Thread.Sleep(100);
            queue.Dispose();
        });

        try
        {
            var e = queue.DequeueAsync(default).Result;
            Assert.Fail();
        }
        catch (AggregateException e) when (e.InnerException is TaskCanceledException)
        {
        }

        task.Wait();
        queue.Dispose();
    }

    [TestMethod]
    public void EnqueueAsync()
    {
        var e1 = new Event();
        var e2 = new Event();

        using var queue = new VirtEventQueue();

        Assert.IsFalse(queue.ContainsAsync(e1, default).Result);

        queue.EnqueueAsync(e1, default).Wait();
        queue.EnqueueAsync(e2, default).Wait();

        Assert.IsTrue(queue.ContainsAsync(e1, default).Result);
        Assert.IsTrue(queue.ContainsAsync(e2, default).Result);
    }

    [TestMethod]
    public void PeekAsync()
    {
        using var queue = new VirtEventQueue();

        queue.EnqueueAsync(new Event(), default).Wait();

        var e1 = queue.PeekAsync(default).Result;
        var e2 = queue.DequeueAsync(default).Result;
        Assert.AreEqual(e1, e2);

        try
        {
            e1 = queue.PeekAsync(default).Result;
            Assert.Fail();
        }
        catch (AggregateException e) when (e.InnerException is InvalidOperationException)
        {
        }
    }

    private class Event : IVirtEvent
    {
        public int GetCallbackId()
        {
            return 0;
        }
    }
}
