namespace LayoutEngine.NG.Style;

using System;
using System.Text;
using AngleSharp.Dom;

/// <summary>
/// Flags used to track what kind of style recalc or layout tree reattachment is needed.
/// These correspond to Blink's internal style recalc flags.
/// </summary>
[Flags]
public enum StyleRecalcFlag : ushort
{
    None = 0,
    RecalcSizeContainer = 1 << 0,
    RecalcDescendantSizeContainers = 1 << 1,
    RecalcStyleContainerChildren = 1 << 2,
    RecalcStyleContainerDescendants = 1 << 3,
    RecalcScrollStateContainer = 1 << 4,
    RecalcDescendantScrollStateContainers = 1 << 5,
    RecalcAnchoredContainer = 1 << 6,
    RecalcDescendantAnchoredContainers = 1 << 7,
    RecalcDescendantContentVisibility = 1 << 8,
    Reattach = 1 << 9,
    SuppressRecalc = 1 << 10,
    MarkReattach = 1 << 11,
}

/// <summary>
/// Indicates how style recalc should propagate to children and pseudo-elements.
/// </summary>
public enum StyleRecalcPropagate
{
    None,                   // No need to update style of any children.
    UpdatePseudoElements,   // Need to update existence and style for pseudo elements.
    IndependentInherit,     // Only inherited properties need to be updated.
    RecalcChildren,         // Need to recalc style for children.
    RecalcDescendants       // Need to recalc style for all descendants.
}

/// <summary>
/// Tracks the need for traversing children, recomputing computed styles,
/// and marking nodes for layout tree re-attachment during style recalc.
/// This is a direct C# translation of Blink's StyleRecalcChange.
/// </summary>
public class StyleRecalcChange
{
    // Grouped flag sets for container query logic.
    private static readonly StyleRecalcFlag RecalcSizeContainerFlags =
        StyleRecalcFlag.RecalcSizeContainer | StyleRecalcFlag.RecalcDescendantSizeContainers;
    private static readonly StyleRecalcFlag RecalcStyleContainerFlags =
        StyleRecalcFlag.RecalcStyleContainerChildren | StyleRecalcFlag.RecalcStyleContainerDescendants;
    private static readonly StyleRecalcFlag RecalcScrollStateContainerFlags =
        StyleRecalcFlag.RecalcScrollStateContainer | StyleRecalcFlag.RecalcDescendantScrollStateContainers;
    private static readonly StyleRecalcFlag RecalcAnchoredContainerFlags =
        StyleRecalcFlag.RecalcAnchoredContainer | StyleRecalcFlag.RecalcDescendantAnchoredContainers;
    private static readonly StyleRecalcFlag RecalcContainerFlags =
        RecalcSizeContainerFlags | RecalcStyleContainerFlags |
        RecalcScrollStateContainerFlags | RecalcAnchoredContainerFlags;

    private StyleRecalcPropagate _propagate;
    private StyleRecalcFlag _flags;

    /// <summary>
    /// Default constructor: no propagation, no flags.
    /// </summary>
    public StyleRecalcChange() : this(StyleRecalcPropagate.None, StyleRecalcFlag.None) { }

    /// <summary>
    /// Constructor with propagation type.
    /// </summary>
    public StyleRecalcChange(StyleRecalcPropagate propagate) : this(propagate, StyleRecalcFlag.None) { }

    /// <summary>
    /// Constructor with propagation and flags.
    /// </summary>
    public StyleRecalcChange(StyleRecalcPropagate propagate, StyleRecalcFlag flags)
    {
        _propagate = propagate;
        _flags = flags;
    }

    /// <summary>
    /// True if no propagation and no flags are set.
    /// </summary>
    public bool IsEmpty => _propagate == StyleRecalcPropagate.None && _flags == StyleRecalcFlag.None;

    /// <summary>
    /// Returns a StyleRecalcChange for children, adjusting flags as needed.
    /// </summary>
    public StyleRecalcChange ForChildren(IElement element)
    {
        // In Blink, this may adjust flags based on the element's style.
        return new StyleRecalcChange(
            RecalcDescendants() ? StyleRecalcPropagate.RecalcDescendants : StyleRecalcPropagate.None,
            FlagsForChildren(element)
        );
    }

