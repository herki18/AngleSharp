namespace LayoutEngine.Core.LayoutNG.Public;

using System.Collections.Generic;

/// <summary>
/// Interface for layout objects that can contain child layout objects.
/// </summary>
public interface ILayoutContainer : ILayoutObject
{
    /// <summary>
    /// Gets the first child layout object.
    /// </summary>
    ILayoutObject? FirstChild { get; }

    /// <summary>
    /// Gets the last child layout object.
    /// </summary>
    ILayoutObject? LastChild { get; }

    /// <summary>
    /// Gets all child layout objects.
    /// </summary>
    IEnumerable<ILayoutObject> Children { get; }

    /// <summary>
    /// Gets the number of child layout objects.
    /// </summary>
    int ChildCount { get; }

    /// <summary>
    /// Adds a child layout object at the end.
    /// </summary>
    void AddChild(ILayoutObject child);

    /// <summary>
    /// Inserts a child layout object before the specified reference child.
    /// </summary>
    void InsertChild(ILayoutObject child, ILayoutObject? beforeChild);

    /// <summary>
    /// Removes a child layout object.
    /// </summary>
    void RemoveChild(ILayoutObject child);

    /// <summary>
    /// Removes all child layout objects.
    /// </summary>
    void RemoveAllChildren();

    /// <summary>
    /// Moves a child to a new position before the specified reference child.
    /// </summary>
    void MoveChild(ILayoutObject child, ILayoutObject? beforeChild);

    /// <summary>
    /// Creates anonymous wrapper blocks if needed for proper CSS box generation.
    /// For example, when inline elements contain block elements.
    /// </summary>
    void CreateAnonymousWrappersIfNeeded();

    /// <summary>
    /// Removes unnecessary anonymous wrappers after DOM changes.
    /// </summary>
    void RemoveUnnecessaryAnonymousWrappers();
}