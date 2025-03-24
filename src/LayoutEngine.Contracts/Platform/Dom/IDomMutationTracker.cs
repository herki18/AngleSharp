// using AngleSharp.Dom;
// using System;
//
// namespace LayoutEngine.Contracts.Platform.Dom;
//
// /// <summary>
// /// Interface for tracking DOM mutations.
// /// </summary>
// public interface IDomMutationTracker
// {
//     /// <summary>
//     /// Starts tracking mutations in the specified document.
//     /// </summary>
//     /// <param name="document">The document to track.</param>
//     void StartTracking(IDocument document);
//
//     /// <summary>
//     /// Stops tracking mutations.
//     /// </summary>
//     void StopTracking();
//
//     /// <summary>
//     /// Signals that an attribute has changed on an element.
//     /// </summary>
//     /// <param name="element">The element whose attribute changed.</param>
//     /// <param name="attributeName">The name of the attribute that changed.</param>
//     /// <param name="oldValue">The old value of the attribute.</param>
//     /// <param name="newValue">The new value of the attribute.</param>
//     void SignalAttributeChanged(IElement element, string attributeName, string? oldValue, string? newValue);
//
//     /// <summary>
//     /// Signals that a node has been added to the document.
//     /// </summary>
//     /// <param name="node">The node that was added.</param>
//     /// <param name="parent">The parent node that the node was added to.</param>
//     void SignalNodeAdded(INode node, INode parent);
//
//     /// <summary>
//     /// Signals that a node has been removed from the document.
//     /// </summary>
//     /// <param name="node">The node that was removed.</param>
//     /// <param name="parent">The parent node that the node was removed from.</param>
//     void SignalNodeRemoved(INode node, INode parent);
// }