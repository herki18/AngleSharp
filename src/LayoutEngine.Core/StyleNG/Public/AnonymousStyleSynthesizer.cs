namespace LayoutEngine.Core.LayoutStyle.Internal;

using System;
using System.Collections.Generic;
using LayoutEngine.Core.LayoutNG.Public;
using LayoutEngine.Core.LayoutStyle.Public;
using AngleSharp.Css.Dom;
using Microsoft.Extensions.Logging;
using Style.Public;

/// <summary>
/// Synthesizes styles for anonymous layout objects according to CSS specifications.
/// </summary>
public class AnonymousStyleSynthesizer : IAnonymousStyleSynthesizer
{
    private readonly ICssStyleDeclarationFactory _declarationFactory;
    private readonly ILogger<AnonymousStyleSynthesizer> _logger;

    // Properties that are always inherited
    private static readonly HashSet<string> AlwaysInheritedProperties = new()
    {
        "color", "font-family", "font-size", "font-weight", "font-style",
        "line-height", "text-align", "text-indent", "text-transform",
        "white-space", "word-spacing", "letter-spacing", "visibility",
        "direction", "unicode-bidi", "quotes", "cursor"
    };

    // Properties that should NOT be inherited by anonymous boxes
    private static readonly HashSet<string> NonInheritedForAnonymous = new()
    {
        "display", "position", "float", "clear", "width", "height",
        "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
        "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
        "border", "border-width", "border-style", "border-color",
        "top", "right", "bottom", "left", "z-index"
    };

