namespace AngleSharp.LayoutEngine.StyleSystem;

using AngleSharp.Css.Dom;

public record StylesheetEntry(ICssStyleSheet Stylesheet, StylesheetOrigin Origin);