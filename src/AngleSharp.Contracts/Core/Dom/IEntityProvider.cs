namespace AngleSharp.Dom;

using System;
using Common;

public interface IEntityProvider
{
    String? GetSymbol(String name);
}

public interface IEntityProviderExtended
{
    String? GetSymbol(IStringOrMemory name);
}