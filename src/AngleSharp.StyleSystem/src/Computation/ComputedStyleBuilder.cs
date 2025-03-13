namespace AngleSharp.StyleSystem.Computation;
using System;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Integration;
using Interfaces;
using Models;

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

    public IComputedStyle? BuildComputedStyle(ICssStyleDeclaration declaration, IElement element, IComputedStyle? parentStyle)
    {
        // First extract CSS variables from the declaration
        _variableResolver.ExtractVariablesFromStyle(element, declaration);

        // Get parent node if available
        var parentNode = parentStyle is ComputedStyle parentComputed ?
            parentComputed.PropertyTreeNode : null;

        // Get or create property tree node
        var node = _propertyTreeManager.GetOrCreateNode(element, parentNode);

        // Get the writing mode to handle logical properties
        var writingMode = GetWritingMode(declaration, element, parentStyle);

        // Process each property in the declaration
        ProcessDeclarationProperties(declaration, element, node, writingMode);

        // Optimize the property tree for sharing and memory efficiency
        _propertyTreeManager.OptimizeTree(node);

        // Create the computed style using the property tree node
        return (_engine.StyleFactory as ComputedStyleFactory)?.CreateComputedStyle(element, parentStyle, declaration, node);
    }

    private void ProcessDeclarationProperties(
        ICssStyleDeclaration declaration,
        IElement element,
        IPropertyTreeNode node,
        WritingMode writingMode)
    {
        // Process each property in the declaration
        foreach (var property in declaration)
        {
            if (property.RawValue == null)
                continue;

            var propertyName = property.Name;
            var propertyValue = property.RawValue;

            // Resolve CSS variables
            var resolvedValue = _variableResolver.ResolveVariablesInValue(propertyValue, element, propertyName);

            // Compute absolute values (unit conversion, calc() evaluation, etc.)
            var computedValue = _valueCalculator.Compute(resolvedValue, element, propertyName);

            if (computedValue == null)
                continue;

            // Handle logical properties (mapping to physical properties based on writing mode)
            if (_stylePropertyMapper.IsLogicalProperty(propertyName))
            {
                var physicalProps = _stylePropertyMapper.MapLogicalToPhysical(
                    propertyName, computedValue, writingMode);

                // Store each physical property in the node
                foreach (var physicalProp in physicalProps)
                {
                    node.SetProperty(physicalProp.Key, physicalProp.Value);
                }
            }
            else
            {
                // Store the computed value in the property tree
                node.SetProperty(propertyName, computedValue);
            }
        }
    }

    private WritingMode GetWritingMode(ICssStyleDeclaration declaration, IElement element, IComputedStyle? parentStyle)
    {
        var directionValue = declaration.GetPropertyValue("direction");
        var writingModeValue = declaration.GetPropertyValue("writing-mode");

        // If no direction or writing-mode specified, inherit from parent
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