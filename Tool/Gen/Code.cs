namespace Gen;

using Binding;
using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;

internal static class Code
{
    internal const string EventCallbackId = "CallbackId";

    internal const string EventInterfaceName = "IVirtEvent";

    internal static readonly CodeParameterDeclarationExpression CancelToken =
        new CodeParameterDeclarationExpression(
            new CodeTypeReference(typeof(CancellationToken)),
            "cancellationToken");

    internal static readonly CodeTypeReference VirtStreamTypeRef =
        new CodeTypeReference("VirtNetStream");

    private static readonly RemoteProcedure[] StreamProcs = new[]
    {
        RemoteProcedure.RemoteProcDomainMigratePrepareTunnel,
        RemoteProcedure.RemoteProcDomainOpenConsole,
        RemoteProcedure.RemoteProcStorageVolUpload,
        RemoteProcedure.RemoteProcStorageVolDownload,
        RemoteProcedure.RemoteProcDomainScreenshot,
        RemoteProcedure.RemoteProcDomainMigratePrepareTunnel3,
        RemoteProcedure.RemoteProcDomainOpenChannel,
    };

    internal static CodeStatement AddCallAsyncStatement<T>(
        CodeMemberMethod method,
        T procName,
        CodeTypeReference? type,
        CodeMethodReferenceExpression innerMethod,
        CodeExpression[] parameters)
    {
        const string response = "innerRes";
        const string task = "innerTask";

        var invoke = new CodeMethodInvokeExpression(innerMethod, parameters);

        var ret = new CodeVariableDeclarationStatement("var", task)
        {
            InitExpression = invoke,
        };

        method.Statements.Add(ret);

        CodeStatement res =
            Code.IsStreamProc(procName) ?
            new CodeVariableDeclarationStatement("var", response)
            {
                InitExpression = new CodeVariableReferenceExpression($"await {ret.Name}"),
            }
            :
            type is not null ?
            new CodeVariableDeclarationStatement("var", response)
            {
                InitExpression = new CodeVariableReferenceExpression($"await {ret.Name}"),
            }
            :
            new CodeExpressionStatement(new CodeVariableReferenceExpression($"await {task}"));

        method.Statements.Add(res);

        return res;
    }

    internal static CodeVariableDeclarationStatement AddConstructStatement(
        CodeMemberMethod method,
        FuncArgs args)
    {
        const string request = "innerReq";

        var ctor = new CodeObjectCreateExpression(args.Type);
        var req = new CodeVariableDeclarationStatement("var", request)
        {
            InitExpression = ctor,
        };
        method.Statements.Add(req);

        foreach (var arg in args.Exprs)
        {
            var assign = new CodeAssignStatement(
                new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression(request),
                    Utility.ToPropertyName(arg.Name)),
                new CodeVariableReferenceExpression(arg.Name));
            method.Statements.Add(assign);
        }

