// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/DeviceActor.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/DeviceActor.cpp

namespace LadyBird.Libraries.LibDevTools.Actors;

using System;
using System.Text.Json.Nodes;

public sealed class DeviceActor : Actor
{
    public const string BaseName = "device";

    // From C++: static NonnullRefPtr<DeviceActor> create(DevToolsServer&, String name)
    public static DeviceActor Create(DevToolsServer devtools, string name)
    {
        return new DeviceActor(devtools, name);
    }

    private DeviceActor(DevToolsServer devtools, string name)
        : base(devtools, name)
    {
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        if (message.Type == "getDescription")
        {
            var buildId = Core.Version.ReadLongVersionString();
            var browserName = Environment.GetEnvironmentVariable("BROWSER_NAME") ?? "Ladybird";
            var browserVersion = Environment.GetEnvironmentVariable("BROWSER_VERSION") ?? "0.0.0";
            var platformName = Environment.OSVersion.Platform.ToString();
            var arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";

            var value = new JsonObject
            {
                ["apptype"] = browserName.ToLowerInvariant(),
                ["name"] = browserName,
                ["brandName"] = browserName,
                ["version"] = browserVersion,
                ["appbuildid"] = buildId,
                ["platformbuildid"] = buildId,
                ["platformversion"] = "135.0",
                ["useragent"] = Web.DefaultUserAgent,
                ["os"] = platformName,
                ["arch"] = arch
            };

            var response = new JsonObject
            {
                ["value"] = value
            };

            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }
}