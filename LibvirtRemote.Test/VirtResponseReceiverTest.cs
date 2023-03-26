namespace LibvirtRemote.Test;

[TestClass]
public class VirtResponseReceiverTest
{
    [TestMethod]
    public void DeleteEventQueue()
    {
        var e = this.CreateMsg();

        using var receiver = this.CreateReceiver(
            0,
            RemoteProcedure.RemoteProcStoragePoolEventLifecycle,
            VirNetMessageType.VirNetReply,
            VirNetMessageStatus.VirNetOk,
            e);

        Thread.Sleep(300);

        var ret = receiver.DeleteEventQueue(e.GetCallbackId());
        Assert.IsTrue(ret);

        ret = receiver.DeleteEventQueue(e.GetCallbackId());
        Assert.IsFalse(ret);
    }

    [TestMethod]
    public void DisposeEventQueue()
    {
        var e = this.CreateMsg();

        using var receiver = this.CreateReceiver(
            0,
            RemoteProcedure.RemoteProcStoragePoolEventLifecycle,
            VirNetMessageType.VirNetReply,
            VirNetMessageStatus.VirNetOk,
            e);

        Thread.Sleep(300);

        var eventQueue = receiver.GetEventQueue(e.GetCallbackId());
        Assert.AreEqual(1, eventQueue.Count);

        receiver.Dispose();

        Assert.IsTrue(receiver.IsStopped);

        Assert.IsFalse(receiver.DeleteEventQueue(e.GetCallbackId()));
        Assert.AreEqual(0, eventQueue.Count);
    }

    [TestMethod]
    public void DisposeStream()
    {
        using var receiver = this.CreateReceiver(
            100,
            socket =>
            {
                socket.Send(
                    Binding.Constants.RemoteProgram,
                    Binding.Constants.RemoteProtocolVersion,
                    1,
                    (int)RemoteProcedure.RemoteProcStorageVolDownload,
                    VirNetMessageType.VirNetStream,
                    VirNetMessageStatus.VirNetOk,
                    null);
                socket.Send(
                    Binding.Constants.RemoteProgram,
                    Binding.Constants.RemoteProtocolVersion,
                    1,
                    (int)RemoteProcedure.RemoteProcStorageVolDownload,
                    VirNetMessageType.VirNetStream,
                    VirNetMessageStatus.VirNetOk,
                    null);
            });

        receiver.Register(1, true);

        Thread.Sleep(300);

        var stream = receiver.GetStream(1);
        Assert.IsNotNull(stream);
        Assert.IsTrue(stream.IsWriteCompleted);

        receiver.Dispose();

        Assert.IsTrue(receiver.IsStopped);

        try
        {
            receiver.GetStream(1);
            Assert.Fail();
        }
        catch (InvalidOperationException)
        {
        }
    }

    [TestMethod]
    public void GetStream()
    {
        using var receiver = this.CreateReceiver(
            100,
            socket =>
            {
                socket.Send(
                    Binding.Constants.RemoteProgram,
                    Binding.Constants.RemoteProtocolVersion,
                    1,
                    (int)RemoteProcedure.RemoteProcStorageVolDownload,
                    VirNetMessageType.VirNetStream,
                    VirNetMessageStatus.VirNetOk,
                    null);
                socket.Send(
                    Binding.Constants.RemoteProgram,
                    Binding.Constants.RemoteProtocolVersion,
                    1,
                    (int)RemoteProcedure.RemoteProcStorageVolDownload,
                    VirNetMessageType.VirNetStream,
                    VirNetMessageStatus.VirNetOk,
                    null);
            });

        receiver.Register(1, true);

        Thread.Sleep(300);

        var stream = receiver.GetStream(1);
        Assert.IsNotNull(stream);
        Assert.IsTrue(stream.IsWriteCompleted);

        try
        {
            receiver.GetStream(2);
            Assert.Fail();
        }
        catch (InvalidOperationException)
        {
        }
    }

    [TestMethod]
    public void GetEventQueue()
    {
        var e = this.CreateMsg();

        using var receiver = this.CreateReceiver(
            0,
            RemoteProcedure.RemoteProcStoragePoolEventLifecycle,
            VirNetMessageType.VirNetReply,
            VirNetMessageStatus.VirNetOk,
            e);

        Thread.Sleep(300);

        var eventQueue1 = receiver.GetEventQueue(e.GetCallbackId());
        Assert.AreEqual(1, eventQueue1.Count);

        var eventQueue2 = receiver.GetEventQueue(e.GetCallbackId());
        Assert.AreEqual(eventQueue1, eventQueue2);

        var eventQueue3 = receiver.GetEventQueue(3);
        Assert.AreNotEqual(eventQueue1, eventQueue3);
    }

    [TestMethod]
    public void RegisterUnRegister()
    {
        using var receiver = this.CreateReceiver(
            0,
            RemoteProcedure.RemoteProcStorageVolDownload,
            VirNetMessageType.VirNetReply,
            VirNetMessageStatus.VirNetOk,
            null);

        receiver.Register(1, true);
        receiver.Unregister(1, true);
    }

    private RemoteStoragePoolEventLifecycleMsg CreateMsg()
    {
        return new RemoteStoragePoolEventLifecycleMsg
        {
            CallbackId = 1,
            Detail = 2,
            Event = 3,
            Pool = new RemoteNonnullStoragePool
            {
                Name = "test",
                Uuid = Guid.NewGuid().ToByteArray(),
            },
        };
    }

    private VirtResponseReceiver CreateReceiver(
        int delayRead,
        RemoteProcedure proc,
        VirNetMessageType type,
        VirNetMessageStatus status,
        object? requst)
    {
        return this.CreateReceiver(delayRead, socket =>
        {
            socket.Send(
                Binding.Constants.RemoteProgram,
                Binding.Constants.RemoteProtocolVersion,
                1,
                (int)proc,
                type,
                status,
                requst);
        });
    }

    private VirtResponseReceiver CreateReceiver(
        int delayRead,
        Action<VirtSocket> setupSocket)
    {
        var mem = new DelayMemoryStream(delayRead);
        var socket = new VirtSocket(mem);

        setupSocket(socket);

        mem.Seek(0, SeekOrigin.Begin);

        return new VirtResponseReceiver(socket);
    }
}
