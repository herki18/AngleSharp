namespace AngleSharp.Html;

using System;
using AngleSharp.Dom;

/// <inheritdoc />
public class TextNodeFactory : ITextNodeFactory<Document, IText>
{
    /// <inheritdoc />
    public IText CreateTextNode(Document owner, String text)
    {
        return new TextNode(owner, text);
    }
}