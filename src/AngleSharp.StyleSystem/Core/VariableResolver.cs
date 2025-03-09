namespace AngleSharp.StyleSystem.Core;

using Css.Dom;
using Css.Values;
using Dom;
using Html.Dom;
using Interfaces;

public class VariableResolver : IVariableResolver
{
    private readonly IBrowsingContext _context;

    public VariableResolver(IBrowsingContext context)
    {
        _context = context;
    }

    public ICssValue? ResolveVariable(string variableName, IElement element, ICssValue? defaultValue = null)
    {
        throw new System.NotImplementedException();
    }

    public ICssValue? ResolveVarFunction(CssVarValue varValue, IElement element)
    {
        throw new System.NotImplementedException();
    }

    public ICssValue ResolveVariablesInValue(ICssValue value, IElement element, string propertyName)
    {
        throw new System.NotImplementedException();
    }

    public void RegisterVariable(IElement element, string variableName, ICssValue value)
    {
        throw new System.NotImplementedException();
    }

    public void ExtractVariablesFromStyle(IElement element, ICssStyleDeclaration style)
    {
        throw new System.NotImplementedException();
    }
}