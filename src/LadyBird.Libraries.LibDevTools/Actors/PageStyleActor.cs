// File: PageStyleActor.cs

namespace LadyBird.Libraries.LibDevTools.Actors;

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

// C# translation of C++ class PageStyleActor
public sealed class PageStyleActor : Actor
{
    public const string BaseName = "page-style";

    private readonly WeakReference<InspectorActor> _inspector; // C++ WeakPtr -> C# WeakReference
    private readonly List<Message> _pendingInspectRequests = new(1);

    // C++: static NonnullRefPtr<PageStyleActor> create(...)
    public static PageStyleActor Create(DevToolsServer devtools, string name, WeakReference<InspectorActor> inspector)
    {
        return new PageStyleActor(devtools, name, inspector);
    }

    // C++: virtual ~PageStyleActor() override;
    public override void Dispose()
    {
        // C++: if (auto tab = InspectorActor::tab_for(m_inspector)) ...
        if (_inspector.TryGetTarget(out var inspector))
        {
            var tab = InspectorActor.TabFor(_inspector);
            if (tab != null)
                Devtools().Delegate.StopListeningForDomProperties(tab.Description);
        }
        base.Dispose();
    }

    // C++: JsonValue serialize_style() const;
    public JsonNode SerializeStyle()
    {
        // C++: JsonObject traits;
        var traits = new JsonObject
        {
            ["fontStyleLevel4"] = true,
            ["fontWeightLevel4"] = true,
            ["fontStretchLevel4"] = true,
            ["fontVariations"] = true
        };

        var style = new JsonObject
        {
            ["actor"] = Name,
            ["traits"] = traits
        };
        return style;
    }

    // C++: private PageStyleActor(...)
    private PageStyleActor(DevToolsServer devtools, string name, WeakReference<InspectorActor> inspector)
        : base(devtools, name)
    {
        _inspector = inspector;
        // C++: if (auto tab = InspectorActor::tab_for(m_inspector)) ...
        var tab = InspectorActor.TabFor(_inspector);
        if (tab != null)
        {
            devtools.Delegate.ListenForDomProperties(tab.Description, properties =>
            {
                // C++: [weak_self = make_weak_ptr<PageStyleActor>()] ...
                var weakSelf = new WeakReference<PageStyleActor>(this);
                if (weakSelf.TryGetTarget(out var self))
                    self.ReceivedDomNodeProperties(properties);
            });
        }
    }

