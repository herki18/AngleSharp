namespace AngleSharp.StyleSystem.Core;

using System;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;
using Css;

/// <summary>
/// Builds computed style objects from CSS declarations.
/// </summary>
public class ComputedStyleBuilder
{
    private readonly IBrowsingContext _context;
    private readonly StyleEngine _engine;
    private readonly IVariableResolver _variableResolver;
    private readonly IValueCalculator _valueCalculator;
    private readonly IStylePropertyMapper _stylePropertyMapper;
    private readonly IPropertyTreeManager _propertyTreeManager;
    private readonly IRenderDevice _renderDevice;

    public ComputedStyleBuilder(
        IBrowsingContext context,
        StyleEngine engine,
        IVariableResolver variableResolver,
        IValueCalculator valueCalculator,
        IStylePropertyMapper stylePropertyMapper,
        IPropertyTreeManager propertyTreeManager,
        IRenderDevice renderDevice)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _variableResolver = variableResolver ?? throw new ArgumentNullException(nameof(variableResolver));
        _valueCalculator = valueCalculator ?? throw new ArgumentNullException(nameof(valueCalculator));
        _stylePropertyMapper = stylePropertyMapper ?? throw new ArgumentNullException(nameof(stylePropertyMapper));
        _propertyTreeManager = propertyTreeManager ?? throw new ArgumentNullException(nameof(propertyTreeManager));
        _renderDevice = renderDevice ?? throw new ArgumentNullException(nameof(renderDevice));
    }

    /// <summary>
    /// Builds a computed style from a CSS style declaration.
    /// </summary>
    public IComputedStyle? BuildComputedStyle(ICssStyleDeclaration declaration, IElement element, IComputedStyle? parentStyle)
    {
        // // First register any CSS custom properties from this declaration
        // _variableResolver.ExtractVariablesFromStyle(element, declaration);
        //
        // // Get or create property tree node
        // var parentNode = parentStyle is ComputedStyle parentComputed ?
        //     parentComputed.PropertyTreeNode : null;
        // var node = _propertyTreeManager.GetOrCreateNode(element, parentNode);
        //
        // // Get the writing mode early because we might need it for logical property mapping
        // var writingMode = GetWritingMode(declaration, element, parentStyle);
        //
        // // Process each property
        // foreach (var property in declaration)
        // {
        //     // Skip properties with no value
        //     if (property.RawValue == null)
        //         continue;
        //
        //     var propertyName = property.Name;
        //     var propertyValue = property.RawValue;
        //
        //     // 1. Resolve any variables first
        //     var resolvedValue = _variableResolver.ResolveVariablesInValue(propertyValue, element, propertyName);
        //
        //     // 2. Then compute the value
        //     var computedValue = _valueCalculator.Compute(resolvedValue, element, propertyName) ?? throw new ArgumentNullException("_valueCalculator.Compute(resolvedValue, element, propertyName)");
        //
        //     // 3. Handle logical property mapping if needed
        //     if (_stylePropertyMapper.IsLogicalProperty(propertyName))
        //     {
        //         var physicalProps = _stylePropertyMapper.MapLogicalToPhysical(
        //             propertyName, computedValue, writingMode);
        //
        //         foreach (var physicalProp in physicalProps)
        //         {
        //             node.SetProperty(physicalProp.Key, physicalProp.Value);
        //         }
        //     }
        //     else
        //     {
        //         // Set the computed value in the property tree
        //         node.SetProperty(propertyName, computedValue);
        //     }
        // }
        //
        // // Optimize the property tree
        // _propertyTreeManager.OptimizeTree(node);



        // Create a computed style via the factory
        return (_engine.StyleFactory as ComputedStyleFactory)?.CreateComputedStyle(element, parentStyle, declaration);
    }

    /// <summary>
    /// Determines the writing mode for an element based on its style and parent style.
    /// </summary>
    private WritingMode GetWritingMode(ICssStyleDeclaration declaration, IElement element, IComputedStyle? parentStyle)
    {
        // Extract direction and writing-mode properties
        var directionValue = declaration.GetPropertyValue("direction");
        var writingModeValue = declaration.GetPropertyValue("writing-mode");

        // Default to parent's writing mode if available
        if (parentStyle != null && string.IsNullOrEmpty(directionValue) && string.IsNullOrEmpty(writingModeValue))
        {
            return parentStyle.WritingMode;
        }

        // Otherwise, calculate based on the element's properties
        var direction = DirectionMode.Ltr;
        var mode = WritingModeType.HorizontalTopToBottom;

        if (directionValue == "rtl")
        {
            direction = DirectionMode.Rtl;
        }

        if (!string.IsNullOrEmpty(writingModeValue))
        {
            mode = writingModeValue switch
            {
                "vertical-rl" => WritingModeType.VerticalRightToLeft,
                "vertical-lr" => WritingModeType.VerticalLeftToRight,
                "sideways-rl" => WritingModeType.SidewaysRightToLeft,
                "sideways-lr" => WritingModeType.SidewaysLeftToRight,
                _ => WritingModeType.HorizontalTopToBottom
            };
        }

        return new WritingMode(direction, mode);
    }
}