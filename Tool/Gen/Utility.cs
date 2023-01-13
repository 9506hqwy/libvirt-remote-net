namespace Gen;

using Binding;
using System.Reflection;
using System.Text.RegularExpressions;

internal static class Utility
{
    private const string ProcPrefix = "RemoteProc";

    private static readonly Assembly BindingAsm = typeof(RemoteProcedure).Assembly;

    internal static IEnumerable<Tuple<RemoteProcedure, Type>> EnumerateEvent()
    {
        foreach ((var procName, var proc) in Utility.EnumerateProc())
        {
            var eventType = Utility.FindEventType(procName);
            if (eventType is null)
            {
                continue;
            }

            if (eventType.GetProperty(Code.EventCallbackId) is null)
            {
                continue;
            }

            yield return new Tuple<RemoteProcedure, Type>((RemoteProcedure)proc.GetValue(null)!, eventType);
        }
    }

    internal static IEnumerable<Tuple<RemoteProcedure, string, Type?, Type?>> EnumerateMethod()
    {
        foreach ((var procName, var proc) in Utility.EnumerateProc())
        {
            var eventType = Utility.FindEventType(procName);
            if (eventType is not null)
            {
                continue;
            }

            var argType = Utility.FindFuncArgsType(procName);
            var retType = Utility.FindFuncRetType(procName);
            yield return new Tuple<RemoteProcedure, string, Type?, Type?>(
                (RemoteProcedure)proc.GetValue(null)!,
                procName,
                argType,
                retType);
        }
    }

    internal static IEnumerable<Tuple<string, FieldInfo>> EnumerateProc()
    {
        foreach (var proc in typeof(RemoteProcedure).GetFields())
        {
            var name = proc.Name.Replace(Utility.ProcPrefix, string.Empty);
            if (proc.Name != name)
            {
                yield return new Tuple<string, FieldInfo>(name, proc);
            }
        }
    }

    internal static Type? FindEventType(string procName)
    {
        return Utility.BindingAsm
            .GetTypes()
            .FirstOrDefault(t => t.Name == $"Remote{procName}Msg");
    }

    internal static Type? FindFuncArgsType(string procName)
    {
        return Utility.BindingAsm
            .GetTypes()
            .FirstOrDefault(t => t.Name == $"Remote{procName}Args");
    }

    internal static Type? FindFuncRetType(string procName)
    {
        return Utility.BindingAsm
            .GetTypes()
            .FirstOrDefault(t => t.Name == $"Remote{procName}Ret");
    }

    internal static string ToArgName(string value)
    {
        return Utility.ToLowerCamelCase(value);
    }

    internal static string ToPropertyName(string value)
    {
        return Utility.ToUpperCamelCase(value);
    }

    private static string ToLowerCamelCase(string value)
    {
        var terms = Regex.Replace(value, @"([A-Z]+)", "_$1")
            .Trim('_')
            .Split('_')
            .Select(t => t.ToLowerInvariant())
            .Select((t, i) => i == 0 ? t : Utility.ToCaption(t));
        return string.Join(string.Empty, terms);
    }

    private static string ToUpperCamelCase(string value)
    {
        var terms = Regex.Replace(value, @"([A-Z]+)", "_$1")
            .Trim('_')
            .Split('_')
            .Select(Utility.ToCaption);
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
