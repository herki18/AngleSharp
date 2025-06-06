namespace LadyBird.Libraries.LibDevTools.Actors;

using System.Text.Json.Nodes;

public sealed class TargetConfigurationActor : Actor
{
    public const string BaseName = "target-configuration";

    // From C++: static NonnullRefPtr<TargetConfigurationActor> create(DevToolsServer&, String name)
    public static TargetConfigurationActor Create(DevToolsServer devtools, string name)
    {
        return new TargetConfigurationActor(devtools, name);
    }

    private TargetConfigurationActor(DevToolsServer devtools, string name)
        : base(devtools, name)
    {
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "updateConfiguration")
        {
            var configurationResult = GetRequiredParameter<JsonObject>(message, "configuration");
            if (!configurationResult.HasValue)
                return;

            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // From C++: JsonObject serialize_configuration() const
    public JsonObject SerializeConfiguration()
    {
        var supportedOptions = new JsonObject
        {
            ["cacheDisabled"] = false,
            ["colorSchemeSimulation"] = false,
            ["customFormatters"] = false,
            ["customUserAgent"] = false,
            ["javascriptEnabled"] = false,
            ["overrideDPPX"] = false,
            ["printSimulationEnabled"] = false,
            ["rdmPaneMaxTouchPoints"] = false,
            ["rdmPaneOrientation"] = false,
            ["recordAllocations"] = false,
            ["reloadOnTouchSimulationToggle"] = false,
            ["restoreFocus"] = false,
            ["serviceWorkersTestingEnabled"] = false,
            ["setTabOffline"] = false,
            ["touchEventsOverride"] = false,
            ["tracerOptions"] = false,
            ["useSimpleHighlightersForReducedMotion"] = false
        };

        var traits = new JsonObject
        {
            ["supportedOptions"] = supportedOptions
        };

        var target = new JsonObject
        {
            ["actor"] = Name,
            ["configuration"] = new JsonObject(),
            ["traits"] = traits
        };

        return target;
    }
}