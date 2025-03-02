namespace AngleSharp.Attributes;

using System;

/// <summary>
///     This attribute is used to determine the hosting interface.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    AllowMultiple = true, Inherited = false)]
public sealed class DomExposedAttribute : Attribute
{
    public DomExposedAttribute(String target)
    {
        Target = target;
    }

    public String Target { get; }
}