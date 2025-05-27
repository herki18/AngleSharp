namespace LayoutEngine.Core.Style.Internal;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using Microsoft.Extensions.Logging;
using Public;

public class InheritanceResolver : IInheritanceResolver
{
    private readonly ILogger<InheritanceResolver> _logger;

    private static readonly HashSet<string> InheritedProperties = new()
    {
        "color", "font-family", "font-size", "font-weight", "font-style",
        "line-height", "text-align", "text-indent", "text-transform",
        "white-space", "word-spacing", "letter-spacing", "visibility"
    };

    public InheritanceResolver(ILogger<InheritanceResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ApplyInheritance(ICssStyleDeclaration childDeclaration, ICssStyleDeclaration parentDeclaration)
    {
        _logger.LogDebug("Applying inheritance from parent");

        foreach (var property in InheritedProperties)
        {
            var parentValue = parentDeclaration.GetPropertyValue(property);
            if (!string.IsNullOrEmpty(parentValue))
            {
                // Only inherit if child doesn't already have this property
                var childValue = childDeclaration.GetPropertyValue(property);
                if (string.IsNullOrEmpty(childValue))
                {
                    childDeclaration.SetProperty(property, parentValue);
                }
            }
        }
    }
}