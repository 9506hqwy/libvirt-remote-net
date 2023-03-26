using System.Net.Sockets;
using LibvirtRemote;
using Xdr;

try
{
    await Work(args);
}
catch (Exception e)
{
    await Console.Error.WriteLineAsync(string.Format("{0}", e));
}

async Task Work(string[] args)
{
    using var tcp = new TcpClient("127.0.0.1", 16509);

    using var stream = tcp.GetStream();

    using var client = new VirtClient(stream);

    await client.ConnectOpenAsync(new XdrOption<string>("qemu:///system"), 0, default);

    var domain = await client.DomainLookupByNameAsync("test", default);
    var cmd = "{\"execute\": \"guest-info\"}";

    var output = await client.DomainAgentCommandAsync(domain, cmd, 60, 0, default);

    await Console.Out.WriteLineAsync(output?.Value ?? string.Empty);

    await client.ConnectCloseAsync(default);
}
