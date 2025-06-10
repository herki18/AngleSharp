// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/RootActor.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/RootActor.cpp

namespace LadyBird.Libraries.LibDevTools.Actors;

using System.Text.Json.Nodes;

public sealed class RootActor : Actor
{
    public const string BaseName = "root";

    // https://firefox-source-docs.mozilla.org/devtools/backend/protocol.html#the-request-reply-notify-pattern
    // the root actor sends at most one "tabListChanged" notification after each "listTabs" request.
    private bool _hasSentTabListChangedSinceLastListTabsRequest = false;

    // From C++: static NonnullRefPtr<RootActor> create(DevToolsServer&, String name)
    public static RootActor Create(DevToolsServer devtools, string name)
    {
        var actor = new RootActor(devtools, name);

        var traits = new JsonObject
        {
            ["sources"] = false,
            ["highlightable"] = true,
            ["customHighlighters"] = true,
            ["networkMonitor"] = false
        };

        var message = new JsonObject
        {
            ["applicationType"] = "browser",
            ["traits"] = traits
        };

        actor.SendMessage(message);
        return actor;
    }

    private RootActor(DevToolsServer devtools, string name)
        : base(devtools, name)
    {
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "connect")
        {
            SendResponse(message, response);
            return;
        }

        if (message.Type == "getRoot")
        {
            response["selected"] = 0;

            foreach (var actor in Devtools.ActorRegistry)
            {
                if (actor.Value is DeviceActor)
                    response["deviceActor"] = actor.Key;
                else if (actor.Value is PreferenceActor)
                    response["preferenceActor"] = actor.Key;
            }

            SendResponse(message, response);
            return;
        }

        if (message.Type == "getProcess")
        {
            var idResult = GetRequiredParameter<ulong>(message, "id");
            if (!idResult.HasValue)
                return;

            foreach (var actor in Devtools.ActorRegistry)
            {
                if (actor.Value is ProcessActor processActor && processActor.Description.Id == idResult.Value)
                {
                    response["processDescriptor"] = processActor.SerializeDescription();
                    break;
                }
            }

            SendResponse(message, response);
            return;
        }

        if (message.Type == "getTab")
        {
            var browserIdResult = GetRequiredParameter<ulong>(message, "browserId");
            if (!browserIdResult.HasValue)
                return;

            foreach (var actor in Devtools.ActorRegistry)
            {
                if (actor.Value is TabActor tabActor && tabActor.Description.Id == browserIdResult.Value)
                {
                    response["tab"] = tabActor.SerializeDescription();
                    break;
                }
            }

            SendResponse(message, response);
            return;
        }

        if (message.Type == "listAddons")
        {
            response["addons"] = new JsonArray();
            SendResponse(message, response);
            return;
        }

        if (message.Type == "listProcesses")
        {
            var processes = new JsonArray();

            foreach (var actor in Devtools.ActorRegistry)
            {
                if (actor.Value is ProcessActor processActor)
                    processes.Add(processActor.SerializeDescription());
            }

            response["processes"] = processes;
            SendResponse(message, response);
            return;
        }

        if (message.Type == "listServiceWorkerRegistrations")
        {
            response["registrations"] = new JsonArray();
            SendResponse(message, response);
            return;
        }

        if (message.Type == "listTabs")
        {
            _hasSentTabListChangedSinceLastListTabsRequest = false;
            var tabs = new JsonArray();

            foreach (var tabDescription in Devtools.Delegate.TabList())
            {
                var actor = Devtools.RegisterActor<TabActor>(tabDescription);
                tabs.Add(actor.SerializeDescription());
            }

            response["tabs"] = tabs;
            SendResponse(message, response);
            return;
        }

        if (message.Type == "listWorkers")
        {
            response["workers"] = new JsonArray();
            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // From C++: void send_tab_list_changed_message()
    public void SendTabListChangedMessage()
    {
        if (_hasSentTabListChangedSinceLastListTabsRequest)
            return;

        var message = new JsonObject
        {
            ["type"] = "tabListChanged"
        };

        SendMessage(message);
        _hasSentTabListChangedSinceLastListTabsRequest = true;
    }
}