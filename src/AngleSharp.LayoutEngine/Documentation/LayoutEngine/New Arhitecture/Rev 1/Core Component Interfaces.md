```code
// This file defines the key interfaces for the StyleSystem architecture
// These interfaces establish the clear boundaries between components
namespace AngleSharp.LayoutEngine.StyleSystem
{
    using System;
    using System.Collections.Generic;
    using AngleSharp.Css;
    using AngleSharp.Css.Dom;
    using AngleSharp.Dom;

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
        Length FontSize { get; }
        
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
        Length Width { get; }
        
        /// <summary>
        /// Gets the computed height.
        /// </summary>
        Length Height { get; }
        
        /// <summary>
        /// Gets the logical inline size (width or height depending on writing mode).
        /// </summary>
        Length InlineSize { get; }
        
        /// <summary>
        /// Gets the logical block size (height or width depending on writing mode).
        /// </summary>
        Length BlockSize { get; }
        
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
        Length LineHeight { get; }
        
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
    /// Manages rule collection and matching.
    /// </summary>
    public interface IRuleCollector
    {
        /// <summary>
        /// Collects all rules that match the element.
        /// </summary>
        /// <param name="element">The element to match.</param>
        /// <param name="pseudoElement">Optional pseudo-element selector.</param>
        /// <returns>Collection of matched rules.</returns>
        IEnumerable<MatchedRule> CollectMatchingRules(IElement element, string? pseudoElement = null);
        
        /// <summary>
        /// Registers a stylesheet with the collector.
        /// </summary>
        /// <param name="stylesheet">The stylesheet to register.</param>
        /// <param name="origin">The origin of the stylesheet.</param>
        void RegisterStylesheet(ICssStyleSheet stylesheet, StylesheetOrigin origin);
        
        /// <summary>
        /// Unregisters a stylesheet from the collector.
        /// </summary>
        /// <param name="stylesheet">The stylesheet to unregister.</param>
        void UnregisterStylesheet(ICssStyleSheet stylesheet);
    }

    /// <summary>
    /// Resolves the cascade of conflicting CSS declarations.
    /// </summary>
    public interface ICascadeResolver
    {
        /// <summary>
        /// Resolves the cascade for an element with the given matched rules.
        /// </summary>
        /// <param name="matchedRules">The rules that matched the element.</param>
        /// <param name="element">The element being styled.</param>
        /// <returns>The cascaded style for the element.</returns>
        ICssStyleDeclaration ResolveCascade(IEnumerable<MatchedRule> matchedRules, IElement element);
    }

    /// <summary>
    /// Processes property inheritance.
    /// </summary>
    public interface IInheritanceProcessor
    {
        /// <summary>
        /// Applies inheritance to the element's style based on the parent's style.
        /// </summary>
        /// <param name="elementStyle">The element's cascaded style.</param>
        /// <param name="parentComputedStyle">The parent's computed style.</param>
        /// <returns>The element's style with inheritance applied.</returns>
        ICssStyleDeclaration ApplyInheritance(ICssStyleDeclaration elementStyle, IComputedStyle? parentComputedStyle);
    }

    /// <summary>
    /// Builds computed style objects from CSS declarations.
    /// </summary>
    public interface IComputedStyleBuilder
    {
        /// <summary>
        /// Builds a computed style from a CSS style declaration.
        /// </summary>
        /// <param name="style">The CSS style declaration.</param>
        /// <param name="element">The element being styled.</param>
        /// <param name="parentStyle">The parent element's computed style.</param>
        /// <returns>A computed style object.</returns>
        IComputedStyle BuildComputedStyle(ICssStyleDeclaration style, IElement element, IComputedStyle? parentStyle);
    }

    /// <summary>
    /// Resolves CSS custom property (variable) values.
    /// </summary>
    public interface IVariableResolver
    {
        /// <summary>
        /// Resolves a CSS variable reference.
        /// </summary>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="element">The element context.</param>
        /// <param name="defaultValue">Optional default value if the variable is not defined.</param>
        /// <returns>The resolved value or default.</returns>
        ICssValue? ResolveVariable(string variableName, IElement element, ICssValue? defaultValue = null);
        
        /// <summary>
        /// Resolves a var() function value.
        /// </summary>
        /// <param name="varValue">The var() function value.</param>
        /// <param name="element">The element context.</param>
        /// <returns>The resolved value.</returns>
        ICssValue? ResolveVarFunction(ICssValue varValue, IElement element);
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

    /// <summary>
    /// Coordinates with the document lifecycle.
    /// </summary>
    public interface IDocumentLifecycleCoordinator
    {
        /// <summary>
        /// Schedules a style update for the next frame.
        /// </summary>
        /// <param name="element">The root element of the subtree to update.</param>
        void ScheduleStyleUpdate(IElement element);
        
        /// <summary>
        /// Performs style updates synchronously.
        /// </summary>
        void UpdateStylesNow();
        
        /// <summary>
        /// Attaches to a document for lifecycle coordination.
        /// </summary>
        /// <param name="document">The document to attach to.</param>
        void AttachToDocument(IDocument document);
        
        /// <summary>
        /// Detaches from the current document.
        /// </summary>
        void DetachFromDocument();
    }

    /// <summary>
    /// Maps between logical and physical properties.
    /// </summary>
    public interface IStylePropertyMapper
    {
        /// <summary>
        /// Maps a logical property to physical properties.
        /// </summary>
        /// <param name="logicalProperty">The logical property name.</param>
        /// <param name="value">The property value.</param>
        /// <param name="writingMode">The writing mode context.</param>
        /// <returns>A dictionary of physical property names and values.</returns>
        IDictionary<string, ICssValue> MapLogicalToPhysical(string logicalProperty, ICssValue value, WritingMode writingMode);
        
        /// <summary>
        /// Maps physical properties to a logical property.
        /// </summary>
        /// <param name="physicalProperties">The physical property names and values.</param>
        /// <param name="logicalProperty">The logical property name.</param>
        /// <param name="writingMode">The writing mode context.</param>
        /// <returns>The logical property value.</returns>
        ICssValue? MapPhysicalToLogical(IDictionary<string, ICssValue> physicalProperties, string logicalProperty, WritingMode writingMode);
    }

    /// <summary>
    /// Represents a writing mode for text direction and flow.
    /// </summary>
    public struct WritingMode
    {
        /// <summary>
        /// Gets the text direction (LTR or RTL).
        /// </summary>
        public Direction Direction { get; }
        
        /// <summary>
        /// Gets the writing mode type.
        /// </summary>
        public WritingModeType Mode { get; }
        
        /// <summary>
        /// Gets whether the writing mode is horizontal.
        /// </summary>
        public bool IsHorizontal { get; }
        
        /// <summary>
        /// Gets whether the writing mode is vertical.
        /// </summary>
        public bool IsVertical { get; }
        
        /// <summary>
        /// Gets whether the text direction is right-to-left.
        /// </summary>
        public bool IsRightToLeft { get; }
    }

    /// <summary>
    /// Represents a matched CSS rule with metadata.
    /// </summary>
    public class MatchedRule
    {
        /// <summary>
        /// Gets the CSS style rule that matched.
        /// </summary>
        public ICssStyleRule Rule { get; }
        
        /// <summary>
        /// Gets the specificity of the matching selector.
        /// </summary>
        public Priority Specificity { get; }
        
        /// <summary>
        /// Gets the origin of the stylesheet containing the rule.
        /// </summary>
        public StylesheetOrigin Origin { get; }
        
        /// <summary>
        /// Gets the original index of the rule in the stylesheets.
        /// </summary>
        public int OriginalIndex { get; }
    }

    /// <summary>
    /// The origin of a stylesheet.
    /// </summary>
    public enum StylesheetOrigin
    {
        /// <summary>
        /// User agent (browser) stylesheet.
        /// </summary>
        UserAgent,
        
        /// <summary>
        /// User stylesheet.
        /// </summary>
        User,
        
        /// <summary>
        /// Author (document) stylesheet.
        /// </summary>
        Author
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
        Table,
        // Other display types...
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
    /// Text alignment options.
    /// </summary>
    public enum TextAlign
    {
        Left,
        Right,
        Center,
        Justify,
        Start,
        End
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
    /// Represents a length value with a unit.
    /// </summary>
    public readonly struct Length
    {
        /// <summary>
        /// Gets the value in pixels.
        /// </summary>
        public float Pixels { get; }
        
        /// <summary>
        /// Gets whether the length is auto.
        /// </summary>
        public bool IsAuto { get; }
        
        /// <summary>
        /// Gets whether the length is a percentage.
        /// </summary>
        public bool IsPercentage { get; }
        
        /// <summary>
        /// Gets the percentage value if applicable.
        /// </summary>
        public float Percentage { get; }
        
        /// <summary>
        /// Creates a length from a pixel value.
        /// </summary>
        /// <param name="pixels">The pixel value.</param>
        /// <returns>A new length.</returns>
        public static Length FromPixels(float pixels) => 
            new Length { Pixels = pixels };
        
        /// <summary>
        /// Creates an auto length.
        /// </summary>
        /// <returns>A new auto length.</returns>
        public static Length Auto => 
            new Length { IsAuto = true };
        
        /// <summary>
        /// Creates a percentage length.
        /// </summary>
        /// <param name="percentage">The percentage value.</param>
        /// <returns>A new percentage length.</returns>
        public static Length FromPercentage(float percentage) => 
            new Length { IsPercentage = true, Percentage = percentage };
    }

    /// <summary>
    /// Represents edges (margin, border, padding).
    /// </summary>
    public readonly struct Edges
    {
        /// <summary>
        /// Gets the top edge.
        /// </summary>
        public Length Top { get; }
        
        /// <summary>
        /// Gets the right edge.
        /// </summary>
        public Length Right { get; }
        
        /// <summary>
        /// Gets the bottom edge.
        /// </summary>
        public Length Bottom { get; }
        
        /// <summary>
        /// Gets the left edge.
        /// </summary>
        public Length Left { get; }
    }

    /// <summary>
    /// Represents logical edges independent of writing mode.
    /// </summary>
    public readonly struct LogicalEdges
    {
        /// <summary>
        /// Gets the block-start edge.
        /// </summary>
        public Length BlockStart { get; }
        
        /// <summary>
        /// Gets the inline-end edge.
        /// </summary>
        public Length InlineEnd { get; }
        
        /// <summary>
        /// Gets the block-end edge.
        /// </summary>
        public Length BlockEnd { get; }
        
        /// <summary>
        /// Gets the inline-start edge.
        /// </summary>
        public Length InlineStart { get; }
        
        /// <summary>
        /// Converts to physical edges based on writing mode.
        /// </summary>
        /// <param name="writingMode">The writing mode.</param>
        /// <returns>Physical edges.</returns>
        public Edges ToPhysical(WritingMode writingMode);
    }

    /// <summary>
    /// Represents a color value.
    /// </summary>
    public readonly struct Color
    {
        /// <summary>
        /// Gets the red component (0-255).
        /// </summary>
        public byte R { get; }
        
        /// <summary>
        /// Gets the green component (0-255).
        /// </summary>
        public byte G { get; }
        
        /// <summary>
        /// Gets the blue component (0-255).
        /// </summary>
        public byte B { get; }
        
        /// <summary>
        /// Gets the alpha component (0-1).
        /// </summary>
        public float A { get; }
        
        /// <summary>
        /// Creates a new color.
        /// </summary>
        /// <param name="r">Red component.</param>
        /// <param name="g">Green component.</param>
        /// <param name="b">Blue component.</param>
        /// <param name="a">Alpha component.</param>
        /// <returns>A new color.</returns>
        public static Color FromRgba(byte r, byte g, byte b, float a = 1.0f) => 
            new Color { R = r, G = g, B = b, A = a };
    }
}
```