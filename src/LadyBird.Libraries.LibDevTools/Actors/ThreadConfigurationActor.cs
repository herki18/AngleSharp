// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/ThreadConfigurationActor.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/ThreadConfigurationActor.cpp

namespace LadyBird.Libraries.LibDevTools.Actors;

using System.Text.Json.Nodes;

public sealed class ThreadConfigurationActor : Actor
{
    public const string BaseName = "thread-configuration";

    // From C++: static NonnullRefPtr<ThreadConfigurationActor> create(DevToolsServer&, String name)
    public static ThreadConfigurationActor Create(DevToolsServer devtools, string name)
    {
        return new ThreadConfigurationActor(devtools, name);
    }

    private ThreadConfigurationActor(DevToolsServer devtools, string name)
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
        var target = new JsonObject
        {
            ["actor"] = Name
        };

        return target;
    }
}