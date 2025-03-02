namespace AngleSharp.Dom;

using System;
using Attributes;

[DomName("DocumentStyle")]
[DomNoInterfaceObject]
public interface IDocumentStyle
{
    [DomName("styleSheets")] IStyleSheetList StyleSheets { get; }

    /// <summary>
    ///     Gets or sets the selected set of stylesheets.
    /// </summary>
    [DomName("selectedStyleSheetSet")]
    String? SelectedStyleSheetSet { get; set; }

    [DomName("lastStyleSheetSet")] String? LastStyleSheetSet { get; }

    [DomName("preferredStyleSheetSet")] String? PreferredStyleSheetSet { get; }

    /// <summary>
    ///     Gets a live list of all currently-available style sheet sets.
    /// </summary>
    [DomName("styleSheetSets")]
    IStringList StyleSheetSets { get; }

    /// <summary>
    ///     Enables stylesheets matching the specified name and disables all others
    ///     (except those without a title, which are always enabled).
    /// </summary>
    [DomName("enableStyleSheetsForSet")]
    void EnableStyleSheetsForSet(String name);
}