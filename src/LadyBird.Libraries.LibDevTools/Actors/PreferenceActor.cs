// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/PreferenceActor.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/PreferenceActor.cpp

namespace LadyBird.Libraries.LibDevTools.Actors;

using System.Text.Json.Nodes;

public sealed class PreferenceActor : Actor
{
    public const string BaseName = "preference";

    // From C++: static NonnullRefPtr<PreferenceActor> create(DevToolsServer&, String name)
    public static PreferenceActor Create(DevToolsServer devtools, string name)
    {
        return new PreferenceActor(devtools, name);
    }

    private PreferenceActor(DevToolsServer devtools, string name)
        : base(devtools, name)
    {
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        // FIXME: During session initialization, Firefox DevTools asks for the following boolean configurations:
        //            browser.privatebrowsing.autostart
        //            devtools.debugger.prompt-connection
        //            dom.serviceWorkers.enabled
        //        We just blindly return `false` for these, but we will eventually want a real configuration manager.
        if (message.Type == "getBoolPref")
        {
            var response = new JsonObject
            {
                ["value"] = false
            };
            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }
}