    public AnonymousStyleSynthesizer(
        ICssStyleDeclarationFactory declarationFactory,
        ILogger<AnonymousStyleSynthesizer> logger)
    {
        _declarationFactory = declarationFactory ?? throw new ArgumentNullException(nameof(declarationFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ILayoutComputedStyle SynthesizeAnonymousBlockStyle(ILayoutObject anonymousBlock, ILayoutComputedStyle parentStyle)
    {
        _logger.LogDebug("Synthesizing anonymous block style");

        var declaration = CreateInheritedDeclaration(parentStyle, LayoutObjectType.AnonymousBlock);

        // Anonymous blocks always have display: block
        declaration.SetProperty("display", "block");

        // Anonymous blocks have no margins, padding, or borders
        SetZeroBoxModel(declaration);

        return new LayoutComputedStyle(anonymousBlock, declaration, true);
    }

    public ILayoutComputedStyle SynthesizeAnonymousInlineStyle(ILayoutObject anonymousInline, ILayoutComputedStyle parentStyle)
    {
        _logger.LogDebug("Synthesizing anonymous inline style");

        var declaration = CreateInheritedDeclaration(parentStyle, LayoutObjectType.AnonymousInline);

        // Anonymous inlines always have display: inline
        declaration.SetProperty("display", "inline");

        // Anonymous inlines have no margins, padding, or borders
        SetZeroBoxModel(declaration);

        return new LayoutComputedStyle(anonymousInline, declaration, true);
    }

    public ILayoutComputedStyle SynthesizeAnonymousTableWrapperStyle(ILayoutObject anonymousTable, ILayoutComputedStyle parentStyle)
    {
        _logger.LogDebug("Synthesizing anonymous table wrapper style");

        var declaration = CreateInheritedDeclaration(parentStyle, LayoutObjectType.Table);

        // Anonymous table wrappers have display: table
        declaration.SetProperty("display", "table");

        // Inherit border-spacing if present
        var borderSpacing = parentStyle.GetPropertyValue("border-spacing");
        if (!string.IsNullOrEmpty(borderSpacing))
        {
            declaration.SetProperty("border-spacing", borderSpacing);
        }

        SetZeroBoxModel(declaration);

        return new LayoutComputedStyle(anonymousTable, declaration, true);
    }

    public ILayoutComputedStyle SynthesizeAnonymousFlexItemStyle(ILayoutObject anonymousFlexItem, ILayoutComputedStyle parentStyle)
    {
        _logger.LogDebug("Synthesizing anonymous flex item style");

        var declaration = CreateInheritedDeclaration(parentStyle, LayoutObjectType.Block);

        // Anonymous flex items are block-level
        declaration.SetProperty("display", "block");

        // Anonymous flex items participate in flex layout
        declaration.SetProperty("flex", "0 1 auto");

        SetZeroBoxModel(declaration);

        return new LayoutComputedStyle(anonymousFlexItem, declaration, true);
    }

    public ILayoutComputedStyle SynthesizeStyle(ILayoutObject anonymousObject, ILayoutComputedStyle parentStyle)
    {
        return anonymousObject.Type switch
        {
            LayoutObjectType.AnonymousBlock => SynthesizeAnonymousBlockStyle(anonymousObject, parentStyle),
            LayoutObjectType.AnonymousInline => SynthesizeAnonymousInlineStyle(anonymousObject, parentStyle),
            LayoutObjectType.Table when anonymousObject.IsAnonymous => SynthesizeAnonymousTableWrapperStyle(anonymousObject, parentStyle),
            _ => SynthesizeDefaultAnonymousStyle(anonymousObject, parentStyle)
        };
    }

    public ICssStyleDeclaration CreateInheritedDeclaration(ILayoutComputedStyle parentStyle, LayoutObjectType anonymousType)
    {
        var declaration = _declarationFactory.Create();

        // Copy all inheritable properties from parent
        foreach (var propertyName in AlwaysInheritedProperties)
        {
            var value = parentStyle.GetPropertyValue(propertyName);
            if (!string.IsNullOrEmpty(value))
            {
                declaration.SetProperty(propertyName, value);
            }
        }

        // Special handling for certain anonymous types
        switch (anonymousType)
        {
            case LayoutObjectType.AnonymousBlock:
                // Anonymous blocks inside inline elements may need special text properties
                CopyTextProperties(parentStyle, declaration);
                break;

            case LayoutObjectType.AnonymousInline:
                // Anonymous inlines inherit more properties
                CopyInlineProperties(parentStyle, declaration);
                break;
        }

        return declaration;
    }

    private ILayoutComputedStyle SynthesizeDefaultAnonymousStyle(ILayoutObject anonymousObject, ILayoutComputedStyle parentStyle)
    {
        _logger.LogDebug("Synthesizing default anonymous style for type: {Type}", anonymousObject.Type);

        var declaration = CreateInheritedDeclaration(parentStyle, anonymousObject.Type);

        // Default to block display for unknown anonymous types
        declaration.SetProperty("display", "block");
        SetZeroBoxModel(declaration);

        return new LayoutComputedStyle(anonymousObject, declaration, true);
    }

    private void SetZeroBoxModel(ICssStyleDeclaration declaration)
    {
        // Anonymous boxes have no margins
        declaration.SetProperty("margin", "0");
        declaration.SetProperty("margin-top", "0");
        declaration.SetProperty("margin-right", "0");
        declaration.SetProperty("margin-bottom", "0");
        declaration.SetProperty("margin-left", "0");

        // Anonymous boxes have no padding
        declaration.SetProperty("padding", "0");
        declaration.SetProperty("padding-top", "0");
        declaration.SetProperty("padding-right", "0");
        declaration.SetProperty("padding-bottom", "0");
        declaration.SetProperty("padding-left", "0");

        // Anonymous boxes have no borders
        declaration.SetProperty("border-width", "0");
        declaration.SetProperty("border-style", "none");
    }

    private void CopyTextProperties(ILayoutComputedStyle parentStyle, ICssStyleDeclaration declaration)
    {
        // Additional text properties that might be needed
        var textProperties = new[]
        {
            "text-decoration", "text-decoration-line", "text-decoration-style",
            "text-decoration-color", "text-shadow", "font-variant"
        };

        foreach (var prop in textProperties)
        {
            var value = parentStyle.GetPropertyValue(prop);
            if (!string.IsNullOrEmpty(value))
            {
                declaration.SetProperty(prop, value);
            }
        }
    }

    private void CopyInlineProperties(ILayoutComputedStyle parentStyle, ICssStyleDeclaration declaration)
    {
        // Anonymous inlines may need vertical-align and line-height
        var inlineProperties = new[] { "vertical-align", "line-height" };

        foreach (var prop in inlineProperties)
        {
            var value = parentStyle.GetPropertyValue(prop);
            if (!string.IsNullOrEmpty(value))
            {
                declaration.SetProperty(prop, value);
            }
        }
    }
}