        return req;
    }

    internal static CodeVariableDeclarationStatement[] AddDeconstructStatement(
        CodeMemberMethod method,
        CodeVariableReferenceExpression response,
        Type type)
    {
        var properties = type.GetProperties();
        if (properties.Length > 7)
        {
            return new CodeVariableDeclarationStatement[0];
        }

        var variables = new List<CodeVariableDeclarationStatement>();
        foreach (var property in properties)
        {
            var assign = new CodeVariableDeclarationStatement(
                "var",
                $"inner{Utility.ToPropertyName(property.Name)}")
            {
                InitExpression = new CodePropertyReferenceExpression(
                    response,
                    Utility.ToPropertyName(property.Name)),
            };
            variables.Add(assign);

            method.Statements.Add(assign);
        }

        return variables.ToArray();
    }

    internal static CodeVariableDeclarationStatement[] AddDeconstructTupleStatement(
        CodeMemberMethod method,
        CodeVariableReferenceExpression response,
        Type type)
    {
        const string response2 = "innerRes2";

        var properties = type.GetProperties();
        if (properties.Length > 7)
        {
            return new CodeVariableDeclarationStatement[0];
        }

        var variables = new List<CodeVariableDeclarationStatement>();
        {
            var assign = new CodeVariableDeclarationStatement(
                "var",
                $"innerStream")
            {
                InitExpression = Code.CreateTupleProperty(response.VariableName, 1),
            };
            variables.Add(assign);

            method.Statements.Add(assign);
        }

        {
            var assign = new CodeVariableDeclarationStatement(
                "var",
                response2)
            {
                InitExpression = Code.CreateTupleProperty(response.VariableName, 2),
            };

            method.Statements.Add(assign);
        }

        foreach (var property in properties)
        {
            var assign = new CodeVariableDeclarationStatement(
                "var",
                $"inner{Utility.ToPropertyName(property.Name)}")
            {
                InitExpression = new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression(response2),
                    Utility.ToPropertyName(property.Name)),
            };
            variables.Add(assign);

            method.Statements.Add(assign);
        }

        return variables.ToArray();
    }

    internal static FuncArgs? AddFuncArgs(CodeMemberMethod method, Type? type, bool isWrapped)
    {
        var args = type is null ? null : new FuncArgs(type, isWrapped);
        if (args is not null)
        {
            method.Parameters.AddRange(args.Exprs);
        }

        method.Parameters.Add(Code.CancelToken);

        return args;
    }

    internal static Type? AddFuncRet<T>(T procName, CodeMemberMethod method, Type? type, bool isWrapped)
    {
        method.ReturnType = new CodeTypeReference("async Task");

        if (Code.IsStreamProc(procName))
        {
            CodeTypeReference? tuple;
            if (type is null && !isWrapped)
            {
                tuple = Code.VirtStreamTypeRef;
            }
            else if (type is null && isWrapped)
            {
                tuple = new CodeTypeReference("Tuple");
                tuple.TypeArguments.Add(Code.VirtStreamTypeRef);
                tuple.TypeArguments.Add(typeof(Xdr.XdrVoid));
            }
            else if (isWrapped)
            {
                tuple = new CodeTypeReference("Tuple");
                tuple.TypeArguments.Add(Code.VirtStreamTypeRef);
                tuple.TypeArguments.Add(new CodeTypeReference(type!));
            }
            else
            {
                tuple = new CodeTypeReference("Tuple");
                tuple.TypeArguments.Add(Code.VirtStreamTypeRef);
                tuple.TypeArguments.Add(Code.CreateFuncRetType(type!, isWrapped));
            }

            method.ReturnType.TypeArguments.Add(tuple!);
            return type;
        }
        else if (type is not null)
        {
            method.ReturnType.TypeArguments.Add(Code.CreateFuncRetType(type, isWrapped));
            return type;
        }
        else
        {
            return null;
        }
    }

    internal static CodeTypeDeclaration CreateEventImpl<T>(
        T procName,
        Type eventType,
        CodeTypeDeclaration intf)
    {
        var progFlag = procName switch
        {
            LxcProcedure _ => new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(typeof(Constants).Name),
                nameof(Constants.LxcProgram)),
            QemuProcedure _ => new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(typeof(Constants).Name),
                nameof(Constants.QemuProgram)),
            RemoteProcedure _ => new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(typeof(Constants).Name),
                nameof(Constants.RemoteProgram)),
            _ => throw new InvalidProgramException(),
        };

        var procFlag = new CodeFieldReferenceExpression(
            new CodeTypeReferenceExpression(typeof(T).Name),
            procName!.ToString()!);

        var cls = new CodeTypeDeclaration(eventType.Name)
        {
            IsPartial = true,
        };

        cls.CustomAttributes.Add(new CodeAttributeDeclaration(
            "VirtEventAttribute",
            new CodeAttributeArgument(progFlag),
            new CodeAttributeArgument(procFlag)));

        cls.BaseTypes.Add(new CodeTypeReference(intf.Name));

        foreach (var intfMethod in intf.Members.OfType<CodeMemberMethod>())
        {
            var method = new CodeMemberMethod()
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = intfMethod.Name,
                ReturnType = intfMethod.ReturnType,
            };

            var field = new CodeFieldReferenceExpression(
                new CodeThisReferenceExpression(),
                Utility.ToArgName(intfMethod.Name.Replace("Get", string.Empty)));
            var impl = new CodeMethodReturnStatement(field);
            method.Statements.Add(impl);

            cls.Members.Add(method);
        }

        return cls;
    }

    internal static CodeTypeDeclaration CreateEventInterface()
    {
        var baseMethod = new CodeMemberMethod()
        {
            Name = $"Get{Code.EventCallbackId}",
            ReturnType = new CodeTypeReference(typeof(int)),
        };

        var intf = new CodeTypeDeclaration(Code.EventInterfaceName)
        {
            IsInterface = true,
        };
        intf.Members.Add(baseMethod);

        return intf;
    }

    internal static CodeParameterDeclarationExpression[] CreateFuncArgs(Type type, bool isWrapped)
    {
        if (isWrapped)
        {
            return new[]
            {
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(type),
                    "arg"),
            };
        }

        return type.GetProperties()
            .Select(p => new CodeParameterDeclarationExpression(
                new CodeTypeReference(p.PropertyType),
                Utility.ToArgName(p.Name)))
            .ToArray();
    }

    internal static CodeTypeReference CreateFuncRetType(Type type, bool isWrapped)
    {
        if (isWrapped)
        {
            return new CodeTypeReference(type);
        }

        var props = type.GetProperties().Select(p => p.PropertyType).ToArray();
        if (props.Length == 1)
        {
            // 1個はそのまま
            return new CodeTypeReference(props[0]);
        }
        else if (props.Length < 8)
        {
            // Tuple は 7 個まで。
            return Code.CreateTupleRef(props);
        }
        else
        {
            // 8 個以上は解体しない。
            return new CodeTypeReference(type);
        }
    }

    internal static CodeExpression CreateFuncRetValue(
        CodeVariableDeclarationStatement variable,
        Type type,
        CodeVariableDeclarationStatement[] variables)
    {
        if (variables.Length == 0)
        {
            // 8 個以上は解体しない。
        }
        else if (variables.Length == 1)
        {
            // 1個はそのまま
            return new CodeVariableReferenceExpression(variables[0].Name);
        }
        else if (variables.Length < 8)
        {
            // Tuple は 7 個まで。
            var retTypes = type!.GetProperties().Select(p => p.PropertyType).ToArray();
            return Code.CreateFuncRetValue(retTypes, variables);
        }

        return new CodeVariableReferenceExpression(variable.Name);
    }

    internal static CodeExpression CreateFuncRetValue(
        Type[] types,
        CodeVariableDeclarationStatement[] variables)
    {
        return Code.CreateFuncRetValue(
            types.Select(t => new CodeTypeReference(t)).ToArray(),
            variables);
    }

    internal static CodeExpression CreateFuncRetValue(
        CodeTypeReference[] types,
        CodeVariableDeclarationStatement[] variables)
    {
        var tuple = Code.CreateTupleRef(types);

        var ctor = new CodeObjectCreateExpression(tuple);
        ctor.Parameters.AddRange(variables
            .Select(v => new CodeVariableReferenceExpression(v.Name))
            .ToArray());

        return ctor;
    }

    internal static CodeExpression CreateTupleProperty(
        string variableName,
        int index)
    {
        return new CodePropertyReferenceExpression(
            new CodeVariableReferenceExpression(variableName),
            $"Item{index}");
    }

    internal static CodeTypeReference CreateTupleRef(Type[] types)
    {
        return Code.CreateTupleRef(types.Select(t => new CodeTypeReference(t)).ToArray());
    }

    internal static CodeTypeReference CreateTupleRef(CodeTypeReference[] types)
    {
        var tuple = new CodeTypeReference("Tuple");
        tuple.TypeArguments.AddRange(types);
        return tuple;
    }

    internal static bool IsStreamProc<T>(T procName)
    {
        return procName switch
        {
            RemoteProcedure remote => Code.StreamProcs.Contains(remote),
            _ => false,
        };
    }

    internal static void WriteFile(string path, CodeNamespace ns)
    {
        var compileUnit = new CodeCompileUnit();
        compileUnit.Namespaces.Add(ns);

        var provider = new CSharpCodeProvider();

        using var stream = File.OpenWrite(path);
        using var writer = new StreamWriter(stream, leaveOpen: true);
        provider.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions());
        writer.Flush();
    }
}
