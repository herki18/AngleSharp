using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;

namespace AngleSharp.LayoutEngine.StyleSystem
{
    using AngleSharp.StyleSystem;

    /// <summary>
    /// Main entry point for style computation.
    /// </summary>
    public interface IStyleEngine
    {
        /// <summary>
        /// Computes the style for an element.
        /// </summary>
        /// <param name="element">The element to compute styles for.</param>
        /// <param name="pseudoElement">Optional pseudo-element selector.</param>
        /// <returns>The computed style for the element.</returns>
        IComputedStyle ComputeElementStyle(IElement element, string? pseudoElement = null);

        /// <summary>
        /// Updates styles after a change to the DOM or stylesheets.
        /// </summary>
        /// <param name="root">The root element of the subtree to update.</param>
        void UpdateStyles(IElement root);

        /// <summary>
        /// Gets a factory for creating computed style objects.
        /// </summary>
        IComputedStyleFactory StyleFactory { get; }

        /// <summary>
        /// Gets the style invalidation tracker.
        /// </summary>
        IStyleInvalidationTracker InvalidationTracker { get; }
    }

    /// <summary>
    /// Factory for creating computed style objects.
    /// </summary>
    public interface IComputedStyleFactory
    {
        /// <summary>
        /// Creates a new computed style object.
        /// </summary>
        /// <returns>A new computed style object.</returns>
        IComputedStyle CreateComputedStyle();

        /// <summary>
        /// Creates a computed style by copying another.
        /// </summary>
        /// <param name="source">The source style to copy.</param>
        /// <returns>A new computed style object with copied values.</returns>
        IComputedStyle CopyComputedStyle(IComputedStyle source);
    }

    /// <summary>
    /// Represents a computed style with optimized property access.
    /// </summary>
    public interface IComputedStyle
    {
        /// <summary>
        /// Gets a computed value by property name.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The computed value.</returns>
        string GetPropertyValue(string propertyName);

        /// <summary>
        /// Gets a typed value for a specific property.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The typed value.</returns>
        T GetValue<T>(string propertyName);

        /// <summary>
        /// Gets the display type of the element.
        /// </summary>
        DisplayType Display { get; }

        /// <summary>
        /// Gets the position type of the element.
        /// </summary>
        PositionType Position { get; }

        /// <summary>
        /// Gets the computed value of the opacity property.
        /// </summary>
        float Opacity { get; }

        /// <summary>
        /// Gets the computed value of the z-index property.
        /// </summary>
        int ZIndex { get; }

        /// <summary>
        /// Gets the computed value of the font-size property.
        /// </summary>
        CssLengthValue FontSize { get; }

        /// <summary>
        /// Gets the writing mode for the element.
        /// </summary>
        WritingMode WritingMode { get; }

        /// <summary>
        /// Gets box-related properties.
        /// </summary>
        IBoxProperties Box { get; }

        /// <summary>
        /// Gets text-related properties.
        /// </summary>
        ITextProperties Text { get; }
    }

    /// <summary>
    /// Box-related computed properties.
    /// </summary>
    public interface IBoxProperties
    {
        /// <summary>
        /// Gets the computed width.
        /// </summary>
        CssLengthValue Width { get; }

        /// <summary>
        /// Gets the computed height.
        /// </summary>
        CssLengthValue Height { get; }

        /// <summary>
        /// Gets the logical inline size (width or height depending on writing mode).
        /// </summary>
        CssLengthValue InlineSize { get; }

        /// <summary>
        /// Gets the logical block size (height or width depending on writing mode).
        /// </summary>
        CssLengthValue BlockSize { get; }

        /// <summary>
        /// Gets the margin edges.
        /// </summary>
        Edges Margin { get; }

        /// <summary>
        /// Gets the border edges.
        /// </summary>
        Edges Border { get; }

        /// <summary>
        /// Gets the padding edges.
        /// </summary>
        Edges Padding { get; }

        /// <summary>
        /// Gets the logical margin edges.
        /// </summary>
        LogicalEdges LogicalMargin { get; }

        /// <summary>
        /// Gets the logical border edges.
        /// </summary>
        LogicalEdges LogicalBorder { get; }

        /// <summary>
        /// Gets the logical padding edges.
        /// </summary>
        LogicalEdges LogicalPadding { get; }
    }

    /// <summary>
    /// Text-related computed properties.
    /// </summary>
    public interface ITextProperties
    {
        /// <summary>
        /// Gets the computed font family.
        /// </summary>
        string FontFamily { get; }

        /// <summary>
        /// Gets the computed font weight.
        /// </summary>
        int FontWeight { get; }

        /// <summary>
        /// Gets whether the font style is italic.
        /// </summary>
        bool IsItalic { get; }

        /// <summary>
        /// Gets the computed line height.
        /// </summary>
        CssLengthValue LineHeight { get; }

        /// <summary>
        /// Gets the computed text align property.
        /// </summary>
        TextAlign TextAlign { get; }

        /// <summary>
        /// Gets the computed color.
        /// </summary>
        Color Color { get; }
    }

    /// <summary>
    /// Tracks style invalidation and dependencies.
    /// </summary>
    public interface IStyleInvalidationTracker
    {
        /// <summary>
        /// Marks an element as needing style recalculation.
        /// </summary>
        /// <param name="element">The element to invalidate.</param>
        void InvalidateElement(IElement element);

        /// <summary>
        /// Marks specific properties as needing recalculation.
        /// </summary>
        /// <param name="element">The element with properties to invalidate.</param>
        /// <param name="properties">The names of the properties to invalidate.</param>
        void InvalidateProperties(IElement element, IEnumerable<string> properties);

        /// <summary>
        /// Determines if an element needs style recalculation.
        /// </summary>
        /// <param name="element">The element to check.</param>
        /// <returns>True if the element needs recalculation; otherwise, false.</returns>
        bool NeedsStyleRecalculation(IElement element);

        /// <summary>
        /// Tracks a style dependency between elements.
        /// </summary>
        /// <param name="dependent">The element that depends on the source.</param>
        /// <param name="source">The element that affects the dependent.</param>
        void TrackDependency(IElement dependent, IElement source);
    }
}