using System.Net.Sockets;
using LibvirtRemote;
using Xdr;

try
{
    await Work();
}
catch (Exception e)
{
    await Console.Error.WriteLineAsync(string.Format("{0}", e));
}

static async Task Work()
{
    using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
    await socket.ConnectAsync(new UnixDomainSocketEndPoint("/var/run/libvirt/libvirt-sock"));

    using var stream = new NetworkStream(socket);

    using var client = new VirtClient(stream);

    await client.ConnectOpenAsync(new XdrOption<string>("qemu:///system"), 0, default);

    (var domains, var _) = await client.ConnectListAllDomainsAsync(1, 1 | 2, default);
    foreach (var domain in domains)
    {
        await Console.Out.WriteLineAsync(domain.Name);
    }

    await client.ConnectCloseAsync(default);

    await socket.DisconnectAsync(false);
}
