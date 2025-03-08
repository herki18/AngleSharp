namespace AngleSharp.StyleSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using LayoutEngine.StyleSystem;

/// <summary>
/// The main entry point for style computation in the LayoutEngine.
/// </summary>
public class StyleEngine : IStyleEngine
{
    private readonly IBrowsingContext _context;
    private readonly PropertyTreeManager _propertyTreeManager;
    private readonly StyleCache _styleCache;
    private readonly RuleCollector _ruleCollector;
    private readonly CascadeResolver _cascadeResolver;
    private readonly InheritanceProcessor _inheritanceProcessor;
    private readonly ComputedStyleBuilder _computedStyleBuilder;
    private readonly StyleInvalidationTracker _invalidationTracker;
    private readonly IComputedStyleFactory _styleFactory;

    /// <summary>
    /// Creates a new StyleEngine instance.
    /// </summary>
    public StyleEngine(IBrowsingContext context)
    {
        _context = context;
        _propertyTreeManager = new PropertyTreeManager();
        _styleCache = new StyleCache();
        _ruleCollector = new RuleCollector(context);
        _cascadeResolver = new CascadeResolver();
        _inheritanceProcessor = new InheritanceProcessor();
        _computedStyleBuilder = new ComputedStyleBuilder(this);
        _invalidationTracker = new StyleInvalidationTracker();
        _styleFactory = new ComputedStyleFactory(this);
    }

    /// <summary>
    /// Gets the style invalidation tracker.
    /// </summary>
    public IStyleInvalidationTracker InvalidationTracker => _invalidationTracker;

    /// <summary>
    /// Gets the factory for creating computed style objects.
    /// </summary>
    public IComputedStyleFactory StyleFactory => _styleFactory;

    /// <summary>
    /// Gets the browsing context.
    /// </summary>
    public IBrowsingContext Context => _context;

    /// <summary>
    /// Gets the property tree manager.
    /// </summary>
    internal PropertyTreeManager PropertyTreeManager => _propertyTreeManager;

    /// <summary>
    /// Computes the style for an element.
    /// </summary>
    public IComputedStyle ComputeElementStyle(IElement element, string? pseudoElement = null)
    {
        // Check the style cache first
        var cacheKey = new StyleCacheKey(element, pseudoElement);
        if (_styleCache.TryGetValue(cacheKey, out var cachedStyle))
        {
            return cachedStyle;
        }

        // Compute parent style first (if applicable)
        IComputedStyle parentStyle = null;
        if (element.ParentElement != null)
        {
            parentStyle = ComputeElementStyle(element.ParentElement);
        }

        // Collect matching rules
        var matchedRules = _ruleCollector.CollectMatchingRules(element, pseudoElement);

        // Resolve the cascade to determine winning declarations
        var cascadedStyle = _cascadeResolver.ResolveCascade(matchedRules, element);

        // Apply inheritance
        var inheritedStyle = _inheritanceProcessor.ApplyInheritance(cascadedStyle, parentStyle);

        // Build final computed style
        var computedStyle = _computedStyleBuilder.BuildComputedStyle(inheritedStyle, element, parentStyle);

        // Cache the result
        _styleCache.Store(cacheKey, computedStyle);

        // Mark element as up-to-date in the invalidation tracker
        _invalidationTracker.MarkAsUpToDate(element);

        return computedStyle;
    }

    /// <summary>
    /// Updates styles after a change to the DOM or stylesheets.
    /// </summary>
    public void UpdateStyles(IElement root)
    {
        // First, update the invalidation state
        var elementsToUpdate = _invalidationTracker.GetElementsToUpdate(root);

        // Then update each element's style
        foreach (var element in elementsToUpdate)
        {
            // Remove from cache to force recomputation
            _styleCache.Remove(new StyleCacheKey(element, null));

            // Recompute style
            ComputeElementStyle(element);
        }
    }
}

/// <summary>
/// Factory for creating computed style objects.
/// </summary>
public class ComputedStyleFactory : IComputedStyleFactory
{
    private readonly StyleEngine _engine;

