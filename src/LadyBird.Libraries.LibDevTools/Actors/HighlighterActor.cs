namespace LadyBird.Libraries.LibDevTools.Actors;

using System;
using System.Text.Json.Nodes;
using LadyBird.Libraries.LibDevTools;

public sealed class HighlighterActor : Actor
{
    public const string BaseName = "highlighter";

    private readonly WeakReference<InspectorActor> _inspector; // From C++ WeakPtr

    // From C++: static NonnullRefPtr<HighlighterActor> create(DevToolsServer&, String name, WeakPtr<InspectorActor>)
    public static HighlighterActor Create(DevToolsServer devtools, string name, WeakReference<InspectorActor> inspector)
    {
        return new HighlighterActor(devtools, name, inspector);
    }

    private HighlighterActor(DevToolsServer devtools, string name, WeakReference<InspectorActor> inspector)
        : base(devtools, name)
    {
        _inspector = inspector;
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "show")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            response["value"] = false;

            var domNode = WalkerActor.DomNodeFor(InspectorActor.WalkerFor(_inspector), nodeResult.Value);
            if (domNode != null)
            {
                Devtools.Delegate.HighlightDomNode(domNode.Tab.Description, domNode.Identifier.Id, domNode.Identifier.PseudoElement);
                response["value"] = true;
            }

            SendResponse(message, response);
            return;
        }

        if (message.Type == "hide")
        {
            var tab = InspectorActor.TabFor(_inspector);
            if (tab != null)
                Devtools.Delegate.ClearHighlightedDomNode(tab.Description);

            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // From C++: JsonValue serialize_highlighter() const
    public JsonValue SerializeHighlighter()
    {
        var highlighter = new JsonObject
        {
            ["actor"] = Name
        };
        return highlighter;
    }
}