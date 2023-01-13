namespace Gen;

using System.CodeDom;

internal class FuncArgs
{
    internal FuncArgs(Type type, bool isWrapped)
    {
        this.Type = type;
        this.IsWrapped = isWrapped;
    }

    internal CodeParameterDeclarationExpression[] Exprs =>
        Code.CreateFuncArgs(this.Type, this.IsWrapped);

    internal bool IsWrapped { get; }

    internal Type Type { get; }
}
