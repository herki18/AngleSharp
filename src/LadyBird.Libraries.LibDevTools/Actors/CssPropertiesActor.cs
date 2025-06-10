// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/CSSPropertiesActor.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/CSSPropertiesActor.cpp

namespace LadyBird.Libraries.LibDevTools.Actors;

using System.Text.Json.Nodes;

public struct CssProperty
{
    public string Name { get; set; }
    public bool IsInherited { get; set; }
}

public sealed class CssPropertiesActor : Actor
{
    public const string BaseName = "css-properties";

    // From C++: static NonnullRefPtr<CSSPropertiesActor> create(DevToolsServer&, String name)
    public static CssPropertiesActor Create(DevToolsServer devtools, string name)
    {
        return new CssPropertiesActor(devtools, name);
    }

    private CssPropertiesActor(DevToolsServer devtools, string name)
        : base(devtools, name)
    {
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "getCSSDatabase")
        {
            var cssPropertyList = Devtools.Delegate.CssPropertyList();
            var properties = new JsonObject();

            foreach (var cssProperty in cssPropertyList)
            {
                var subproperties = new JsonArray();
                subproperties.Add(cssProperty.Name);

                var property = new JsonObject
                {
                    ["isInherited"] = cssProperty.IsInherited,
                    ["supports"] = new JsonArray(),
                    ["values"] = new JsonArray(),
                    ["subproperties"] = subproperties
                };

                properties[cssProperty.Name] = property;
            }

            response["properties"] = properties;
            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }
}