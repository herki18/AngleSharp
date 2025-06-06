namespace LadyBird.Libraries.LibDevTools;

using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Actors;

public abstract class DevToolsDelegate
{
    // From C++: virtual Vector<TabDescription> tab_list() const
    public virtual List<TabDescription> TabList() => new();

    // From C++: virtual Vector<CSSProperty> css_property_list() const
    public virtual List<CssProperty> CssPropertyList() => new();

    // From C++: using OnTabInspectionComplete = Function<void(ErrorOr<JsonValue>)>
    public delegate Task OnTabInspectionComplete(Result<JsonValue> result);

    // From C++: virtual void inspect_tab(TabDescription const&, OnTabInspectionComplete) const
    public virtual void InspectTab(TabDescription tab, OnTabInspectionComplete onComplete) { }

    // From C++: using OnDOMNodePropertiesReceived = Function<void(WebView::DOMNodeProperties)>
    public delegate void OnDomNodePropertiesReceived(WebView.DomNodeProperties properties);

    // From C++: virtual void listen_for_dom_properties(TabDescription const&, OnDOMNodePropertiesReceived) const
    public virtual void ListenForDomProperties(TabDescription tab, OnDomNodePropertiesReceived onReceived) { }

    // From C++: virtual void stop_listening_for_dom_properties(TabDescription const&) const
    public virtual void StopListeningForDomProperties(TabDescription tab) { }

    // From C++: virtual void inspect_dom_node(TabDescription const&, WebView::DOMNodeProperties::Type, Web::UniqueNodeID, Optional<Web::CSS::PseudoElement>) const
    public virtual void InspectDomNode(TabDescription tab, WebView.DomNodeProperties.PropertyType type, Web.UniqueNodeId nodeId, Web.Css.PseudoElement? pseudoElement) { }

    // From C++: virtual void clear_inspected_dom_node(TabDescription const&) const
    public virtual void ClearInspectedDomNode(TabDescription tab) { }

    // From C++: virtual void highlight_dom_node(TabDescription const&, Web::UniqueNodeID, Optional<Web::CSS::PseudoElement>) const
    public virtual void HighlightDomNode(TabDescription tab, Web.UniqueNodeId nodeId, Web.Css.PseudoElement? pseudoElement) { }

    // From C++: virtual void clear_highlighted_dom_node(TabDescription const&) const
    public virtual void ClearHighlightedDomNode(TabDescription tab) { }

    // From C++: using OnDOMMutationReceived = Function<void(WebView::Mutation)>
    public delegate void OnDomMutationReceived(WebView.Mutation mutation);

    // From C++: virtual void listen_for_dom_mutations(TabDescription const&, OnDOMMutationReceived) const
    public virtual void ListenForDomMutations(TabDescription tab, OnDomMutationReceived onReceived) { }

    // From C++: virtual void stop_listening_for_dom_mutations(TabDescription const&) const
    public virtual void StopListeningForDomMutations(TabDescription tab) { }

    // From C++: using OnDOMNodeHTMLReceived = Function<void(ErrorOr<String>)>
    public delegate Task OnDomNodeHtmlReceived(Result<string> result);

    // From C++: using OnDOMNodeEditComplete = Function<void(ErrorOr<Web::UniqueNodeID>)>
    public delegate Task OnDomNodeEditComplete(Result<Web.UniqueNodeId> result);

    // From C++: virtual void get_dom_node_inner_html(TabDescription const&, Web::UniqueNodeID, OnDOMNodeHTMLReceived) const
    public virtual void GetDomNodeInnerHtml(TabDescription tab, Web.UniqueNodeId nodeId, OnDomNodeHtmlReceived onReceived) { }

    // From C++: virtual void get_dom_node_outer_html(TabDescription const&, Web::UniqueNodeID, OnDOMNodeHTMLReceived) const
    public virtual void GetDomNodeOuterHtml(TabDescription tab, Web.UniqueNodeId nodeId, OnDomNodeHtmlReceived onReceived) { }

    // From C++: virtual void set_dom_node_outer_html(TabDescription const&, Web::UniqueNodeID, String const&, OnDOMNodeEditComplete) const
    public virtual void SetDomNodeOuterHtml(TabDescription tab, Web.UniqueNodeId nodeId, string html, OnDomNodeEditComplete onComplete) { }

    // From C++: virtual void set_dom_node_text(TabDescription const&, Web::UniqueNodeID, String const&, OnDOMNodeEditComplete) const
    public virtual void SetDomNodeText(TabDescription tab, Web.UniqueNodeId nodeId, string text, OnDomNodeEditComplete onComplete) { }

    // From C++: virtual void set_dom_node_tag(TabDescription const&, Web::UniqueNodeID, String const&, OnDOMNodeEditComplete) const
    public virtual void SetDomNodeTag(TabDescription tab, Web.UniqueNodeId nodeId, string tagName, OnDomNodeEditComplete onComplete) { }

