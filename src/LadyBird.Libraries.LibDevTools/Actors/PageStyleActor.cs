namespace LadyBird.Libraries.LibDevTools.Actors;

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

public sealed class PageStyleActor : Actor
{
    public const string BaseName = "page-style";

    private readonly WeakReference<InspectorActor> _inspector; // From C++ WeakPtr
    private readonly List<Message> _pendingInspectRequests = new();

    // From C++: static NonnullRefPtr<PageStyleActor> create(DevToolsServer&, String name, WeakPtr<InspectorActor>)
    public static PageStyleActor Create(DevToolsServer devtools, string name, WeakReference<InspectorActor> inspector)
    {
        return new PageStyleActor(devtools, name, inspector);
    }

    private PageStyleActor(DevToolsServer devtools, string name, WeakReference<InspectorActor> inspector)
        : base(devtools, name)
    {
        _inspector = inspector;

        var tab = InspectorActor.TabFor(_inspector);
        if (tab != null)
        {
            var weakSelf = new WeakReference<PageStyleActor>(this); // Mapping from C++ make_weak_ptr
            devtools.Delegate.ListenForDomProperties(tab.Description,
                (properties) =>
                {
                    if (weakSelf.TryGetTarget(out var self))
                        self.ReceivedDomNodeProperties(properties);
                });
        }
    }

    ~PageStyleActor()
    {
        var tab = InspectorActor.TabFor(_inspector);
        if (tab != null)
            Devtools.Delegate.StopListeningForDomProperties(tab.Description);
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
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
            InspectDomNode(message, WebView.DomNodeProperties.PropertyType.ComputedStyle);
            return;
        }

        if (message.Type == "getLayout")
        {
            InspectDomNode(message, WebView.DomNodeProperties.PropertyType.Layout);
            return;
        }

        if (message.Type == "getUsedFontFaces")
        {
            InspectDomNode(message, WebView.DomNodeProperties.PropertyType.UsedFonts);
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

    // From C++: JsonValue serialize_style() const
    public JsonValue SerializeStyle()
    {
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

    // From C++: void inspect_dom_node(Message const&, WebView::DOMNodeProperties::Type)
    private void InspectDomNode(Message message, WebView.DomNodeProperties.PropertyType propertyType)
    {
        var nodeResult = GetRequiredParameter<string>(message, "node");
        if (!nodeResult.HasValue)
            return;

        var domNode = WalkerActor.DomNodeFor(InspectorActor.WalkerFor(_inspector), nodeResult.Value);
        if (domNode == null)
        {
            SendUnknownActorError(message, nodeResult.Value);
            return;
        }

        Devtools.Delegate.InspectDomNode(domNode.Tab.Description, propertyType, domNode.Identifier.Id, domNode.Identifier.PseudoElement);
        _pendingInspectRequests.Add(message);
    }

    // From C++: void received_dom_node_properties(WebView::DOMNodeProperties const&)
    private void ReceivedDomNodeProperties(WebView.DomNodeProperties properties)
    {
        if (_pendingInspectRequests.Count == 0)
            return;

        var response = new JsonObject();

        switch (properties.Type)
        {
            case WebView.DomNodeProperties.PropertyType.ComputedStyle:
                if (properties.Properties is JsonObject computedStyle)
                    ReceivedComputedStyle(response, computedStyle);
                break;

            case WebView.DomNodeProperties.PropertyType.Layout:
                if (properties.Properties is JsonObject layout)
                    ReceivedLayout(response, layout);
                break;

            case WebView.DomNodeProperties.PropertyType.UsedFonts:
                if (properties.Properties is JsonArray fonts)
                    ReceivedFonts(response, fonts);
                break;
        }

        var message = _pendingInspectRequests[0];
        _pendingInspectRequests.RemoveAt(0);
        SendResponse(message, response);
    }

    // From C++: static void received_layout(JsonObject& response, JsonObject const& node_box_sizing)
    private static void ReceivedLayout(JsonObject response, JsonObject nodeBoxSizing)
    {
        response["autoMargins"] = new JsonObject();

        double PixelValue(string key) => nodeBoxSizing.TryGetPropertyValue(key, out var value) &&
            value?.GetValue<double>() is double d ? d : 0.0;

        void SetPixelValue(string key) => response[key] = $"{PixelValue(key)}px";

        void SetComputedValue(string key) => response[key] = nodeBoxSizing.TryGetPropertyValue(key, out var value) ?
            value?.GetValue<string>() ?? string.Empty : string.Empty;

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

    // From C++: static void received_computed_style(JsonObject& response, JsonObject const& computed_style)
    private static void ReceivedComputedStyle(JsonObject response, JsonObject computedStyle)
    {
        var computed = new JsonObject();

        foreach (var member in computedStyle)
        {
            var property = new JsonObject
            {
                ["matched"] = true,
                ["value"] = member.Value
            };
            computed[member.Key] = property;
        }

        response["computed"] = computed;
    }

    // From C++: static void received_fonts(JsonObject& response, JsonArray const& fonts)
    private static void ReceivedFonts(JsonObject response, JsonArray fonts)
    {
        var fontFaces = new JsonArray();

        foreach (var font in fonts)
        {
            if (font is not JsonObject fontObj)
                continue;

            var name = fontObj.TryGetPropertyValue("name", out var nameValue) ?
                nameValue?.GetValue<string>() ?? string.Empty : string.Empty;
            var weight = fontObj.TryGetPropertyValue("weight", out var weightValue) &&
                weightValue?.GetValue<long>() is long w ? w : 0L;

            var fontFace = new JsonObject
            {
                ["CSSFamilyName"] = name,
                ["CSSGeneric"] = JsonValue.Create((object)null),
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