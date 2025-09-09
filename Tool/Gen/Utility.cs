namespace Gen;

using Binding;
using System.Reflection;
using System.Text.RegularExpressions;

internal static class Utility
{
    internal const string LxcPrefix = "Lxc";

    internal const string QemuPrefix = "Qemu";

    internal const string RemotePrefix = "Remote";

    private static readonly Assembly BindingAsm = typeof(RemoteProcedure).Assembly;

    internal static IEnumerable<Tuple<T, Type>> EnumerateEvent<T>(string prefix)
    {
        foreach ((var procName, var proc) in EnumerateProc<T>($"{prefix}Proc"))
        {
            var eventType = FindEventType(prefix, procName);
            if (eventType is null)
            {
                continue;
            }

            if (eventType.GetProperty(Code.EventCallbackId) is null)
            {
                continue;
            }

            yield return new Tuple<T, Type>((T)proc.GetValue(null)!, eventType);
        }
    }

    internal static IEnumerable<Tuple<T, string, Type?, Type?>> EnumerateMethod<T>(string prefix)
    {
        foreach ((var procName, var proc) in EnumerateProc<T>($"{prefix}Proc"))
        {
            var eventType = FindEventType(prefix, procName);
            if (eventType is not null)
            {
                continue;
            }

            var argType = FindFuncArgsType(prefix, procName);
            var retType = FindFuncRetType(prefix, procName);
            yield return new Tuple<T, string, Type?, Type?>(
                (T)proc.GetValue(null)!,
                procName,
                argType,
                retType);
        }
    }

    internal static string ToArgName(string value)
    {
        return ToLowerCamelCase(value);
    }

    internal static string ToPropertyName(string value)
    {
        return ToUpperCamelCase(value);
    }

    private static IEnumerable<Tuple<string, FieldInfo>> EnumerateProc<T>(string prefix)
    {
        foreach (var proc in typeof(T).GetFields())
        {
            var name = proc.Name.Replace(prefix, string.Empty);
            if (proc.Name != name)
            {
                yield return new Tuple<string, FieldInfo>(name, proc);
            }
        }
    }

    private static Type? FindEventType(string prefix, string procName)
    {
        return BindingAsm
            .GetTypes()
            .FirstOrDefault(t => t.Name == $"{prefix}{procName}Msg");
    }

    private static Type? FindFuncArgsType(string prefix, string procName)
    {
        return BindingAsm
            .GetTypes()
            .FirstOrDefault(t => t.Name == $"{prefix}{procName}Args");
    }

    private static Type? FindFuncRetType(string prefix, string procName)
    {
        return BindingAsm
            .GetTypes()
            .FirstOrDefault(t => t.Name == $"{prefix}{procName}Ret");
    }

    private static string ToLowerCamelCase(string value)
    {
        var terms = Regex.Replace(value, @"([A-Z]+)", "_$1")
            .Trim('_')
            .Split('_')
            .Select(t => t.ToLowerInvariant())
            .Select((t, i) => i == 0 ? t : ToCaption(t));
        return string.Join(string.Empty, terms);
    }

    private static string ToUpperCamelCase(string value)
    {
        var terms = Regex.Replace(value, @"([A-Z]+)", "_$1")
            .Trim('_')
            .Split('_')
            .Select(ToCaption);
        return string.Join(string.Empty, terms);
    }

    private static string ToCaption(string value)
    {
        var caption = value
            .ToCharArray()
            .Select((ch, i) => i == 0 ? char.ToUpper(ch) : char.ToLower(ch))
            .ToArray();
        return new string(caption);
    }
}