    public ComputedStyleFactory(StyleEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Creates a new computed style object.
    /// </summary>
    public IComputedStyle CreateComputedStyle()
    {
        // In practice, you always need an element and style declaration to create a computed style
        throw new InvalidOperationException("Cannot create a computed style without context");
    }

    /// <summary>
    /// Creates a computed style by copying another.
    /// </summary>
    public IComputedStyle CopyComputedStyle(IComputedStyle source)
    {
        // In a real implementation, we would create a deep copy
        throw new NotImplementedException("Copying computed styles is not implemented yet");
    }

    /// <summary>
    /// Creates a computed style for an element with the given declarations.
    /// </summary>
    internal IComputedStyle CreateComputedStyle(IElement element, IComputedStyle parentStyle, ICssStyleDeclaration declaration)
    {
        // Create a property tree node for the element
        var parentNode = parentStyle != null && parentStyle is ComputedStyle parentComputed
            ? parentComputed.PropertyTreeNode
            : null;

        var propertyNode = _engine.PropertyTreeManager.GetOrCreateNode(element, parentNode);

        // Create the computed style
        return new ComputedStyle(element, parentStyle, declaration, propertyNode);
    }
}

/// <summary>
/// Collects and matches CSS rules for elements.
/// </summary>
public class RuleCollector
{
    private readonly IBrowsingContext _context;
    private readonly List<StylesheetEntry> _stylesheets = new List<StylesheetEntry>();

    public RuleCollector(IBrowsingContext context)
    {
        _context = context;
        RegisterDocumentStylesheets();
    }

    /// <summary>
    /// Collects all rules that match the element.
    /// </summary>
    public IEnumerable<MatchedRule> CollectMatchingRules(IElement element, string pseudoElement = null)
    {
        var matchedRules = new List<MatchedRule>();
        var index = 0;

        // For each stylesheet
        foreach (var stylesheetEntry in _stylesheets)
        {
            // For each rule in the stylesheet
            foreach (var rule in stylesheetEntry.Stylesheet.Rules.OfType<ICssStyleRule>())
            {
                // Check if the rule's selector matches the element and pseudoElement
                if (DoesSelectorMatch(rule, element, pseudoElement))
                {
                    // Calculate specificity
                    var specificity = CalculateSpecificity(rule.SelectorText);

                    matchedRules.Add(new MatchedRule
                    {
                        Rule = rule,
                        Specificity = new Priority(specificity),
                        Origin = stylesheetEntry.Origin,
                        OriginalIndex = index++
                    });
                }
            }
        }

        // Return matched rules sorted by specificity and order
        return matchedRules.OrderBy(r => r.Specificity)
            .ThenBy(r => (int)r.Origin)
            .ThenBy(r => r.OriginalIndex);
    }

    /// <summary>
    /// Registers a stylesheet with the collector.
    /// </summary>
    public void RegisterStylesheet(ICssStyleSheet stylesheet, StylesheetOrigin origin)
    {
        _stylesheets.Add(new StylesheetEntry
        {
            Stylesheet = stylesheet,
            Origin = origin
        });
    }

    /// <summary>
    /// Unregisters a stylesheet from the collector.
    /// </summary>
    public void UnregisterStylesheet(ICssStyleSheet stylesheet)
    {
        _stylesheets.RemoveAll(entry => entry.Stylesheet == stylesheet);
    }

    /// <summary>
    /// Registers all stylesheets in the document.
    /// </summary>
    private void RegisterDocumentStylesheets()
    {
        // Register user agent stylesheets
        // In a real implementation, we would load default styles

        // Register user stylesheets
        // In a real implementation, we would load user preferences

        // Register document stylesheets
        var document = _context.Active;
        if (document != null)
        {
            foreach (var stylesheet in document.StyleSheets)
            {
                RegisterStylesheet(stylesheet as ICssStyleSheet, StylesheetOrigin.Author);
            }
        }
    }

