using System.Net.Sockets;
using Binding;
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
    using var tcp = new TcpClient("127.0.0.1", 16509);

    using var stream = tcp.GetStream();

    using var client = new VirtClient(stream);

    await client.ConnectOpenAsync(new XdrOption<string>("qemu:///system"), 0, default);

    var callbackId = await client.ConnectDomainEventCallbackRegisterAnyAsync(0, null, default);
    var eventStream = client.GetEventStream(callbackId);

    var e = await eventStream.ReadAsync(default);
    if (e is RemoteDomainEventCallbackLifecycleMsg m)
    {
        await Console.Out.WriteLineAsync(string.Format("{0} event={1} detail={2}", m.Msg.Dom.Name, m.Msg.Event, m.Msg.Detail));
    }

    _ = client.DeleteEventStream(eventStream);

    await client.ConnectDomainEventCallbackDeregisterAnyAsync(callbackId, default);

    await client.ConnectCloseAsync(default);
}
