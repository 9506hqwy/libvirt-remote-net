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

        foreach ((var procName, var methodName, var argType, var retType) in Utility.EnumerateMethod())
        {
            var method1 = this.ImplementMethod(procName, methodName, argType, retType);
            cls.Members.Add(method1);

            var method2 = this.ImplementWrappedMethod(procName, methodName, argType, retType);
            cls.Members.Add(method2);
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

        foreach ((RemoteProcedure procName, var eventType) in Utility.EnumerateEvent())
        {
            var cls = Code.CreateEventImpl(procName, eventType, intf);
            ns.Types.Add(cls);
        }

        Code.WriteFile("VirtEvent.cs", ns);
    }

    internal CodeMemberMethod ImplementMethod(
        RemoteProcedure procName,
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

    internal CodeMemberMethod ImplementWrappedMethod(
        RemoteProcedure procName,
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

        var innerMethodName = Code.StreamProcs.Contains(procName) ?
            "CallWithStreamAsync" :
            "CallAsync";

        var rType = Code.StreamProcs.Contains(procName) ?
            Code.VirtStreamTypeRef :
            ret is not null ?
            new CodeTypeReference(ret) :
            null;

        this.SetWrappedMethod(method, innerMethodName, procName, args, rType);

        return method;
    }

    internal void SetMethod(
        RemoteProcedure procName,
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

            if (!Code.StreamProcs.Contains(procName))
            {
                var variables = Code.AddDeconstructStatement(method, (CodeVariableReferenceExpression)val, retType!);
                val = Code.CreateFuncRetValue(variable, retType!, variables);
            }

            method.Statements.Add(new CodeMethodReturnStatement(val));
        }
    }

    internal void SetWrappedMethod(
        CodeMemberMethod method,
        string innerMethod,
        RemoteProcedure procName,
        FuncArgs? argType,
        CodeTypeReference? retType)
    {
        var procFlag = new CodeFieldReferenceExpression(
            new CodeTypeReferenceExpression(typeof(RemoteProcedure)),
            procName.ToString());

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
}
