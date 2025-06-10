// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/ProcessActor.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/ProcessActor.cpp

namespace LadyBird.Libraries.LibDevTools.Actors;

using System.Text.Json.Nodes;

public struct ProcessDescription
{
    public ulong Id { get; set; }
    public bool IsParent { get; set; }
    public bool IsWindowlessParent { get; set; }
}

public sealed class ProcessActor : Actor
{
    public const string BaseName = "process";
    private readonly ProcessDescription _description;

    // From C++: static NonnullRefPtr<ProcessActor> create(DevToolsServer&, String name, ProcessDescription)
    public static ProcessActor Create(DevToolsServer devtools, string name, ProcessDescription description)
    {
        return new ProcessActor(devtools, name, description);
    }

    private ProcessActor(DevToolsServer devtools, string name, ProcessDescription description)
        : base(devtools, name)
    {
        _description = description;
    }

    public ProcessDescription Description => _description;

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        SendUnrecognizedPacketTypeError(message);
    }

    // From C++: JsonObject serialize_description() const
    public JsonObject SerializeDescription()
    {
        var traits = new JsonObject
        {
            ["watcher"] = _description.IsParent,
            ["supportsReloadDescriptor"] = true
        };

        var description = new JsonObject
        {
            ["actor"] = Name,
            ["id"] = _description.Id,
            ["isParent"] = _description.IsParent,
            ["isWindowlessParent"] = _description.IsWindowlessParent,
            ["traits"] = traits
        };

        return description;
    }
}