namespace LibvirtRemote.Test;

using System.Net.Sockets;
using Xdr;

[TestClass]
public class ClientTest
{
    private VirtClient? client;

    private NetworkStream? stream;

    private TcpClient? tcp;

    [TestInitialize]
    public void TestInitialize()
    {
        this.tcp = new TcpClient("127.0.0.1", 16509);
        this.stream = this.tcp.GetStream();
        this.client = new VirtClient(this.stream);
        this.client.ConnectOpenAsync(new XdrOption<string>("qemu:///system"), 0, default).Wait();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        this.client!.ConnectCloseAsync(default).Wait();
        this.client.Dispose();
        this.stream!.Dispose();
        this.tcp!.Dispose();
    }

    [TestMethod]
    [Ignore]
    public void ConnectGetTypeAsync()
    {
        var type = this.client!.ConnectGetTypeAsync(default).Result;
        Assert.AreEqual("QEMU", type);
    }

    [TestMethod]
    [Ignore]
    public void ConnectGetVersionAsync()
    {
        var ver = this.client!.ConnectGetVersionAsync(default).Result;
        Assert.IsNotNull(ver);
    }

    [TestMethod]
    [Ignore]
    public void DomainGetCpuStatsAsync()
    {
        (var doms, var _) = this.client!.ConnectListAllDomainsAsync(1, 1 | 2, default).Result;
        var dom = doms.First();

        (var _, var count) = this.client.DomainGetCpuStatsAsync(dom, 0, 0, 0, 0, default).Result;
        (var _, var nparams) = this.client.DomainGetCpuStatsAsync(dom, 0, 0, 1, 0, default).Result;
        (var stats, var _) = this.client.DomainGetCpuStatsAsync(dom, (uint)nparams, 0, (uint)count, 0, default).Result;
        Assert.IsNotNull(stats[0].Value.Ul);
    }

    [TestMethod]
    [Ignore]
    public void StoragePoolEventLifecycle()
    {
        var callbackId = this.client!.ConnectStoragePoolEventRegisterAnyAsync(0, null, default).Result;
        var eventStream = this.client.GetEventStream(callbackId);

        var e = eventStream.ReadAsync(default).Result;
        Assert.IsNotNull(e);

        this.client.DeleteEventStream(eventStream);
    }

    [TestMethod]
    [Ignore]
    public void StorageVolDownload()
    {
        var vol = this.client!.StorageVolLookupByPathAsync("/var/lib/libvirt/images/test.qcow2", default).Result;
        using var virStream = this.client.StorageVolDownloadAsync(vol, 0, 0, 0, default).Result;
        using var file = File.OpenWrite("/tmp/test.qcow2");
        virStream.CopyTo(file);
        file.Flush();
    }

    [TestMethod]
    [Ignore]
    public void StorageVolUpload()
    {
        var vol = this.client!.StorageVolLookupByPathAsync("/var/lib/libvirt/images/test.qcow2", default).Result;
        using var virStream = this.client.StorageVolUploadAsync(vol, 0, 0, 0, default).Result;
        using var file = File.OpenRead("/tmp/test.qcow2");
        file.CopyTo(virStream);
        virStream.WriteCompletedAsync(default).Wait();
    }
}
