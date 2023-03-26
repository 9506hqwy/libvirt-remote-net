namespace Gen;

using Binding;
using System.CodeDom;
using Xdr;

internal class MainShell
{
    internal static void Main(string[] args)
    {
        try
        {
            var shell = new MainShell();
            shell.Work(args);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("{0}", e);
        }
    }

    internal void Work(string[] args)
    {
        this.WriteEvent();
        this.WriteClient();
    }

    internal void WriteClient()
    {
        var cls = new CodeTypeDeclaration("VirtClient")
        {
            IsPartial = true,
        };

        foreach ((var procName, var methodName, var argType, var retType) in Utility.EnumerateMethod<QemuProcedure>(Utility.QemuPrefix))
        {
            this.AddMemberToCls(procName, methodName, argType, retType, cls);
        }

        foreach ((var procName, var methodName, var argType, var retType) in Utility.EnumerateMethod<RemoteProcedure>(Utility.RemotePrefix))
        {
            this.AddMemberToCls(procName, methodName, argType, retType, cls);
        }

        var ns = new CodeNamespace("LibvirtRemote");
        ns.Types.Add(cls);

        Code.WriteFile("VirtClient.cs", ns);
    }

    internal void WriteEvent()
    {
        var ns = new CodeNamespace("Binding");

        var intf = Code.CreateEventInterface();
        ns.Types.Add(intf);

        foreach ((var procName, var eventType) in Utility.EnumerateEvent<QemuProcedure>(Utility.QemuPrefix))
        {
            var cls = Code.CreateEventImpl(procName, eventType, intf);
            ns.Types.Add(cls);
        }

        foreach ((var procName, var eventType) in Utility.EnumerateEvent<RemoteProcedure>(Utility.RemotePrefix))
        {
            var cls = Code.CreateEventImpl(procName, eventType, intf);
            ns.Types.Add(cls);
        }

        Code.WriteFile("VirtEvent.cs", ns);
    }

    internal CodeMemberMethod ImplementMethod<T>(
        T procName,
        string methodName,
        Type? argType,
        Type? retType)
    {
        var method = new CodeMemberMethod
        {
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            Name = $"{methodName}Async",
        };

        var args = Code.AddFuncArgs(method, argType, false);

        var ret = Code.AddFuncRet(procName, method, retType, false);

        this.SetMethod(procName, method, methodName, args, ret);

        return method;
    }

    internal CodeMemberMethod ImplementWrappedMethod<T>(
        T procName,
        string methodName,
        Type? argType,
        Type? retType)
    {
        var method = new CodeMemberMethod
        {
            Attributes = MemberAttributes.Public | MemberAttributes.Final,
            Name = $"{methodName}WrappedAsync",
        };

        var args = Code.AddFuncArgs(method, argType, true);

        var ret = Code.AddFuncRet(procName, method, retType, true);

        var innerMethodName = Code.IsStreamProc(procName) ?
            "CallWithStreamAsync" :
            "CallAsync";

        var rType = ret is not null ?
            new CodeTypeReference(ret) :
            null;

        this.SetWrappedMethod(method, innerMethodName, procName, args, rType);

        return method;
    }

    internal void SetMethod<T>(
        T procName,
        CodeMemberMethod method,
        string methodName,
        FuncArgs? args,
        Type? retType)
    {
        var req = args is null ? null : Code.AddConstructStatement(method, args);

        var rType = retType is null ? null : new CodeTypeReference(retType);

        var innerMethod = new CodeMethodReferenceExpression(
            new CodeThisReferenceExpression(),
            $"{methodName}WrappedAsync");

        var parameters = new List<CodeExpression>();

        if (req is not null)
        {
            parameters.Add(new CodeVariableReferenceExpression(req.Name));
        }

        parameters.Add(new CodeVariableReferenceExpression(Code.CancelToken.Name));

        var res = Code.AddCallAsyncStatement(method, procName, rType, innerMethod, parameters.ToArray());

        if (res is CodeVariableDeclarationStatement variable)
        {
            CodeExpression val = new CodeVariableReferenceExpression(variable.Name);

            if (!Code.IsStreamProc(procName))
            {
                var variables = Code.AddDeconstructStatement(method, (CodeVariableReferenceExpression)val, retType!);
                val = Code.CreateFuncRetValue(variable, retType!, variables);
            }
            else if (retType is null)
            {
                val = Code.CreateTupleProperty(variable.Name, 1);
            }
            else
            {
                var retTypes = retType!.GetProperties()
                    .Select(p => p.PropertyType)
                    .Select(t => new CodeTypeReference(t))
                    .ToList();
                retTypes.Insert(0, Code.VirtStreamTypeRef);

                var variables = Code.AddDeconstructTupleStatement(method, (CodeVariableReferenceExpression)val, retType!);

                val = Code.CreateFuncRetValue(retTypes.ToArray(), variables);
            }

            method.Statements.Add(new CodeMethodReturnStatement(val));
        }
    }

    internal void SetWrappedMethod<T>(
        CodeMemberMethod method,
        string innerMethod,
        T procName,
        FuncArgs? argType,
        CodeTypeReference? retType)
    {
        var procFlag = new CodeFieldReferenceExpression(
            new CodeTypeReferenceExpression(typeof(T)),
            procName!.ToString()!);

        var callAsync = new CodeMethodReferenceExpression(
            new CodeThisReferenceExpression(),
            innerMethod);
        if (retType is null)
        {
            callAsync.TypeArguments.Add(typeof(XdrVoid));
        }
        else
        {
            callAsync.TypeArguments.Add(retType);
        }

        callAsync.TypeArguments.Add(typeof(T));

        var parameters = new List<CodeExpression>();

        parameters.Add(procFlag);

        if (argType is not null)
        {
            parameters.Add(new CodeVariableReferenceExpression("arg"));
        }
        else
        {
            parameters.Add(new CodePrimitiveExpression());
        }

        parameters.Add(new CodeVariableReferenceExpression(Code.CancelToken.Name));

        var res = Code.AddCallAsyncStatement(method, procName, retType, callAsync, parameters.ToArray());

        if (res is CodeVariableDeclarationStatement variable)
        {
            method.Statements.Add(
                new CodeMethodReturnStatement(new CodeVariableReferenceExpression(variable.Name)));
        }
    }

    private void AddMemberToCls<T>(T procName, string methodName, Type? argType, Type? retType, CodeTypeDeclaration cls)
    {
        var method1 = this.ImplementMethod(procName, methodName, argType, retType);
        cls.Members.Add(method1);

        var method2 = this.ImplementWrappedMethod(procName, methodName, argType, retType);
        cls.Members.Add(method2);
    }
}
