// Main namespace and classes translated from C++
namespace LadyBird.Libraries.LibDevTools.Actors;

using System.Text.Json.Nodes;

// C++: struct TabDescription
public struct TabDescription
{
    public ulong Id; // u64
    public string Title;
    public string Url;
}

// C++: class TabActor final : public Actor
public sealed class TabActor : Actor
{
    // C++: static constexpr auto base_name = "tab"sv;
    public static readonly string BaseName = "tab";

    // C++: static NonnullRefPtr<TabActor> create(DevToolsServer&, String name, TabDescription);
    public static TabActor Create(DevToolsServer devtools, string name, TabDescription description)
    {
        // C++: return adopt_ref(*new TabActor(devtools, move(name), move(description)));
        return new TabActor(devtools, name, description);
    }

    // C++: virtual ~TabActor() override;
    ~TabActor()
    {
        // C++: reset_selected_node();
        ResetSelectedNode();
    }

    // C++: TabDescription const& description() const { return m_description; }
    public TabDescription Description => _description;

    // C++: JsonObject serialize_description() const;
    public JsonObject SerializeDescription()
    {
        // C++ logic
        var traits = new JsonObject();
        traits["watcher"] = true;
        traits["supportsReloadDescriptor"] = true;

        // FIXME: We are using the tab's ID multiple times here. This is likely not correct, as both Firefox and Servo
        //        provide different IDs for browserId, browsingContextID, and outerWindowID.
        var description = new JsonObject();
        description["actor"] = Name;
        description["title"] = _description.Title;
        description["url"] = _description.Url;
        description["browserId"] = _description.Id;
        description["browsingContextID"] = _description.Id;
        description["outerWindowID"] = _description.Id;
        description["traits"] = traits;
        return description;
    }

    // C++: void reset_selected_node();
    public void ResetSelectedNode()
    {
        // C++: devtools().delegate().clear_highlighted_dom_node(description());
        // C++: devtools().delegate().clear_inspected_dom_node(description());
        Devtools().Delegate().ClearHighlightedDomNode(Description);
        Devtools().Delegate().ClearInspectedDomNode(Description);
    }

    // C++: private: TabActor(DevToolsServer&, String name, TabDescription);
    private TabActor(DevToolsServer devtools, string name, TabDescription description)
        : base(devtools, name)
    {
        _description = description;
    }

    // C++: virtual void handle_message(Message const&) override;
    protected override void HandleMessage(Message message)
    {
        // C++: JsonObject response;
        var response = new JsonObject();

        // C++: if (message.type == "getFavicon"sv)
        if (message.Type == "getFavicon")
        {
            // FIXME: Firefox DevTools wants a favicon URL here, but supplying a URL seems to prevent this tab from being
            //        listed on the about:debugging page. Both Servo and Firefox itself supply `null` here.
            response["favicon"] = new JsonValue();
            SendResponse(message, response);
            return;
        }

        // C++: if (message.type == "getWatcher"sv)
        if (message.Type == "getWatcher")
        {
            if (_watcher == null)
                _watcher = Devtools().RegisterActor<WatcherActor>(this);

            response["actor"] = _watcher.Name();
            response["traits"] = _watcher.SerializeDescription();
            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // C++: TabDescription m_description;
    private TabDescription _description;

    // C++: WeakPtr<WatcherActor> m_watcher;
    private WatcherActor _watcher;
}