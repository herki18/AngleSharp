namespace AngleSharp.Renderer;

#pragma warning disable CS8604, CS1591
/// <summary>
/// Handles automatic margin calculations for CSS-style layouts, particularly for centering elements
/// and distributing available space when margins are set to 'auto'.
/// </summary>
public static class AutoMarginResolver
{
    /// <summary>
    /// Calculates the final left and right margins when one or both margins are set to 'auto'.
    /// Following CSS box model rules:
    /// - If both margins are auto, the element is centered by distributing space equally
    /// - If one margin is auto, it takes up all remaining space
    /// - If no margins are auto, the original margin values are returned
    /// </summary>
    /// <param name="parentAvailableWidth">Total available width in the parent container</param>
    /// <param name="elementContentWidth">Width of the element itself</param>
    /// <param name="marginLeftVal">Specified left margin value (ignored if marginLeftIsAuto is true)</param>
    /// <param name="marginRightVal">Specified right margin value (ignored if marginRightIsAuto is true)</param>
    /// <param name="marginLeftIsAuto">Whether left margin is set to 'auto'</param>
    /// <param name="marginRightIsAuto">Whether right margin is set to 'auto'</param>
    /// <returns>Tuple containing the resolved (left margin, right margin) values</returns>
    public static (float marginLeft, float marginRight) CalculateAutoMargins(
        float parentAvailableWidth,
        float elementContentWidth,
        float marginLeftVal,
        float marginRightVal,
        bool marginLeftIsAuto,
        bool marginRightIsAuto)
    {
        // Calculate remaining space after accounting for element width and non-auto margins
        float usedNonAutoSpace = marginLeftVal + elementContentWidth + marginRightVal;
        float remainingSpace = parentAvailableWidth - usedNonAutoSpace;

        // Ensure we don't have negative space (prevents overflow)
        if (remainingSpace < 0)
            remainingSpace = 0;

        // Both margins auto: center the element
        if (marginLeftIsAuto && marginRightIsAuto)
        {
            float halfSpace = remainingSpace / 2f;
            return (halfSpace, halfSpace);
        }

        // Left margin auto: use all remaining space on left
        if (marginLeftIsAuto)
        {
            return (remainingSpace, marginRightVal);
        }

        // Right margin auto: use all remaining space on right
        if (marginRightIsAuto)
        {
            return (marginLeftVal, remainingSpace);
        }

        // No auto margins: return original values
        return (marginLeftVal, marginRightVal);
    }
}