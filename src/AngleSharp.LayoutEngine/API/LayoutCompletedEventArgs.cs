namespace AngleSharp.LayoutEngine.API;

using System;

#pragma warning disable CS8604, CS8618, CS9264, CS8600, CS8602, CS8603, CS8625
/// <summary>
/// Event arguments for when layout is completed.
/// </summary>
public class LayoutCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Creates new layout completed event arguments.
    /// </summary>
    /// <param name="result">The layout result.</param>
    public LayoutCompletedEventArgs(LayoutResult result)
    {
        Result = result;
    }

    /// <summary>
    /// Gets the layout result.
    /// </summary>
    public LayoutResult Result { get; }
}