namespace AngleSharp.StyleSystem.Properties
{
    using System.Collections.Generic;
    using AngleSharp.Dom;
    using AngleSharp.StyleSystem.Interfaces;

    /// <summary>
    /// Basic implementation of IStyleApplicationStrategy without any dependency on IStyleEngine.
    /// </summary>
    public class BasicStyleApplicationStrategy : IStyleApplicationStrategy
    {
        /// <summary>
        /// Determines if a subtree should be skipped during style computation.
        /// </summary>
        public bool ShouldSkipSubtree(IElement element)
        {
            if (element == null)
                return true;

            // Check for display:none in inline style
            var styleAttr = element.GetAttribute("style");
            if (styleAttr != null &&
                (styleAttr.Contains("display:none") ||
                 styleAttr.Contains("display: none")))
                return true;

            // Check for hidden attribute
            if (element.HasAttribute("hidden"))
                return true;

            return false;
        }

        /// <summary>
        /// Determines if the target element can share its style with the donor element.
        /// </summary>
        public bool CanShareStyleWith(IElement target, IElement donor)
        {
            if (target == null || donor == null)
                return false;

            // Check basic element properties that affect styling
            if (target.NodeName != donor.NodeName)
                return false;

            if (target.Id != donor.Id)
                return false;

            if (target.ClassName != donor.ClassName)
                return false;

            // Check inline styles
            bool targetHasStyle = target.HasAttribute("style");
            bool donorHasStyle = donor.HasAttribute("style");

            if (targetHasStyle != donorHasStyle)
                return false;

            if (targetHasStyle && donorHasStyle)
            {
                if (target.GetAttribute("style") != donor.GetAttribute("style"))
                    return false;
            }

            // Check other key attributes that affect styling
            string[] styleAffectingAttributes = {
                "dir", "lang", "title", "disabled", "checked", "readonly", "selected"
            };

            foreach (var attr in styleAffectingAttributes)
            {
                bool targetHasAttr = target.HasAttribute(attr);
                bool donorHasAttr = donor.HasAttribute(attr);

                if (targetHasAttr != donorHasAttr)
                    return false;

                if (targetHasAttr && donorHasAttr)
                {
                    if (target.GetAttribute(attr) != donor.GetAttribute(attr))
                        return false;
                }
            }

            // Check parent context - elements can only share styles if they have the same parent
            var targetParent = target.ParentElement;
            var donorParent = donor.ParentElement;

            if (targetParent == null || donorParent == null)
                return targetParent == donorParent;

            return targetParent.NodeName == donorParent.NodeName &&
                   targetParent.Id == donorParent.Id &&
                   targetParent.ClassName == donorParent.ClassName;
        }

        /// <summary>
        /// Gets elements in the optimal traversal order for style computation.
        /// </summary>
        public IEnumerable<IElement> GetElementTraversalOrder(IElement root)
        {
            if (root == null)
                yield break;

            // Use breadth-first traversal for style computation
            // This is optimal for style sharing and inheritance
            var queue = new Queue<IElement>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var element = queue.Dequeue();
                yield return element;

                foreach (var child in element.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }

        /// <summary>
        /// Creates a style context for the element without depending on IStyleEngine.
        /// The StyleTreeResolver will populate the parent style.
        /// </summary>
        public StyleContext CreateStyleContext(IElement element)
        {
            if (element == null)
                return new StyleContext();

            // Create context with minimal information - StyleTreeResolver will fill in the rest
            return new StyleContext
            {
                Element = element,
                ParentStyle = null, // StyleTreeResolver will set this
                IsInDocumentFlow = !IsOutOfFlow(element),
                IsVisible = !ShouldSkipSubtree(element),
                HasContent = true,
                IsContained = HasStyleContainment(element),
                ContainmentRoot = FindContainmentRoot(element)
            };
        }

        private bool IsOutOfFlow(IElement element)
        {
            if (element == null)
                return false;

            var style = element.GetAttribute("style");
            if (style == null)
                return false;

            return style.Contains("position: absolute") ||
                   style.Contains("position:absolute") ||
                   style.Contains("position: fixed") ||
                   style.Contains("position:fixed") ||
                   style.Contains("float: left") ||
                   style.Contains("float:left") ||
                   style.Contains("float: right") ||
                   style.Contains("float:right");
        }

        private bool HasStyleContainment(IElement element)
        {
            if (element == null)
                return false;

            var style = element.GetAttribute("style");
            if (style == null)
                return false;

            return style.Contains("contain: style") ||
                   style.Contains("contain:style") ||
                   style.Contains("contain: layout") ||
                   style.Contains("contain:layout") ||
                   style.Contains("contain: paint") ||
                   style.Contains("contain:paint") ||
                   style.Contains("contain: strict") ||
                   style.Contains("contain:strict") ||
                   style.Contains("contain: content") ||
                   style.Contains("contain:content");
        }

        private IElement? FindContainmentRoot(IElement element)
        {
            if (element == null)
                return null;

            var current = element.ParentElement;
            while (current != null)
            {
                if (HasStyleContainment(current))
                    return current;

                current = current.ParentElement;
            }

            return null;
        }
    }
}