    /// <summary>
    /// Checks if a rule's selector matches an element.
    /// </summary>
    private bool DoesSelectorMatch(ICssStyleRule rule, IElement element, string pseudoElement)
    {
        // In a real implementation, we would use AngleSharp's selector matching
        // For simplicity, we'll do a very basic check here
        try
        {
            var matches = element.Matches(rule.SelectorText);

            // TODO: Handle pseudo-elements properly
            var hasPseudo = rule.SelectorText.Contains(":");

            return matches && (pseudoElement == null || hasPseudo);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Calculates the specificity of a selector.
    /// </summary>
    private int CalculateSpecificity(string selector)
    {
        // In a real implementation, we would calculate actual specificity
        // For simplicity, we'll return a placeholder value
        return 1;
    }

    private class StylesheetEntry
    {
        public ICssStyleSheet Stylesheet { get; set; }
        public StylesheetOrigin Origin { get; set; }
    }
}

/// <summary>
/// Resolves the cascade of conflicting CSS declarations.
/// </summary>
public class CascadeResolver
{
    /// <summary>
    /// Resolves the cascade for an element with the given matched rules.
    /// </summary>
    public ICssStyleDeclaration ResolveCascade(IEnumerable<MatchedRule> matchedRules, IElement element)
    {
        // Create a new style declaration to hold the cascaded styles
        var result = element.Owner.Context.CreateStyleDeclaration();

        // Apply rules in order of specificity
        foreach (var rule in matchedRules)
        {
            foreach (var property in rule.Rule.Style.Declarations)
            {
                // Only override if the new declaration has higher specificity or is !important
                var existing = result.GetProperty(property.Name);
                if (existing == null || property.IsImportant && !existing.IsImportant)
                {
                    result.SetProperty(property.Name, property.Value, property.IsImportant ? "important" : null);
                }
            }
        }

        // Apply inline styles (highest specificity)
        if (element is IHtmlElement htmlElement)
        {
            var inlineStyle = htmlElement.Style;

            foreach (var property in inlineStyle.Declarations)
            {
                // Inline styles override everything except !important rules
                var existing = result.GetProperty(property.Name);
                if (existing == null || !existing.IsImportant)
                {
                    result.SetProperty(property.Name, property.Value, property.IsImportant ? "important" : null);
                }
            }
        }

        return result;
    }
}

/// <summary>
/// Processes property inheritance.
/// </summary>
public class InheritanceProcessor
{
    /// <summary>
    /// Applies inheritance to the element's style based on the parent's style.
    /// </summary>
    public ICssStyleDeclaration ApplyInheritance(ICssStyleDeclaration elementStyle, IComputedStyle parentComputedStyle)
    {
        // If there's no parent, no inheritance needed
        if (parentComputedStyle == null)
        {
            return elementStyle;
        }

        // Create a new style declaration to hold the inherited styles
        var result = elementStyle.Parent?.Owner?.Context?.CreateStyleDeclaration() ?? elementStyle;

        // Copy all properties from the element style
        foreach (var property in elementStyle.Declarations)
        {
            result.SetProperty(property.Name, property.Value, property.IsImportant ? "important" : null);
        }

        // Define inheritable properties
        var inheritableProperties = new[]
        {
            "color", "font-family", "font-size", "font-weight", "font-style",
            "line-height", "text-align", "text-indent", "text-transform",
            "letter-spacing", "word-spacing", "white-space",
            "direction", "visibility"
            // Add more inheritable properties as needed
        };

        // Apply inheritance for properties not explicitly set
        foreach (var propertyName in inheritableProperties)
        {
            if (result.GetProperty(propertyName) == null)
            {
                // Inherit from parent
                var inheritedValue = parentComputedStyle.GetPropertyValue(propertyName);
                if (!string.IsNullOrEmpty(inheritedValue))
                {
                    result.SetProperty(propertyName, inheritedValue);
                }
            }
        }

        return result;
    }
}

/// <summary>
/// Builds computed style objects from CSS declarations.
/// </summary>
public class ComputedStyleBuilder
{
    private readonly StyleEngine _engine;

    public ComputedStyleBuilder(StyleEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Builds a computed style from a CSS style declaration.
    /// </summary>
    public IComputedStyle BuildComputedStyle(ICssStyleDeclaration style, IElement element, IComputedStyle parentStyle)
    {
        // Create a computed style via the factory
        return (_engine.StyleFactory as ComputedStyleFactory)
            .CreateComputedStyle(element, parentStyle, style);
    }
}

/// <summary>
/// Tracks style invalidation and dependencies.
/// </summary>
public class StyleInvalidationTracker : IStyleInvalidationTracker
{
    private readonly Dictionary<IElement, InvalidationState> _invalidationStates = new Dictionary<IElement, InvalidationState>();
    private readonly Dictionary<IElement, HashSet<IElement>> _dependencies = new Dictionary<IElement, HashSet<IElement>>();

    /// <summary>
    /// Marks an element as needing style recalculation.
    /// </summary>
    public void InvalidateElement(IElement element)
    {
        MarkAsInvalid(element);

        // Also invalidate dependent elements
        if (_dependencies.TryGetValue(element, out var dependents))
        {
            foreach (var dependent in dependents)
            {
                MarkAsInvalid(dependent);
            }
        }

        // Invalidate children for inheritance
        InvalidateSubtree(element);
    }

    /// <summary>
    /// Marks specific properties as needing recalculation.
    /// </summary>
    public void InvalidateProperties(IElement element, IEnumerable<string> properties)
    {
        // For simplicity, we'll just invalidate the entire element
        // In a real implementation, we'd track property-level invalidation
        InvalidateElement(element);
    }

    /// <summary>
    /// Determines if an element needs style recalculation.
    /// </summary>
    public bool NeedsStyleRecalculation(IElement element)
    {
        if (_invalidationStates.TryGetValue(element, out var state))
        {
            return state == InvalidationState.Invalid;
        }

        // If not tracked, assume it needs calculation
        return true;
    }

    /// <summary>
    /// Tracks a style dependency between elements.
    /// </summary>
    public void TrackDependency(IElement dependent, IElement source)
    {
        if (!_dependencies.TryGetValue(source, out var dependents))
        {
            dependents = new HashSet<IElement>();
            _dependencies[source] = dependents;
        }

        dependents.Add(dependent);
    }

    /// <summary>
    /// Gets all elements that need to be updated in a subtree.
    /// </summary>
    public IEnumerable<IElement> GetElementsToUpdate(IElement root)
    {
        var result = new List<IElement>();
        CollectInvalidElements(root, result);
        return result;
    }

    /// <summary>
    /// Marks an element as up-to-date after style calculation.
    /// </summary>
    public void MarkAsUpToDate(IElement element)
    {
        _invalidationStates[element] = InvalidationState.Valid;
    }

    /// <summary>
    /// Recursively invalidates a subtree.
    /// </summary>
    private void InvalidateSubtree(IElement element)
    {
        foreach (var child in element.Children)
        {
            MarkAsInvalid(child);
            InvalidateSubtree(child);
        }
    }

    /// <summary>
    /// Marks an element as invalid.
    /// </summary>
    private void MarkAsInvalid(IElement element)
    {
        _invalidationStates[element] = InvalidationState.Invalid;
    }

    /// <summary>
    /// Recursively collects invalid elements.
    /// </summary>
    private void CollectInvalidElements(IElement element, List<IElement> result)
    {
        if (NeedsStyleRecalculation(element))
        {
            result.Add(element);
        }

        foreach (var child in element.Children)
        {
            CollectInvalidElements(child, result);
        }
    }

    private enum InvalidationState
    {
        Valid,
        Invalid
    }
}

/// <summary>
/// Caches computed styles to avoid redundant computation.
/// </summary>
public class StyleCache
{
    private readonly Dictionary<StyleCacheKey, IComputedStyle> _cache = new Dictionary<StyleCacheKey, IComputedStyle>();
    private readonly int _maxSize = 10000; // Arbitrary limit to prevent unbounded growth

    /// <summary>
    /// Tries to get a cached style.
    /// </summary>
    public bool TryGetValue(StyleCacheKey key, out IComputedStyle style)
    {
        return _cache.TryGetValue(key, out style);
    }

    /// <summary>
    /// Stores a computed style in the cache.
    /// </summary>
    public void Store(StyleCacheKey key, IComputedStyle style)
    {
        if (_cache.Count >= _maxSize)
        {
            // In a real implementation, we'd use a more sophisticated eviction strategy
            _cache.Clear();
        }

        _cache[key] = style;
    }

    /// <summary>
    /// Removes a style from the cache.
    /// </summary>
    public void Remove(StyleCacheKey key)
    {
        _cache.Remove(key);
    }

    /// <summary>
    /// Clears the entire cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }
}

/// <summary>
/// A key for the style cache.
/// </summary>
public readonly struct StyleCacheKey : IEquatable<StyleCacheKey>
{
    public readonly IElement Element;
    public readonly string PseudoElement;

    public StyleCacheKey(IElement element, string pseudoElement)
    {
        Element = element;
        PseudoElement = pseudoElement;
    }

    public override bool Equals(object obj)
    {
        return obj is StyleCacheKey key && Equals(key);
    }

    public bool Equals(StyleCacheKey other)
    {
        return EqualityComparer<IElement>.Default.Equals(Element, other.Element) &&
               PseudoElement == other.PseudoElement;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Element, PseudoElement);
    }
}

/// <summary>
/// Represents CSS selector specificity.
/// </summary>
public readonly struct Priority : IComparable<Priority>, IEquatable<Priority>
{
    // In a real implementation, this would have separate fields for ID, class, and element selectors
    private readonly int _value;

    public Priority(int value)
    {
        _value = value;
    }

    public int CompareTo(Priority other)
    {
        return _value.CompareTo(other._value);
    }

    public bool Equals(Priority other)
    {
        return _value == other._value;
    }

    public override bool Equals(object obj)
    {
        return obj is Priority priority && Equals(priority);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }
}

/// <summary>
/// Represents a writing mode for text direction and flow.
/// </summary>
public readonly struct WritingMode : IEquatable<WritingMode>
{
    /// <summary>
    /// Gets the text direction (LTR or RTL).
    /// </summary>
    public Direction Direction { get; }

    /// <summary>
    /// Gets the writing mode type.
    /// </summary>
    public WritingModeType Mode { get; }

    public WritingMode(Direction direction, WritingModeType mode)
    {
        Direction = direction;
        Mode = mode;
    }

    /// <summary>
    /// Gets whether the writing mode is horizontal.
    /// </summary>
    public bool IsHorizontal => Mode == WritingModeType.HorizontalTopToBottom;

    /// <summary>
    /// Gets whether the writing mode is vertical.
    /// </summary>
    public bool IsVertical => !IsHorizontal;

    /// <summary>
    /// Gets whether the text direction is right-to-left.
    /// </summary>
    public bool IsRightToLeft => Direction == Direction.Rtl;

    public bool Equals(WritingMode other)
    {
        return Direction == other.Direction && Mode == other.Mode;
    }

    public override bool Equals(object obj)
    {
        return obj is WritingMode mode && Equals(mode);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Direction, Mode);
    }
}

/// <summary>
/// The display type of an element.
/// </summary>
public enum DisplayType
{
    None,
    Block,
    Inline,
    InlineBlock,
    Flex,
    Grid,
    Table
}

/// <summary>
/// The position type of an element.
/// </summary>
public enum PositionType
{
    Static,
    Relative,
    Absolute,
    Fixed,
    Sticky
}


/// <summary>
/// Text direction options.
/// </summary>
public enum Direction
{
    Ltr,
    Rtl
}

/// <summary>
/// Writing mode types.
/// </summary>
public enum WritingModeType
{
    HorizontalTopToBottom,
    VerticalRightToLeft,
    VerticalLeftToRight,
    SidewaysRightToLeft,
    SidewaysLeftToRight
}

/// <summary>
/// The origin of a stylesheet.
/// </summary>
public enum StylesheetOrigin
{
    UserAgent,
    User,
    Author
}