    // From C++: virtual void add_dom_node_attributes(TabDescription const&, Web::UniqueNodeID, ReadonlySpan<WebView::Attribute>, OnDOMNodeEditComplete) const
    public virtual void AddDomNodeAttributes(TabDescription tab, Web.UniqueNodeId nodeId, IReadOnlyList<WebView.Attribute> attributes, OnDomNodeEditComplete onComplete) { }

    // From C++: virtual void replace_dom_node_attribute(TabDescription const&, Web::UniqueNodeID, String const&, ReadonlySpan<WebView::Attribute>, OnDOMNodeEditComplete) const
    public virtual void ReplaceDomNodeAttribute(TabDescription tab, Web.UniqueNodeId nodeId, string attributeToReplace, IReadOnlyList<WebView.Attribute> replacementAttributes, OnDomNodeEditComplete onComplete) { }

    // From C++: virtual void create_child_element(TabDescription const&, Web::UniqueNodeID, OnDOMNodeEditComplete) const
    public virtual void CreateChildElement(TabDescription tab, Web.UniqueNodeId parentId, OnDomNodeEditComplete onComplete) { }

    // From C++: virtual void insert_dom_node_before(TabDescription const&, Web::UniqueNodeID, Web::UniqueNodeID, Optional<Web::UniqueNodeID>, OnDOMNodeEditComplete) const
    public virtual void InsertDomNodeBefore(TabDescription tab, Web.UniqueNodeId nodeId, Web.UniqueNodeId parentId, Web.UniqueNodeId? siblingId, OnDomNodeEditComplete onComplete) { }

    // From C++: virtual void clone_dom_node(TabDescription const&, Web::UniqueNodeID, OnDOMNodeEditComplete) const
    public virtual void CloneDomNode(TabDescription tab, Web.UniqueNodeId nodeId, OnDomNodeEditComplete onComplete) { }

    // From C++: virtual void remove_dom_node(TabDescription const&, Web::UniqueNodeID, OnDOMNodeEditComplete) const
    public virtual void RemoveDomNode(TabDescription tab, Web.UniqueNodeId nodeId, OnDomNodeEditComplete onComplete) { }

    // From C++: using OnStyleSheetsReceived = Function<void(ErrorOr<Vector<Web::CSS::StyleSheetIdentifier>>)>
    public delegate Task OnStyleSheetsReceived(Result<List<Web.Css.StyleSheetIdentifier>> result);

    // From C++: using OnStyleSheetSourceReceived = Function<void(Web::CSS::StyleSheetIdentifier const&, String)>
    public delegate void OnStyleSheetSourceReceived(Web.Css.StyleSheetIdentifier styleSheet, string source);

    // From C++: virtual void retrieve_style_sheets(TabDescription const&, OnStyleSheetsReceived) const
    public virtual void RetrieveStyleSheets(TabDescription tab, OnStyleSheetsReceived onReceived) { }

    // From C++: virtual void retrieve_style_sheet_source(TabDescription const&, Web::CSS::StyleSheetIdentifier const&) const
    public virtual void RetrieveStyleSheetSource(TabDescription tab, Web.Css.StyleSheetIdentifier styleSheet) { }

    // From C++: virtual void listen_for_style_sheet_sources(TabDescription const&, OnStyleSheetSourceReceived) const
    public virtual void ListenForStyleSheetSources(TabDescription tab, OnStyleSheetSourceReceived onReceived) { }

    // From C++: virtual void stop_listening_for_style_sheet_sources(TabDescription const&) const
    public virtual void StopListeningForStyleSheetSources(TabDescription tab) { }

    // From C++: using OnScriptEvaluationComplete = Function<void(ErrorOr<JsonValue>)>
    public delegate Task OnScriptEvaluationComplete(Result<JsonValue> result);

    // From C++: virtual void evaluate_javascript(TabDescription const&, String const&, OnScriptEvaluationComplete) const
    public virtual void EvaluateJavaScript(TabDescription tab, string script, OnScriptEvaluationComplete onComplete) { }

    // From C++: using OnConsoleMessageAvailable = Function<void(i32 message_id)>
    public delegate void OnConsoleMessageAvailable(int messageId);

    // From C++: using OnReceivedConsoleMessages = Function<void(i32 start_index, Vector<WebView::ConsoleOutput>)>
    public delegate void OnReceivedConsoleMessages(int startIndex, List<WebView.ConsoleOutput> messages);

    // From C++: virtual void listen_for_console_messages(TabDescription const&, OnConsoleMessageAvailable, OnReceivedConsoleMessages) const
    public virtual void ListenForConsoleMessages(TabDescription tab, OnConsoleMessageAvailable onAvailable, OnReceivedConsoleMessages onReceived) { }

    // From C++: virtual void stop_listening_for_console_messages(TabDescription const&) const
    public virtual void StopListeningForConsoleMessages(TabDescription tab) { }

    // From C++: virtual void request_console_messages(TabDescription const&, i32) const
    public virtual void RequestConsoleMessages(TabDescription tab, int startIndex) { }
}