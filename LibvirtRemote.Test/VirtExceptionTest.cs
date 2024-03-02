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

        mem.Seek(0, SeekOrigin.Begin);

        var ret = (VirtException)formater.Deserialize(mem);

        Assert.IsNotNull(ret);
        Assert.IsNotNull(ret.Error);
        Assert.AreEqual(exp.Error.Code, ret.Error.Code);
        Assert.AreEqual(exp.Error.Message.Value, ret.Error.Message.Value);
    }
#pragma warning restore SYSLIB0011
}
