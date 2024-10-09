namespace AngleSharp.Html.Dom
{
    using AngleSharp.Dom;
    using System;
    using ViewSync;

    /// <summary>
    /// Represents the HTML div element.
    /// </summary>
    sealed class HtmlDivElement : HtmlElement, IHtmlDivElement
    {
        public HtmlDivElement(Document owner, String? prefix = null, IViewSynchronizer? view = null)
            : base(owner, TagNames.Div, prefix, NodeFlags.Special, view)
        {
        }
    }
}
