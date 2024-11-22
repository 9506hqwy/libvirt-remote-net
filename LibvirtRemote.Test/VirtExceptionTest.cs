namespace LibvirtRemote.Test;

using System.Runtime.Serialization.Formatters.Binary;

[TestClass]
public class VirtExceptionTest
{
#pragma warning disable SYSLIB0011
    [TestMethod]
    [Ignore]
    public void Serialize()
    {
        var error = new VirNetMessageError
        {
            Code = 1,
            Message = new XdrOption<string>("Message"),
        };

        var exp = new VirtException(error);

        var mem = new MemoryStream();

        var formater = new BinaryFormatter();
        formater.Serialize(mem, exp);

        _ = mem.Seek(0, SeekOrigin.Begin);

#pragma warning disable CA2300
#pragma warning disable CA2301
        var ret = (VirtException)formater.Deserialize(mem);
#pragma warning restore CA2300
#pragma warning restore CA2301

        Assert.IsNotNull(ret);
        Assert.IsNotNull(ret.Error);
        Assert.AreEqual(exp.Error.Code, ret.Error.Code);
        Assert.AreEqual(exp.Error.Message.Value, ret.Error.Message.Value);
    }
#pragma warning restore SYSLIB0011
}
