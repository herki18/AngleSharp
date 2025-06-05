// Main implementation of WatcherActor, converted from C++ to C#
// File-scoped namespace and C# naming conventions are used

using System;
using System.Collections.Generic;

namespace DevTools;

using System.Text.Json.Nodes;
using LadyBird.Libraries.LibDevTools;
using LadyBird.Libraries.LibDevTools.Actors;

public sealed class WatcherActor : Actor
{
    public static readonly string BaseName = "watcher";

    // Mapping C++ WeakPtr<TabActor> to C# WeakReference<TabActor>
    private readonly WeakReference<TabActor> _tab;
    // Mapping C++ WeakPtr<Actor> to C# WeakReference<Actor>
    private WeakReference<Actor>? _target;
    // Mapping C++ WeakPtr<TargetConfigurationActor> to C# WeakReference<TargetConfigurationActor>
    private WeakReference<TargetConfigurationActor>? _targetConfiguration;
    // Mapping C++ WeakPtr<ThreadConfigurationActor> to C# WeakReference<ThreadConfigurationActor>
    private WeakReference<ThreadConfigurationActor>? _threadConfiguration;

    // C++: static NonnullRefPtr<WatcherActor> create(...)
    public static WatcherActor Create(DevToolsServer devtools, string name, WeakReference<TabActor> tab)
        => new(devtools, name, tab);

    // C++: WatcherActor(DevToolsServer&, String name, WeakPtr<TabActor>)
    private WatcherActor(DevToolsServer devtools, string name, WeakReference<TabActor> tab)
        : base(devtools, name)
    {
        _tab = tab;
    }

    // C++: virtual ~WatcherActor() override;
    // In C#, no need for explicit destructor unless unmanaged resources are used.

    // C++: void handle_message(Message const& message)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "getParentBrowsingContextID")
        {
            var browsingContextId = GetRequiredParameter<ulong>(message, "browsingContextID");
            if (!browsingContextId.HasValue)
                return;

            response["browsingContextID"] = browsingContextId.Value;
            SendResponse(message, response);
            return;
        }

        if (message.Type == "getTargetConfigurationActor")
        {
            if (_targetConfiguration == null || !_targetConfiguration.TryGetTarget(out var targetConfig))
            {
                targetConfig = Devtools.RegisterActor<TargetConfigurationActor>();
                _targetConfiguration = new WeakReference<TargetConfigurationActor>(targetConfig);
            }

            response["configuration"] = targetConfig.SerializeConfiguration();
            SendResponse(message, response);
            return;
        }

        if (message.Type == "getThreadConfigurationActor")
        {
            if (_threadConfiguration == null || !_threadConfiguration.TryGetTarget(out var threadConfig))
            {
                threadConfig = Devtools.RegisterActor<ThreadConfigurationActor>();
                _threadConfiguration = new WeakReference<ThreadConfigurationActor>(threadConfig);
            }

            response["configuration"] = threadConfig.SerializeConfiguration();
            SendResponse(message, response);
            return;
        }

        if (message.Type == "watchResources")
        {
            var resourceTypes = GetRequiredParameter<JsonArray>(message, "resourceTypes");
            if (!resourceTypes.HasValue)
                return;

#if DEVTOOLS_DEBUG
            foreach (var resourceType in resourceTypes.Value.Values)
            {
                if (resourceType is not string resourceTypeStr)
                    continue;
                if (resourceTypeStr != "console-message")
                    DebugLogger.DbgLn($"Unrecognized `watchResources` resource type: '{resourceTypeStr}'");
            }
#endif

            SendResponse(message, response);
            return;
        }

        if (message.Type == "watchTargets")
        {
            var targetType = GetRequiredParameter<string>(message, "targetType");
            if (!targetType.HasValue)
                return;

            if (targetType.Value == "frame")
            {
                var cssProperties = Devtools.RegisterActor<CSSPropertiesActor>();
                var console = Devtools.RegisterActor<ConsoleActor>(_tab);
                var inspector = Devtools.RegisterActor<InspectorActor>(_tab);
                var styleSheets = Devtools.RegisterActor<StyleSheetsActor>(_tab);
                var thread = Devtools.RegisterActor<ThreadActor>();

                var target = Devtools.RegisterActor<FrameActor>(_tab, cssProperties, console, inspector, styleSheets, thread);
                _target = new WeakReference<Actor>(target);

                response["type"] = "target-available-form";
                response["target"] = target.SerializeTarget();
                SendResponse(message, response);

                target.SendFrameUpdateMessage();

                SendMessage(new Message()); // Empty message as in C++
                return;
            }
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // C++: JsonObject serialize_description() const
    public JsonObject SerializeDescription()
    {
        var resources = new JsonObject
        {
            ["Cache"] = false,
            ["console-message"] = true,
            ["cookies"] = false,
            ["css-change"] = false,
            ["css-message"] = false,
            ["css-registered-properties"] = false,
            ["document-event"] = false,
            ["error-message"] = false,
            ["extension-storage"] = false,
            ["indexed-db"] = false,
            ["jstracer-state"] = false,
            ["jstracer-trace"] = false,
            ["last-private-context-exit"] = false,
            ["local-storage"] = false,
            ["network-event"] = false,
            ["network-event-stacktrace"] = false,
            ["platform-message"] = false,
            ["reflow"] = false,
            ["server-sent-event"] = false,
            ["session-storage"] = false,
            ["source"] = false,
            ["stylesheet"] = false,
            ["thread-state"] = false,
            ["websocket"] = false
        };

        var description = new JsonObject
        {
            ["shared_worker"] = false,
            ["service_worker"] = false,
            ["frame"] = true,
            ["process"] = false,
            ["worker"] = false,
            ["resources"] = resources
        };

        return description;
    }
}