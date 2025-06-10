// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/StyleSheetsActor.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/StyleSheetsActor.cpp

namespace LadyBird.Libraries.LibDevTools.Actors;

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

public sealed class StyleSheetsActor : Actor
{
    public const string BaseName = "style-sheets";
    private readonly WeakReference<TabActor> _tab; // From C++ WeakPtr
    private List<Web.Css.StyleSheetIdentifier> _styleSheets = new();
    private readonly Dictionary<int, Message> _pendingStyleSheetSourceRequests = new();

    // From C++: static NonnullRefPtr<StyleSheetsActor> create(DevToolsServer&, String name, WeakPtr<TabActor>)
    public static StyleSheetsActor Create(DevToolsServer devtools, string name, WeakReference<TabActor> tab)
    {
        return new StyleSheetsActor(devtools, name, tab);
    }

    private StyleSheetsActor(DevToolsServer devtools, string name, WeakReference<TabActor> tab)
        : base(devtools, name)
    {
        _tab = tab;

        // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
        if (tab.TryGetTarget(out var tabActor))
        {
            var weakSelf = new WeakReference<StyleSheetsActor>(this); // Mapping from C++ make_weak_ptr
            devtools.Delegate.ListenForStyleSheetSources(
                tabActor.Description,
                (styleSheet, source) =>
                {
                    if (weakSelf.TryGetTarget(out var self))
                        self.StyleSheetSourceReceived(styleSheet, source);
                });
        }
    }

    ~StyleSheetsActor()
    {
        // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
        if (_tab.TryGetTarget(out var tab))
            Devtools.Delegate.StopListeningForStyleSheetSources(tab.Description);
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        if (message.Type == "getText")
        {
            var resourceIdResult = GetRequiredParameter<string>(message, "resourceId");
            if (!resourceIdResult.HasValue)
                return;

            var parts = resourceIdResult.Value.Split(':');
            if (parts.Length < 2 || !int.TryParse(parts[^1], out var index) || index >= _styleSheets.Count)
            {
                SendUnknownActorError(message, resourceIdResult.Value);
                return;
            }

            // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
            if (_tab.TryGetTarget(out var tab))
            {
                Devtools.Delegate.RetrieveStyleSheetSource(tab.Description, _styleSheets[index]);
                _pendingStyleSheetSourceRequests[index] = message;
            }
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // From C++: void set_style_sheets(Vector<Web::CSS::StyleSheetIdentifier>)
    public void SetStyleSheets(List<Web.Css.StyleSheetIdentifier> styleSheets)
    {
        _styleSheets = styleSheets;
    }

    // From C++: void style_sheet_source_received(Web::CSS::StyleSheetIdentifier const&, String source)
    private void StyleSheetSourceReceived(Web.Css.StyleSheetIdentifier styleSheet, string source)
    {
        var index = _styleSheets.FindIndex(candidate =>
            candidate.Type == styleSheet.Type && candidate.Url == styleSheet.Url);

        if (index == -1)
            return;

        if (!_pendingStyleSheetSourceRequests.TryGetValue(index, out var pendingMessage))
            return;

        _pendingStyleSheetSourceRequests.Remove(index);

        // FIXME: Support the `longString` message type so that we don't have to send the entire style sheet
        //        source at once for large sheets.
        var response = new JsonObject
        {
            ["text"] = source
        };

        SendResponse(pendingMessage, response);
    }
}