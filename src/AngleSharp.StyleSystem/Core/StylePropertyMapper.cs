namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
using Css.Dom;
using Interfaces;

public class StylePropertyMapper : IStylePropertyMapper
{
    public IDictionary<String, ICssValue> MapLogicalToPhysical(string logicalProperty, ICssValue value, WritingMode writingMode)
    {
        throw new NotImplementedException();
    }

    public ICssValue? MapPhysicalToLogical(IDictionary<String, ICssValue> physicalProperties, string logicalProperty, WritingMode writingMode)
    {
        throw new NotImplementedException();
    }

    public Boolean IsLogicalProperty(string propertyName)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<String> GetPhysicalProperties(string logicalProperty)
    {
        throw new NotImplementedException();
    }
}