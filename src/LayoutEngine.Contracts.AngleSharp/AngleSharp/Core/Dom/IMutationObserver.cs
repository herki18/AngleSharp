namespace AngleSharp.Dom;

using AngleSharp.Attributes;
using System;
using System.Collections.Generic;

/// <summary>
/// Interface for MutationObserver that provides developers a way to react to changes in a DOM.
/// </summary>
[DomName("MutationObserver")]
public interface IMutationObserver
{
    /// <summary>
    /// Stops the MutationObserver instance from receiving
    /// notifications of DOM mutations. Until the observe()
    /// method is used again, observer's callback will not be invoked.
    /// </summary>
    [DomName("disconnect")]
    void Disconnect();

    /// <summary>
    /// Registers the MutationObserver instance to receive notifications of
    /// DOM mutations on the specified node.
    /// </summary>
    /// <param name="target">
    /// The Node on which to observe DOM mutations.
    /// </param>
    /// <param name="childList">
    /// If additions and removals of the target node's child elements
    /// (including text nodes) are to be observed.
    /// </param>
    /// <param name="subtree">
    /// If mutations to not just target, but also target's descendants are
    /// to be observed.
    /// </param>
    /// <param name="attributes">
    /// If mutations to target's attributes are to be observed.
    /// </param>
    /// <param name="characterData">
    /// If mutations to target's data are to be observed.
    /// </param>
    /// <param name="attributeOldValue">
    /// If attributes is set to true and target's attribute value before
    /// the mutation needs to be recorded.
    /// </param>
    /// <param name="characterDataOldValue">
    /// If characterData is set to true and target's data before the
    /// mutation needs to be recorded.
    /// </param>
    /// <param name="attributeFilter">
    /// The attributes to observe. If this is not set, then all attributes
    /// are being observed.
    /// </param>
    [DomName("observe")]
    [DomInitDict(offset: 1)]
    void Connect(INode target, Boolean childList = false, Boolean subtree = false, Boolean? attributes = null, Boolean? characterData = null, Boolean? attributeOldValue = null, Boolean? characterDataOldValue = null, IEnumerable<String>? attributeFilter = null);

    /// <summary>
    /// Empties the MutationObserver instance's record queue and returns
    /// what was in there.
    /// </summary>
    /// <returns>Returns an Array of MutationRecords.</returns>
    [DomName("takeRecords")]
    IEnumerable<IMutationRecord> Flush();
}