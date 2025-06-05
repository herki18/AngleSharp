// Copyright (c) 2025, Tim Flynn <trflynn89@ladybird.org>
// SPDX-License-Identifier: BSD-2-Clause

namespace LadyBird.Libraries.LibDevTools;

using System;
using System.Collections.Generic;
using Actors;

// DevToolsDelegate as a C# 8.0+ interface with default implementations
public interface IDevToolsDelegate
{
    // C++: virtual Vector<TabDescription> tab_list() const
    List<TabDescription> TabList() => new List<TabDescription>();

    // C++: virtual Vector<CSSProperty> css_property_list() const
    List<CSSProperty> CssPropertyList() => new List<CSSProperty>();

    // Delegates for callbacks
    delegate void OnTabInspectionComplete(ErrorOr<JsonValue> result);
    delegate void OnDOMNodePropertiesReceived(DOMNodeProperties properties);
    delegate void OnDOMMutationReceived(Mutation mutation);
    delegate void OnDOMNodeHTMLReceived(ErrorOr<string> result);
    delegate void OnDOMNodeEditComplete(ErrorOr<UniqueNodeID> result);
    delegate void OnStyleSheetsReceived(ErrorOr<List<StyleSheetIdentifier>> result);
    delegate void OnStyleSheetSourceReceived(StyleSheetIdentifier id, string source);
    delegate void OnScriptEvaluationComplete(ErrorOr<JsonValue> result);
    delegate void OnConsoleMessageAvailable(int messageId);
    delegate void OnReceivedConsoleMessages(int startIndex, List<ConsoleOutput> messages);

    // C++: virtual void inspect_tab(...) const
    void InspectTab(TabDescription tab, OnTabInspectionComplete onComplete) { }

    // C++: virtual void listen_for_dom_properties(...) const
    void ListenForDomProperties(TabDescription tab, OnDOMNodePropertiesReceived onReceived) { }

    // C++: virtual void stop_listening_for_dom_properties(...) const
    void StopListeningForDomProperties(TabDescription tab) { }

    // C++: virtual void inspect_dom_node(...) const
    void InspectDomNode(
        TabDescription tab,
        DOMNodeProperties.Type type,
        UniqueNodeID nodeId,
        PseudoElement? pseudoElement = null) { }

    // ... other methods have empty default implementations ...
    void ClearInspectedDomNode(TabDescription tab) { }
    void HighlightDomNode(TabDescription tab, UniqueNodeID nodeId, PseudoElement? pseudoElement = null) { }
    void ClearHighlightedDomNode(TabDescription tab) { }
    void ListenForDomMutations(TabDescription tab, OnDOMMutationReceived onReceived) { }
    void StopListeningForDomMutations(TabDescription tab) { }
    void GetDomNodeInnerHtml(TabDescription tab, UniqueNodeID nodeId, OnDOMNodeHTMLReceived onReceived) { }
    void GetDomNodeOuterHtml(TabDescription tab, UniqueNodeID nodeId, OnDOMNodeHTMLReceived onReceived) { }
    void SetDomNodeOuterHtml(TabDescription tab, UniqueNodeID nodeId, string html, OnDOMNodeEditComplete onComplete) { }
    void SetDomNodeText(TabDescription tab, UniqueNodeID nodeId, string text, OnDOMNodeEditComplete onComplete) { }
    void SetDomNodeTag(TabDescription tab, UniqueNodeID nodeId, string tag, OnDOMNodeEditComplete onComplete) { }
    void AddDomNodeAttributes(TabDescription tab, UniqueNodeID nodeId, IReadOnlyList<Attribute> attributes, OnDOMNodeEditComplete onComplete) { }
    void ReplaceDomNodeAttribute(TabDescription tab, UniqueNodeID nodeId, string attributeName, IReadOnlyList<Attribute> attributes, OnDOMNodeEditComplete onComplete) { }
    void CreateChildElement(TabDescription tab, UniqueNodeID nodeId, OnDOMNodeEditComplete onComplete) { }
    void InsertDomNodeBefore(TabDescription tab, UniqueNodeID nodeId, UniqueNodeID beforeNodeId, UniqueNodeID? referenceNodeId, OnDOMNodeEditComplete onComplete) { }
    void CloneDomNode(TabDescription tab, UniqueNodeID nodeId, OnDOMNodeEditComplete onComplete) { }
    void RemoveDomNode(TabDescription tab, UniqueNodeID nodeId, OnDOMNodeEditComplete onComplete) { }
    void RetrieveStyleSheets(TabDescription tab, OnStyleSheetsReceived onReceived) { }
    void RetrieveStyleSheetSource(TabDescription tab, StyleSheetIdentifier id) { }
    void ListenForStyleSheetSources(TabDescription tab, OnStyleSheetSourceReceived onReceived) { }
    void StopListeningForStyleSheetSources(TabDescription tab) { }
    void EvaluateJavascript(TabDescription tab, string script, OnScriptEvaluationComplete onComplete) { }
    void ListenForConsoleMessages(TabDescription tab, OnConsoleMessageAvailable onAvailable, OnReceivedConsoleMessages onReceived) { }
    void StopListeningForConsoleMessages(TabDescription tab) { }
    void RequestConsoleMessages(TabDescription tab, int startIndex) { }
}