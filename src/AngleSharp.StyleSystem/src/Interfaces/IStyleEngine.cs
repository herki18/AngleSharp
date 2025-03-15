namespace AngleSharp.StyleSystem.Interfaces;

using AngleSharp.Css;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Storage;

/// <summary>
/// Defines the interface for a style engine that computes CSS styles for DOM elements.
/// </summary>
public interface IStyleEngine
{
    /// <summary>
    /// Computes the style for a specific element.
    /// </summary>
    /// <param name="element">The element to compute styles for.</param>
    /// <param name="pseudoElement">Optional pseudo-element selector.</param>
    /// <returns>The computed style for the element.</returns>
    IComputedStyle ComputeElementStyle(IElement element, string? pseudoElement = null);

    /// <summary>
    /// Updates styles for an entire subtree starting at the given root element.
    /// </summary>
    /// <param name="root">The root element of the subtree to update.</param>
    void UpdateStyles(IElement root);

    /// <summary>
    /// Gets the style invalidation tracker used by this engine.
    /// </summary>
    IStyleInvalidationTracker InvalidationTracker { get; }

    /// <summary>
    /// Gets the factory used to create computed style objects.
    /// </summary>
    IComputedStyleFactory StyleFactory { get; }

    /// <summary>
    /// Gets the browsing context this engine is associated with.
    /// </summary>
    IBrowsingContext Context { get; }

    /// <summary>
    /// Gets or sets the render device used for viewport-related calculations.
    /// </summary>
    IRenderDevice RenderDevice { get; set; }

    /// <summary>
    /// Gets the stylesheet manager used by this engine.
    /// </summary>
    IStyleSheetManager StylesheetManager { get; }

    /// <summary>
    /// Gets the rule collector used by this engine.
    /// </summary>
    IRuleCollector RuleCollector { get; }

    /// <summary>
    /// Gets the cascade resolver used by this engine.
    /// </summary>
    ICascadeResolver CascadeResolver { get; }

    /// <summary>
    /// Gets the inheritance processor used by this engine.
    /// </summary>
    IInheritanceProcessor InheritanceProcessor { get; }

    /// <summary>
    /// Gets the variable resolver used by this engine.
    /// </summary>
    IVariableResolver VariableResolver { get; }

    /// <summary>
    /// Gets the computed style builder used by this engine.
    /// </summary>
    IComputedStyleBuilder ComputedStyleBuilder { get; }

    /// <summary>
    /// Gets the property tree manager used by this engine.
    /// </summary>
    IPropertyTreeManager PropertyTreeManager { get; }

    /// <summary>
    /// Gets or sets whether style optimization is enabled.
    /// </summary>
    bool OptimizationEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether to collect optimization metrics.
    /// </summary>
    bool CollectMetrics { get; set; }

    /// <summary>
    /// Gets optimization metrics.
    /// </summary>
    /// <returns>Metrics about optimizations performed.</returns>
    OptimizationMetrics GetOptimizationMetrics();

    /// <summary>
    /// Notifies the engine of a viewport size change.
    /// </summary>
    /// <param name="width">The new viewport width.</param>
    /// <param name="height">The new viewport height.</param>
    void NotifyViewportChanged(int width, int height);

    /// <summary>
    /// Optimizes all computed styles in the system.
    /// </summary>
    void OptimizeAllStyles();
}