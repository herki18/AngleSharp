namespace LayoutEngine.Contracts.LayoutSystem;

/// <summary>
/// Represents box edges (top, right, bottom, left).
/// </summary>
public struct BoxEdges
{
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }
        
    public BoxEdges(float all)
    {
        Top = Right = Bottom = Left = all;
    }
        
    public BoxEdges(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }
}