    /// <summary>
    /// Returns a StyleRecalcChange for pseudo-elements.
    /// </summary>
    public StyleRecalcChange ForPseudoElement()
    {
        if (_propagate == StyleRecalcPropagate.UpdatePseudoElements)
            return new StyleRecalcChange(StyleRecalcPropagate.RecalcChildren, _flags);
        return this;
    }

    /// <summary>
    /// Ensures the propagation is at least the given value.
    /// </summary>
    public StyleRecalcChange EnsureAtLeast(StyleRecalcPropagate propagate)
    {
        return (propagate > _propagate)
            ? new StyleRecalcChange(propagate, _flags)
            : new StyleRecalcChange(_propagate, _flags);
    }

    /// <summary>
    /// Forces propagation to all descendants.
    /// </summary>
    public StyleRecalcChange ForceRecalcDescendants() =>
        new StyleRecalcChange(StyleRecalcPropagate.RecalcDescendants, _flags);

    /// <summary>
    /// Forces propagation to children.
    /// </summary>
    public StyleRecalcChange ForceRecalcChildren() =>
        new StyleRecalcChange(StyleRecalcPropagate.RecalcChildren, _flags);

    /// <summary>
    /// Forces layout tree reattachment.
    /// </summary>
    public StyleRecalcChange ForceReattachLayoutTree() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.Reattach);

    /// <summary>
    /// Forces marking for reattachment (for ::first-line, etc).
    /// </summary>
    public StyleRecalcChange ForceMarkReattachLayoutTree() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.MarkReattach);

    // The following methods set specific flags for container query recalc.
    public StyleRecalcChange ForceRecalcSizeContainer() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.RecalcSizeContainer);

    public StyleRecalcChange ForceRecalcDescendantSizeContainers() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.RecalcDescendantSizeContainers);

    public StyleRecalcChange ForceRecalcStyleContainerChildren() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.RecalcStyleContainerChildren);

    public StyleRecalcChange ForceRecalcStyleContainerDescendants() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.RecalcStyleContainerDescendants);

    public StyleRecalcChange ForceRecalcScrollStateContainer() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.RecalcScrollStateContainer);

    public StyleRecalcChange ForceRecalcDescendantScrollStateContainers() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.RecalcDescendantScrollStateContainers);

    public StyleRecalcChange ForceRecalcAnchoredContainer() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.RecalcAnchoredContainer);

    public StyleRecalcChange ForceRecalcDescendantAnchoredContainers() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.RecalcDescendantAnchoredContainers);

    public StyleRecalcChange ForceRecalcDescendantContentVisibility() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.RecalcDescendantContentVisibility);

    /// <summary>
    /// Suppresses recalc for the current node (used for container query roots).
    /// </summary>
    public StyleRecalcChange SuppressRecalc() =>
        new StyleRecalcChange(_propagate, _flags | StyleRecalcFlag.SuppressRecalc);

    /// <summary>
    /// Combines two StyleRecalcChange objects, taking the maximum propagation and union of flags.
    /// </summary>
    public StyleRecalcChange Combine(StyleRecalcChange other)
    {
        return new StyleRecalcChange(
            (StyleRecalcPropagate)Math.Max((int)_propagate, (int)other._propagate),
            _flags | other._flags
        );
    }

    /// <summary>
    /// True if layout tree reattachment is needed.
    /// </summary>
    public bool ReattachLayoutTree => _flags.HasFlag(StyleRecalcFlag.Reattach);

    /// <summary>
    /// True if the node should be marked for reattachment (for ::first-line, etc).
    /// </summary>
    public bool MarkReattachLayoutTree =>
        (_flags & (StyleRecalcFlag.MarkReattach | StyleRecalcFlag.Reattach | StyleRecalcFlag.SuppressRecalc)) ==
        (StyleRecalcFlag.MarkReattach | StyleRecalcFlag.Reattach);

    /// <summary>
    /// True if style recalc should propagate to children.
    /// </summary>
    public bool RecalcChildren() => _propagate > StyleRecalcPropagate.UpdatePseudoElements;

    /// <summary>
    /// True if style recalc should propagate to all descendants.
    /// </summary>
    public bool RecalcDescendants() => _propagate == StyleRecalcPropagate.RecalcDescendants;

    /// <summary>
    /// True if pseudo-elements should be updated.
    /// </summary>
    public bool UpdatePseudoElements() => _propagate != StyleRecalcPropagate.None;

    /// <summary>
    /// True if recalc is suppressed for this node.
    /// </summary>
    public bool IsSuppressed => _flags.HasFlag(StyleRecalcFlag.SuppressRecalc);

    /// <summary>
    /// True if the value of the 'rem' unit may have changed.
    /// </summary>
    public bool RemUnitsMaybeChanged => RecalcDescendants();

    /// <summary>
    /// True if container-relative units may have changed.
    /// </summary>
    public bool ContainerRelativeUnitsMaybeChanged =>
        _flags.HasFlag(StyleRecalcFlag.RecalcDescendantSizeContainers);

    // --- Main logic from Blink's .cc file ---

    /// <summary>
    /// Should we traverse children of this element for style recalc?
    /// </summary>
    public bool TraverseChildren(IElement element)
    {
        return RecalcChildren() ||
               RecalcContainerQueryDependent() ||
               element.ChildNeedsStyleRecalc() ||
               RecalcDescendantContentVisibility();
    }

    /// <summary>
    /// Should we traverse pseudo-elements of this element for style recalc?
    /// </summary>
    public bool TraversePseudoElements(IElement element)
    {
        return UpdatePseudoElements() ||
               RecalcContainerQueryDependent() ||
               element.ChildNeedsStyleRecalc() ||
               RecalcDescendantContentVisibility();
    }

    /// <summary>
    /// Should we traverse this child node for style recalc?
    /// </summary>
    public bool TraverseChild(INode node)
    {
        return ShouldRecalcStyleFor(node) ||
               node.ChildNeedsStyleRecalc() ||
               node.GetForceReattachLayoutTree() ||
               RecalcContainerQueryDependent() ||
               node.NeedsLayoutSubtreeUpdate() ||
               RecalcDescendantContentVisibility();
    }

    /// <summary>
    /// Should we recalc container query dependent styles for this node?
    /// </summary>
    public bool RecalcContainerQueryDependent(INode node)
    {
        if (!RecalcContainerQueryDependent())
            return false;
        var element = node as Element;
        if (element == null)
            return false;
        var oldStyle = element.GetComputedStyle();
        if (oldStyle == null)
            return true;
        return (RecalcSizeContainerQueryDependent() &&
                (oldStyle.DependsOnSizeContainerQueries() ||
                 oldStyle.HighlightPseudoElementStylesDependOnContainerUnits())) ||
               (RecalcStyleContainerQueryDependent() &&
                oldStyle.DependsOnStyleContainerQueries()) ||
               (RecalcScrollStateContainerQueryDependent() &&
                oldStyle.DependsOnScrollStateContainerQueries()) ||
               (RecalcAnchoredContainerQueryDependent() &&
                oldStyle.DependsOnAnchoredContainerQueries());
    }

    /// <summary>
    /// Should we recalc style for this node?
    /// </summary>
    public bool ShouldRecalcStyleFor(INode node)
    {
        if (_flags.HasFlag(StyleRecalcFlag.SuppressRecalc))
            return false;
        if (RecalcChildren())
            return true;
        if (node.NeedsStyleRecalc())
            return true;
        return RecalcContainerQueryDependent(node);
    }

    /// <summary>
    /// Should we update this pseudo-element?
    /// </summary>
    public bool ShouldUpdatePseudoElement(IPseudoElement pseudoElement)
    {
        if (UpdatePseudoElements())
            return true;
        if (pseudoElement.NeedsStyleRecalc())
            return true;
        if (pseudoElement.ChildNeedsStyleRecalc())
            return true;
        if (pseudoElement.NeedsLayoutSubtreeUpdate())
            return true;
        if (!RecalcContainerQueryDependent())
            return false;
        var style = pseudoElement.ComputedStyleRef();
        return (RecalcSizeContainerQueryDependent() &&
                style.DependsOnSizeContainerQueries()) ||
               (RecalcStyleContainerQueryDependent() &&
                style.DependsOnStyleContainerQueries()) ||
               (RecalcScrollStateContainerQueryDependent() &&
                style.DependsOnScrollStateContainerQueries()) ||
               (RecalcAnchoredContainerQueryDependent() &&
                style.DependsOnAnchoredContainerQueries());
    }

    /// <summary>
    /// Computes the flags to use for children, based on the current element and flags.
    /// </summary>
    public StyleRecalcFlag FlagsForChildren(IElement element)
    {
        if (_flags == StyleRecalcFlag.None)
            return StyleRecalcFlag.None;

        var result = _flags & ~StyleRecalcFlag.RecalcStyleContainerChildren;

        // If we're recalc'ing a size container, but the element is itself a container, don't traverse into children.
        if ((result & (RecalcSizeContainerFlags | StyleRecalcFlag.SuppressRecalc)) == StyleRecalcFlag.RecalcSizeContainer)
        {
            var oldStyle = element.GetComputedStyle();
            if (oldStyle != null && oldStyle.CanMatchSizeContainerQueries(element))
            {
                result &= ~StyleRecalcFlag.RecalcSizeContainer;
            }
        }

        // SuppressRecalc only applies to the container itself, not its children.
        // MarkReattach only survives one level past the container.
        if ((result & StyleRecalcFlag.SuppressRecalc) != 0)
        {
            result &= ~StyleRecalcFlag.SuppressRecalc;
        }
        else
        {
            result &= ~StyleRecalcFlag.MarkReattach;
        }

        return result;
    }

    /// <summary>
    /// Returns true if we can do independent inheritance for this style recalc.
    /// </summary>
    public bool IndependentInherit(ComputedStyle oldStyle)
    {
        return _propagate == StyleRecalcPropagate.IndependentInherit &&
               (!RecalcSizeContainerQueryDependent() || !oldStyle.DependsOnSizeContainerQueries()) &&
               (!RecalcStyleContainerQueryDependent() || !oldStyle.DependsOnStyleContainerQueries());
    }

    // --- Helper methods for flag checks ---

    /// <summary>
    /// True if any size container query recalc is needed.
    /// </summary>
    private bool RecalcSizeContainerQueryDependent() => (_flags & RecalcSizeContainerFlags) != 0;

    /// <summary>
    /// True if any style container query recalc is needed.
    /// </summary>
    private bool RecalcStyleContainerQueryDependent() => (_flags & RecalcStyleContainerFlags) != 0;

    /// <summary>
    /// True if any scroll state container query recalc is needed.
    /// </summary>
    private bool RecalcScrollStateContainerQueryDependent() => (_flags & RecalcScrollStateContainerFlags) != 0;

    /// <summary>
    /// True if any anchored container query recalc is needed.
    /// </summary>
    private bool RecalcAnchoredContainerQueryDependent() => (_flags & RecalcAnchoredContainerFlags) != 0;

    /// <summary>
    /// True if any container query recalc is needed.
    /// </summary>
    private bool RecalcContainerQueryDependent() => (_flags & RecalcContainerFlags) != 0;

    /// <summary>
    /// True if descendant content-visibility recalc is needed.
    /// </summary>
    private bool RecalcDescendantContentVisibility() => _flags.HasFlag(StyleRecalcFlag.RecalcDescendantContentVisibility);

    // --- ToString for debugging ---

    /// <summary>
    /// Returns a string representation of the StyleRecalcChange for debugging.
    /// </summary>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append("StyleRecalcChange{propagate=");
        builder.Append(_propagate.ToString());
        builder.Append(", flags=");
        if (_flags == StyleRecalcFlag.None)
        {
            builder.Append("kNoFlags");
        }
        else
        {
            var flags = _flags;
            string separator = "";
            void AppendFlag(StyleRecalcFlag flag, string name)
            {
                if ((flags & flag) != 0)
                {
                    builder.Append(separator);
                    builder.Append(name);
                    separator = "|";
                    flags &= ~flag;
                }
            }
            AppendFlag(StyleRecalcFlag.RecalcSizeContainer, "kRecalcSizeContainer");
            AppendFlag(StyleRecalcFlag.RecalcDescendantSizeContainers, "kRecalcDescendantSizeContainers");
            AppendFlag(StyleRecalcFlag.Reattach, "kReattach");
            AppendFlag(StyleRecalcFlag.SuppressRecalc, "kSuppressRecalc");
            // Add more as needed...
            if (flags != StyleRecalcFlag.None)
            {
                builder.Append(separator);
                builder.Append("UnknownFlag=");
                builder.Append(flags);
            }
        }
        builder.Append("}");
        return builder.ToString();
    }
}