namespace Binding;

using System.Reflection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class VirtEventAttribute : Attribute
{
    static VirtEventAttribute()
    {
        VirtEventAttribute.Attrs = typeof(VirtEventAttribute).Assembly
            .GetTypes()
            .Where(typeof(IVirtEvent).IsAssignableFrom)
            .Where(t => t.IsClass)
            .Select(VirtEventAttribute.Get)
            .ToArray();
    }

    public VirtEventAttribute(uint prog, QemuProcedure proc)
    {
        this.Prog = prog;
        this.Proc = (int)proc;
    }

    public VirtEventAttribute(uint prog, RemoteProcedure proc)
    {
        this.Prog = prog;
        this.Proc = (int)proc;
    }

    public int Proc { get; }

    public uint Prog { get; }

    public Type? Type { get; private set; }

    internal static VirtEventAttribute[] Attrs { get; }

    private static VirtEventAttribute Get(Type type)
    {
        var attr = type.GetCustomAttribute<VirtEventAttribute>();
        attr.Type = type;
        return attr;
    }
}
