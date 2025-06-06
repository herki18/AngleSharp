namespace LadyBird.Libraries.LibDevTools.Actors;

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

public sealed class InspectorActor : Actor
{
    public const string BaseName = "inspector";

    private readonly WeakReference<TabActor> _tab; // From C++ WeakPtr
    private WeakReference<WalkerActor> _walker; // From C++ WeakPtr
    private WeakReference<PageStyleActor> _pageStyle; // From C++ WeakPtr
    private readonly Dictionary<string, WeakReference<HighlighterActor>> _highlighters = new();

    // From C++: static NonnullRefPtr<InspectorActor> create(DevToolsServer&, String name, WeakPtr<TabActor>)
    public static InspectorActor Create(DevToolsServer devtools, string name, WeakReference<TabActor> tab)
    {
        return new InspectorActor(devtools, name, tab);
    }

    private InspectorActor(DevToolsServer devtools, string name, WeakReference<TabActor> tab)
        : base(devtools, name)
    {
        _tab = tab;
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "getPageStyle")
        {
            if (_pageStyle == null || !_pageStyle.TryGetTarget(out _))
            {
                var pageStyle = Devtools.RegisterActor<PageStyleActor>(new WeakReference<InspectorActor>(this));
                _pageStyle = new WeakReference<PageStyleActor>(pageStyle);
            }

            if (_pageStyle.TryGetTarget(out var pageStyleActor))
                response["pageStyle"] = pageStyleActor.SerializeStyle();

            SendResponse(message, response);
            return;
        }

        if (message.Type == "getHighlighterByType")
        {
            var typeNameResult = GetRequiredParameter<string>(message, "typeName");
            if (!typeNameResult.HasValue)
                return;

            var typeName = typeNameResult.Value;
            if (!_highlighters.TryGetValue(typeName, out var highlighterRef) ||
                !highlighterRef.TryGetTarget(out var highlighter))
            {
                highlighter = Devtools.RegisterActor<HighlighterActor>(new WeakReference<InspectorActor>(this));
                _highlighters[typeName] = new WeakReference<HighlighterActor>(highlighter);
            }

            response["highlighter"] = highlighter.SerializeHighlighter();
            SendResponse(message, response);
            return;
        }

        if (message.Type == "getWalker")
        {
            // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
            if (_tab.TryGetTarget(out var tab))
            {
                Devtools.Delegate.InspectTab(tab.Description,
                    AsyncHandler<InspectorActor, JsonValue>(message, (self, domTree, resp) =>
                    {
                        if (!WalkerActor.IsSuitableForDomInspection(domTree))
                        {
                            // C++ equivalent: dbgln_if(DEVTOOLS_DEBUG, "Did not receive a suitable DOM tree: {}", dom_tree);
                            Console.WriteLine($"Did not receive a suitable DOM tree: {domTree}");
                            return;
                        }
                        self.ReceivedDomTree(resp, domTree.AsObject());
                    }));
            }
            return;
        }

        if (message.Type == "supportsHighlighters")
        {
            response["value"] = true;
            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // From C++: void received_dom_tree(JsonObject& response, JsonObject dom_tree)
    private void ReceivedDomTree(JsonObject response, JsonObject domTree)
    {
        var walkerActor = Devtools.RegisterActor<WalkerActor>(_tab, domTree);
        _walker = new WeakReference<WalkerActor>(walkerActor);

        var walker = new JsonObject
        {
            ["actor"] = walkerActor.Name,
            ["root"] = walkerActor.SerializeRoot()
        };

        response["walker"] = walker;
    }

    // From C++: static RefPtr<TabActor> tab_for(WeakPtr<InspectorActor> const& weak_inspector)
    public static TabActor TabFor(WeakReference<InspectorActor> weakInspector)
    {
        if (weakInspector != null && weakInspector.TryGetTarget(out var inspector) &&
            inspector._tab.TryGetTarget(out var tab))
        {
            return tab;
        }
        return null;
    }

    // From C++: static RefPtr<WalkerActor> walker_for(WeakPtr<InspectorActor> const& weak_inspector)
    public static WalkerActor WalkerFor(WeakReference<InspectorActor> weakInspector)
    {
        if (weakInspector != null && weakInspector.TryGetTarget(out var inspector) &&
            inspector._walker != null && inspector._walker.TryGetTarget(out var walker))
        {
            return walker;
        }
        return null;
    }
}