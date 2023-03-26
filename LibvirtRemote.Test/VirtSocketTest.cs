namespace LibvirtRemote.Test;

[TestClass]
public class VirtSocketTest
{
    [TestMethod]
    public void ReceiveNotEnoughData()
    {
        var mem = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x01 });
        using var socket = new VirtSocket(mem);

        mem.Seek(0, SeekOrigin.Begin);

        var res = socket.Receive();
        Assert.IsNull(res);
    }

    [TestMethod]
    public void ReceivePending()
    {
        var mem = new MemoryStream(new byte[] { 0x00, 0x00, 0x00 });
        using var socket = new VirtSocket(mem);

        mem.Seek(0, SeekOrigin.Begin);

        try
        {
            socket.Receive();
            Assert.Fail();
        }
        catch (AggregateException e) when (e.InnerException is TaskCanceledException)
        {
        }
    }

    [TestMethod]
    public void ReceiveZeroed()
    {
        var mem = new MemoryStream();
        using var socket = new VirtSocket(mem);

        mem.Seek(0, SeekOrigin.Begin);

        try
        {
            socket.Receive();
            Assert.Fail();
        }
        catch (AggregateException e) when (e.InnerException is TaskCanceledException)
        {
        }
    }

    [TestMethod]
    public void SendNoBody()
    {
        var mem = new MemoryStream();
        using var socket = new VirtSocket(mem);

        var n = socket.Send(
            Binding.Constants.RemoteProgram,
            Binding.Constants.RemoteProtocolVersion,
            1,
            2,
            VirNetMessageType.VirNetCall,
            VirNetMessageStatus.VirNetOk,
            null);
        Assert.AreEqual(28u, n);

        mem.Seek(0, SeekOrigin.Begin);

        var res = socket.Receive();
        Assert.AreEqual(Binding.Constants.RemoteProgram, res!.Header.Prog);
        Assert.AreEqual(Binding.Constants.RemoteProtocolVersion, res.Header.Vers);
        Assert.AreEqual(1u, res.Header.Serial);
        Assert.AreEqual(2, res.Header.Proc);
        Assert.AreEqual(VirNetMessageType.VirNetCall, res.Header.Type);
        Assert.AreEqual(VirNetMessageStatus.VirNetOk, res.Header.Status);
        Assert.IsNull(res.Body);
    }

    [TestMethod]
    public void SendWithBody()
    {
        var mem = new MemoryStream();
        using var socket = new VirtSocket(mem);

        var n = socket.Send(
            Binding.Constants.RemoteProgram,
            Binding.Constants.RemoteProtocolVersion,
            1,
            2,
            VirNetMessageType.VirNetCall,
            VirNetMessageStatus.VirNetOk,
            1);
        Assert.AreEqual(32u, n);

        mem.Seek(0, SeekOrigin.Begin);

        var res = socket.Receive();
        Assert.AreEqual(Binding.Constants.RemoteProgram, res!.Header.Prog);
        Assert.AreEqual(Binding.Constants.RemoteProtocolVersion, res.Header.Vers);
        Assert.AreEqual(1u, res.Header.Serial);
        Assert.AreEqual(2, res.Header.Proc);
        Assert.AreEqual(VirNetMessageType.VirNetCall, res.Header.Type);
        Assert.AreEqual(VirNetMessageStatus.VirNetOk, res.Header.Status);
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x01 }, res.Body);
    }

    [TestMethod]
    public void SendMultiThreaded()
    {
        var mem = new MemoryStream();
        using var socket = new VirtSocket(mem);

        using var blocking = new ManualResetEvent(false);

        var t1 = Task.Run(() =>
        {
            blocking.WaitOne();
            socket.Send(
                Binding.Constants.RemoteProgram,
                Binding.Constants.RemoteProtocolVersion,
                1,
                2,
                VirNetMessageType.VirNetCall,
                VirNetMessageStatus.VirNetOk,
                null);
        });

        var t2 = Task.Run(() =>
        {
            blocking.WaitOne();
            socket.Send(
                Binding.Constants.RemoteProgram,
                Binding.Constants.RemoteProtocolVersion,
                3,
                4,
                VirNetMessageType.VirNetCall,
                VirNetMessageStatus.VirNetOk,
                null);
        });

        blocking.Set();
        t1.Wait();
        t2.Wait();

        mem.Seek(0, SeekOrigin.Begin);

        var res1 = socket.Receive();
        var res2 = socket.Receive();
        var ret =
            (res1!.Header.Serial == 1u && res2!.Header.Serial == 3u) ||
            (res1.Header.Serial == 3u && res2!.Header.Serial == 1u);
        Assert.IsTrue(ret);
    }
}
