namespace LadyBird.Libraries.LibDevTools.Actors;

using System.Text.Json.Nodes;

public sealed class LayoutInspectorActor : Actor
{
    public const string BaseName = "layout-inspector";

    // From C++: static NonnullRefPtr<LayoutInspectorActor> create(DevToolsServer&, String name)
    public static LayoutInspectorActor Create(DevToolsServer devtools, string name)
    {
        return new LayoutInspectorActor(devtools, name);
    }

    private LayoutInspectorActor(DevToolsServer devtools, string name)
        : base(devtools, name)
    {
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "getCurrentFlexbox")
        {
            response["flexbox"] = JsonValue.Create((object)null);
            SendResponse(message, response);
            return;
        }

        if (message.Type == "getGrids")
        {
            response["grids"] = new JsonArray();
            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }
}