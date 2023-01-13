namespace LibvirtRemote.Test;

[TestClass]
public class VirtEventQueueTest
{
    [TestMethod]
    public void ClearAsync()
    {
        var queue = new VirtEventQueue();
        queue.EnqueueAsync(new Event()).Wait();
        queue.ClearAsync().Wait();
        Assert.AreEqual(0, queue.Count);

        var task = Task.Run(() =>
        {
            Thread.Sleep(100);
            queue.ClearAsync().Wait();
            Thread.Sleep(100);
            queue.EnqueueAsync(new Event()).Wait();
        });

        // FIXED ME:
        // var e = queue.DequeueAsync().Result;
        // Assert.AreEqual(0, queue.Count);

        task.Wait();
    }

    [TestMethod]
    public void ContainsAsync()
    {
        var e1 = new Event();
        var e2 = new Event();

        var queue = new VirtEventQueue();

        Assert.IsFalse(queue.ContainsAsync(e1).Result);

        queue.EnqueueAsync(e1).Wait();

        Assert.IsTrue(queue.ContainsAsync(e1).Result);
        Assert.IsFalse(queue.ContainsAsync(e2).Result);
    }

    [TestMethod]
    public void DequeueAsync()
    {
        var e1 = new Event();

        var queue = new VirtEventQueue();
        queue.EnqueueAsync(e1).Wait();

        var e2 = queue.DequeueAsync().Result;
        Assert.AreEqual(e1, e2);
    }

    [TestMethod]
    public void Dispose()
    {
        var queue = new VirtEventQueue();
        queue.EnqueueAsync(new Event()).Wait();
        queue.Dispose();
        Assert.AreEqual(0, queue.Count);

        queue = new VirtEventQueue();

        var task = Task.Run(() =>
        {
            Thread.Sleep(100);
            queue.Dispose();
        });

        try
        {
            var e = queue.DequeueAsync().Result;
            Assert.Fail();
        }
        catch (AggregateException e) when (e.InnerException is TaskCanceledException)
        {
        }

        task.Wait();
    }

    [TestMethod]
    public void EnqueueAsync()
    {
        var e1 = new Event();
        var e2 = new Event();

        var queue = new VirtEventQueue();

        Assert.IsFalse(queue.ContainsAsync(e1).Result);

        queue.EnqueueAsync(e1).Wait();
        queue.EnqueueAsync(e2).Wait();

        Assert.IsTrue(queue.ContainsAsync(e1).Result);
        Assert.IsTrue(queue.ContainsAsync(e2).Result);
    }

    [TestMethod]
    public void PeekAsync()
    {
        var queue = new VirtEventQueue();

        queue.EnqueueAsync(new Event()).Wait();

        var e1 = queue.PeekAsync().Result;
        var e2 = queue.DequeueAsync().Result;
        Assert.AreEqual(e1, e2);

        try
        {
            e1 = queue.PeekAsync().Result;
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
