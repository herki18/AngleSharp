namespace AngleSharp.Dom;

/// <summary>
///
/// </summary>
public interface ITextNodeFactory<TDocument, TTextNode>
    where TDocument : IDocument
    where TTextNode : IText
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    IText CreateTextNode(TDocument owner, string text);
}