    // C++: virtual void handle_message(Message const&) override;
    public override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "getAllUsedFontFaces")
        {
            response["fontFaces"] = new JsonArray();
            SendResponse(message, response);
            return;
        }

        if (message.Type == "getApplied")
        {
            // FIXME: This provides information to the "styles" pane in the inspector tab, which allows toggling and editing
            //        styles live. We do not yet support figuring out the list of styles that apply to a specific node.
            response["entries"] = new JsonArray();
            SendResponse(message, response);
            return;
        }

        if (message.Type == "getComputed")
        {
            InspectDomNode(message, WebView.DOMNodeProperties.Type.ComputedStyle);
            return;
        }

        if (message.Type == "getLayout")
        {
            InspectDomNode(message, WebView.DOMNodeProperties.Type.Layout);
            return;
        }

        if (message.Type == "getUsedFontFaces")
        {
            InspectDomNode(message, WebView.DOMNodeProperties.Type.UsedFonts);
            return;
        }

        if (message.Type == "isPositionEditable")
        {
            response["value"] = false;
            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // C++: void inspect_dom_node(Message const&, WebView::DOMNodeProperties::Type);
    private void InspectDomNode(Message message, WebView.DOMNodeProperties.Type propertyType)
    {
        var node = GetRequiredParameter<string>(message, "node");
        if (node == null)
            return;

        var domNode = WalkerActor.DomNodeFor(InspectorActor.WalkerFor(_inspector), node);
        if (domNode == null)
        {
            SendUnknownActorError(message, node);
            return;
        }

        Devtools().Delegate.InspectDomNode(domNode.Tab.Description, propertyType, domNode.Identifier.Id, domNode.Identifier.PseudoElement);
        _pendingInspectRequests.Add(new Message { Id = message.Id });
    }

    // C++: void received_dom_node_properties(WebView::DOMNodeProperties const&);
    public void ReceivedDomNodeProperties(WebView.DOMNodeProperties properties)
    {
        if (_pendingInspectRequests.Count == 0)
            return;

        var response = new JsonObject();

        switch (properties.PropertyType)
        {
            case WebView.DOMNodeProperties.Type.ComputedStyle:
                if (properties.Properties is JsonObject computedStyle)
                    ReceivedComputedStyle(response, computedStyle);
                break;
            case WebView.DOMNodeProperties.Type.Layout:
                if (properties.Properties is JsonObject nodeBoxSizing)
                    ReceivedLayout(response, nodeBoxSizing);
                break;
            case WebView.DOMNodeProperties.Type.UsedFonts:
                if (properties.Properties is JsonArray fonts)
                    ReceivedFonts(response, fonts);
                break;
        }

        var message = _pendingInspectRequests[0];
        _pendingInspectRequests.RemoveAt(0);
        SendResponse(message, response);
    }

    // C++: static void received_layout(JsonObject& response, JsonObject const& node_box_sizing)
    private static void ReceivedLayout(JsonObject response, JsonObject nodeBoxSizing)
    {
        response["autoMargins"] = new JsonObject();

        double PixelValue(string key) =>
            nodeBoxSizing[key]?.GetValue<double>() ?? 0;

        void SetPixelValue(string key) =>
            response[key] = $"{PixelValue(key)}px";

        void SetComputedValue(string key) =>
            response[key] = nodeBoxSizing[key]?.GetValue<string>() ?? string.Empty;

        // FIXME: This response should also contain "top", "right", "bottom", and "left", but our box model metrics in
        //        WebContent do not provide this information.

        SetComputedValue("width");
        SetComputedValue("height");

        SetPixelValue("border-top-width");
        SetPixelValue("border-right-width");
        SetPixelValue("border-bottom-width");
        SetPixelValue("border-left-width");

        SetPixelValue("margin-top");
        SetPixelValue("margin-right");
        SetPixelValue("margin-bottom");
        SetPixelValue("margin-left");

        SetPixelValue("padding-top");
        SetPixelValue("padding-right");
        SetPixelValue("padding-bottom");
        SetPixelValue("padding-left");

        SetComputedValue("box-sizing");
        SetComputedValue("display");
        SetComputedValue("float");
        SetComputedValue("line-height");
        SetComputedValue("position");
        SetComputedValue("z-index");
    }

    // C++: static void received_computed_style(JsonObject& response, JsonObject const& computed_style)
    private static void ReceivedComputedStyle(JsonObject response, JsonObject computedStyle)
    {
        var computed = new JsonObject();

        foreach (var property in computedStyle)
        {
            var name = property.Key;
            var value = property.Value;
            var propertyObj = new JsonObject
            {
                ["matched"] = true,
                ["value"] = value
            };
            computed[name] = propertyObj;
        }

        response["computed"] = computed;
    }

    // C++: static void received_fonts(JsonObject& response, JsonArray const& fonts)
    private static void ReceivedFonts(JsonObject response, JsonArray fonts)
    {
        var fontFaces = new JsonArray();

        foreach (var font in fonts)
        {
            if (font is not JsonObject fontObj)
                continue;

            var name = fontObj["name"]?.GetValue<string>() ?? string.Empty;
            var weight = fontObj["weight"]?.GetValue<long>() ?? 0;

            var fontFace = new JsonObject
            {
                ["CSSFamilyName"] = name,
                ["CSSGeneric"] = new JsonObject(),
                ["format"] = "",
                ["localName"] = "",
                ["metadata"] = "",
                ["name"] = name,
                ["srcIndex"] = -1,
                ["style"] = "",
                ["URI"] = "",
                ["variationAxes"] = new JsonArray(),
                ["variationInstances"] = new JsonArray(),
                ["weight"] = weight
            };

            fontFaces.Add(fontFace);
        }

        response["fontFaces"] = fontFaces;
    }
}