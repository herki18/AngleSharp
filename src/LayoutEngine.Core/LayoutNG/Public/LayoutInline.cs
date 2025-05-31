namespace LayoutEngine.Core.LayoutNG.Public;

using System.Linq;
using AngleSharp.Dom;

/// <summary>
/// Layout object for inline elements (span, a, em, strong, etc).
/// </summary>
public class LayoutInline : LayoutContainer
{
    public LayoutInline(INode? node) : base(node)
    {
    }

    public override LayoutObjectType Type => LayoutObjectType.Inline;

    public override bool IsInline => true;

    public override void CreateAnonymousWrappersIfNeeded()
    {
        // Check if we have any block children
        bool hasBlockChildren = Children.Any(c => c.IsBlock);

        if (hasBlockChildren)
        {
            // This is an unusual case - inline elements shouldn't contain blocks
            // But if it happens, we need to handle it by splitting the inline
            HandleBlockInInline();
        }
    }

    private void HandleBlockInInline()
    {
        // When an inline contains a block, we need to:
        // 1. Close the inline before the block
        // 2. Create the block
        // 3. Re-open the inline after the block

        // This requires coordination with the parent container
        if (Parent is ILayoutContainer parentContainer)
        {
            var childrenToProcess = Children.ToList();
            var beforeThis = GetNextSibling(this);

            // Remove this inline from parent temporarily
            parentContainer.RemoveChild(this);

            LayoutInline? currentInline = null;

            foreach (var child in childrenToProcess)
            {
                RemoveChild(child);

                if (child.IsBlock)
                {
                    // Insert block directly into parent
                    parentContainer.InsertChild(child, beforeThis);
                    currentInline = null;
                }
                else
                {
                    if (currentInline == null)
                    {
                        // Create new inline continuation
                        currentInline = CreateInlineContinuation();
                        parentContainer.InsertChild(currentInline, beforeThis);
                    }

                    currentInline.AddChild(child);
                }
            }

            // If we have any children left, re-insert this inline
            if (ChildCount > 0)
            {
                parentContainer.InsertChild(this, beforeThis);
            }
            else if (currentInline == null)
            {
                // No children left and no continuation created,
                // create empty inline to maintain structure
                parentContainer.InsertChild(this, beforeThis);
            }
        }
    }

    private LayoutInline CreateInlineContinuation()
    {
        // Create a new inline with the same style as this one
        var continuation = IsAnonymous
            ? new LayoutAnonymousInline()
            : new LayoutInline(null);

        continuation.Style = Style;
        return continuation;
    }

    private new ILayoutObject? GetNextSibling(ILayoutObject child)
    {
        if (Parent is LayoutContainer container)
        {
            return container.GetNextSibling(child);
        }
        return null;
    }
}

/// <summary>
/// Anonymous inline layout object created during inline splitting.
/// </summary>
public class LayoutAnonymousInline : LayoutInline
{
    public LayoutAnonymousInline() : base(null)
    {
    }

    public override LayoutObjectType Type => LayoutObjectType.AnonymousInline;

    public override bool IsAnonymous => true;
}