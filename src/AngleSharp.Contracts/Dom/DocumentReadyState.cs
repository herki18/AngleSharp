namespace AngleSharp.Dom;

using Attributes;

public enum DocumentReadyState : System.Byte
{
    [DomName("loading")]
    Loading,
    [DomName("interactive")]
    Interactive,
    [DomName("complete")]
    Complete
}