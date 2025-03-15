namespace AngleSharp.StyleSystem.Computation;

using System;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;

/// <summary>
/// Builds computed style objects from CSS declarations.
/// </summary>
public class ComputedStyleBuilder : IComputedStyleBuilder
{
    private readonly IBrowsingContext _context;
    private readonly IVariableResolver _variableResolver;
    private readonly IValueCalculator _valueCalculator;
    private readonly IStylePropertyMapper _stylePropertyMapper;
    private readonly IPropertyTreeManager _propertyTreeManager;
    private readonly IRenderDevice _renderDevice;
    private readonly IComputedStyleFactory _styleFactory;

    /// <summary>
    /// Creates a new computed style builder.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <param name="engine">The style engine.</param>
    /// <param name="variableResolver">The variable resolver.</param>
    /// <param name="valueCalculator">The value calculator.</param>
    /// <param name="stylePropertyMapper">The style property mapper.</param>
    /// <param name="propertyTreeManager">The property tree manager.</param>
    /// <param name="renderDevice">The render device.</param>
    public ComputedStyleBuilder(
        IBrowsingContext context,
        IStyleEngine  engine,
        IVariableResolver variableResolver,
        IValueCalculator valueCalculator,
        IStylePropertyMapper stylePropertyMapper,
        IPropertyTreeManager propertyTreeManager,
        IRenderDevice renderDevice,
        IComputedStyleFactory styleFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _variableResolver = variableResolver ?? throw new ArgumentNullException(nameof(variableResolver));
        _valueCalculator = valueCalculator ?? throw new ArgumentNullException(nameof(valueCalculator));
        _stylePropertyMapper = stylePropertyMapper ?? throw new ArgumentNullException(nameof(stylePropertyMapper));
        _propertyTreeManager = propertyTreeManager ?? throw new ArgumentNullException(nameof(propertyTreeManager));
        _renderDevice = renderDevice ?? throw new ArgumentNullException(nameof(renderDevice));
        _styleFactory = styleFactory ?? throw new ArgumentNullException(nameof(styleFactory));
    }

    /// <summary>
    /// Builds a computed style from a CSS style declaration.
    /// </summary>
    /// <param name="declaration">The CSS style declaration to process.</param>
    /// <param name="element">The element being styled.</param>
    /// <param name="parentStyle">The parent element's computed style.</param>
    /// <returns>A computed style object.</returns>
    public IComputedStyle? BuildComputedStyle(ICssStyleDeclaration declaration, IElement element, IComputedStyle? parentStyle)
    {
        _variableResolver.ExtractVariablesFromStyle(element, declaration);

        var parentNode = parentStyle is ComputedStyle parentComputed ?
            parentComputed.PropertyTreeNode : null;

        var node = _propertyTreeManager.GetOrCreateNode(element, parentNode);
        var writingMode = GetWritingMode(declaration, element, parentStyle);

        ProcessDeclarationProperties(declaration, element, node, writingMode);
        _propertyTreeManager.OptimizeTree(node);

        return _styleFactory.CreateComputedStyle(element, parentStyle, declaration, node);
    }

    private void ProcessDeclarationProperties(
        ICssStyleDeclaration declaration,
        IElement element,
        IPropertyTreeNode node,
        WritingMode writingMode)
    {
        foreach (var property in declaration)
        {
            if (property.RawValue == null)
                continue;

            var propertyName = property.Name;
            var propertyValue = property.RawValue;

            var resolvedValue = _variableResolver.ResolveVariablesInValue(propertyValue, element, propertyName);
            var computedValue = _valueCalculator.Compute(resolvedValue, element, propertyName);

            if (computedValue == null)
                continue;

            if (_stylePropertyMapper.IsLogicalProperty(propertyName))
            {
                var physicalProps = _stylePropertyMapper.MapLogicalToPhysical(
                    propertyName, computedValue, writingMode);

                foreach (var physicalProp in physicalProps)
                {
                    node.SetProperty(physicalProp.Key, physicalProp.Value);
                }
            }
            else
            {
                node.SetProperty(propertyName, computedValue);
            }
        }
    }

    private WritingMode GetWritingMode(ICssStyleDeclaration declaration, IElement element, IComputedStyle? parentStyle)
    {
        var directionValue = declaration.GetPropertyValue("direction");
        var writingModeValue = declaration.GetPropertyValue("writing-mode");

        if (parentStyle != null && string.IsNullOrEmpty(directionValue) && string.IsNullOrEmpty(writingModeValue))
        {
            return parentStyle.WritingMode;
        }

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