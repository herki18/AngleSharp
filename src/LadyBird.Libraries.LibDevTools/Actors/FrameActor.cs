namespace LadyBird.Libraries.LibDevTools.Actors;

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using LadyBird.Libraries.LibDevTools;

public sealed class FrameActor : Actor
{
    public const string BaseName = "frame";

    private readonly WeakReference<TabActor> _tab; // From C++ WeakPtr
    private readonly WeakReference<CssPropertiesActor> _cssProperties; // From C++ WeakPtr
    private readonly WeakReference<ConsoleActor> _console; // From C++ WeakPtr
    private readonly WeakReference<InspectorActor> _inspector; // From C++ WeakPtr
    private readonly WeakReference<StyleSheetsActor> _styleSheets; // From C++ WeakPtr
    private readonly WeakReference<ThreadActor> _thread; // From C++ WeakPtr

    private int _highestNotifiedMessageIndex = -1;
    private int _highestReceivedMessageIndex = -1;
    private bool _waitingForMessages = false;

    // From C++: static NonnullRefPtr<FrameActor> create(...)
    public static FrameActor Create(DevToolsServer devtools, string name,
        WeakReference<TabActor> tab, WeakReference<CssPropertiesActor> cssProperties,
        WeakReference<ConsoleActor> console, WeakReference<InspectorActor> inspector,
        WeakReference<StyleSheetsActor> styleSheets, WeakReference<ThreadActor> thread)
    {
        return new FrameActor(devtools, name, tab, cssProperties, console, inspector, styleSheets, thread);
    }

    private FrameActor(DevToolsServer devtools, string name,
        WeakReference<TabActor> tab, WeakReference<CssPropertiesActor> cssProperties,
        WeakReference<ConsoleActor> console, WeakReference<InspectorActor> inspector,
        WeakReference<StyleSheetsActor> styleSheets, WeakReference<ThreadActor> thread)
        : base(devtools, name)
    {
        _tab = tab;
        _cssProperties = cssProperties;
        _console = console;
        _inspector = inspector;
        _styleSheets = styleSheets;
        _thread = thread;

        // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
        if (tab.TryGetTarget(out var tabActor))
        {
            var weakSelf = new WeakReference<FrameActor>(this); // Mapping from C++ make_weak_ptr

            devtools.Delegate.ListenForConsoleMessages(
                tabActor.Description,
                (messageIndex) =>
                {
                    if (weakSelf.TryGetTarget(out var self))
                        self.ConsoleMessageAvailable(messageIndex);
                },
                (startIndex, consoleOutput) =>
                {
                    if (weakSelf.TryGetTarget(out var self))
                        self.ConsoleMessagesReceived(startIndex, consoleOutput);
                });

            // FIXME: We should adopt WebContent to inform us when style sheets are available or removed.
            devtools.Delegate.RetrieveStyleSheets(tabActor.Description,
                AsyncHandler<FrameActor, List<Web.Css.StyleSheetIdentifier>>(null,
                    (self, styleSheets, response) => self.StyleSheetsAvailable(response, styleSheets)));
        }
    }

    ~FrameActor()
    {
        // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
        if (_tab.TryGetTarget(out var tab))
            Devtools.Delegate.StopListeningForConsoleMessages(tab.Description);
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "detach")
        {
            // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
            if (_tab.TryGetTarget(out var tab))
            {
                Devtools.Delegate.StopListeningForDomProperties(tab.Description);
                Devtools.Delegate.StopListeningForDomMutations(tab.Description);
                Devtools.Delegate.StopListeningForConsoleMessages(tab.Description);
                Devtools.Delegate.StopListeningForStyleSheetSources(tab.Description);
                tab.ResetSelectedNode();
            }
            SendResponse(message, response);
            return;
        }

        if (message.Type == "listFrames")
        {
            SendResponse(message, response);
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // From C++: void send_frame_update_message()
    public void SendFrameUpdateMessage()
    {
        var frames = new JsonArray();
        // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
        if (_tab.TryGetTarget(out var tabActor))
        {
            var frame = new JsonObject
            {
                ["id"] = tabActor.Description.Id,
                ["title"] = tabActor.Description.Title,
                ["url"] = tabActor.Description.Url
            };
            frames.Add(frame);
        }

        var message = new JsonObject
        {
            ["type"] = "frameUpdate",
            ["frames"] = frames
        };
        SendMessage(message);
    }

    // From C++: JsonObject serialize_target() const
    public JsonObject SerializeTarget()
    {
        var traits = new JsonObject
        {
            ["frames"] = true,
            ["isBrowsingContext"] = true,
            ["logInPage"] = false,
            ["navigation"] = true,
            ["supportsTopLevelTargetFlag"] = true,
            ["watchpoints"] = true
        };

        var target = new JsonObject
        {
            ["actor"] = Name,
            ["traits"] = traits
        };

        // Using WeakReference.TryGetTarget for all weak references
        if (_tab.TryGetTarget(out var tabActor))
        {
            target["title"] = tabActor.Description.Title;
            target["url"] = tabActor.Description.Url;
            target["browsingContextID"] = tabActor.Description.Id;
            target["outerWindowID"] = tabActor.Description.Id;
            target["isTopLevelTarget"] = true;
        }

        if (_cssProperties.TryGetTarget(out var cssProperties))
            target["cssPropertiesActor"] = cssProperties.Name;
        if (_console.TryGetTarget(out var console))
            target["consoleActor"] = console.Name;
        if (_inspector.TryGetTarget(out var inspector))
            target["inspectorActor"] = inspector.Name;
        if (_styleSheets.TryGetTarget(out var styleSheets))
            target["styleSheetsActor"] = styleSheets.Name;
        if (_thread.TryGetTarget(out var thread))
            target["threadActor"] = thread.Name;

        return target;
    }

    // From C++: void style_sheets_available(JsonObject& response, Vector<Web::CSS::StyleSheetIdentifier> style_sheets)
    private void StyleSheetsAvailable(JsonObject response, List<Web.Css.StyleSheetIdentifier> styleSheets)
    {
        var sheets = new JsonArray();
        string tabUrl = null;

        if (_tab.TryGetTarget(out var tabActor))
            tabUrl = tabActor.Description.Url;

        if (!_styleSheets.TryGetTarget(out var styleSheetsActor))
            return;

        for (int i = 0; i < styleSheets.Count; i++)
        {
            var styleSheet = styleSheets[i];
            var resourceId = $"{styleSheetsActor.Name}-stylesheet:{i}";
            JsonValue href = JsonValue.Create((object)null);
            JsonValue sourceMapBaseUrl = JsonValue.Create((object)null);
            JsonValue title = JsonValue.Create((object)null);

            if (styleSheet.Url != null)
            {
                // LibWeb sets the URL to a style sheet name for UA style sheets. DevTools would reject these invalid URLs.
                if (styleSheet.Type == Web.Css.StyleSheetIdentifier.StyleSheetType.UserAgent)
                {
                    title = styleSheet.Url;
                    sourceMapBaseUrl = tabUrl;
                }
                else
                {
                    href = styleSheet.Url;
                    sourceMapBaseUrl = styleSheet.Url;
                }
            }
            else
            {
                sourceMapBaseUrl = tabUrl;
            }

            var sheet = new JsonObject
            {
                ["atRules"] = new JsonArray(),
                ["constructed"] = false,
                ["disabled"] = false,
                ["fileName"] = JsonValue.Create((object)null),
                ["href"] = href,
                ["isNew"] = false,
                ["nodeHref"] = tabUrl,
                ["resourceId"] = resourceId,
                ["ruleCount"] = styleSheet.RuleCount,
                ["sourceMapBaseURL"] = sourceMapBaseUrl,
                ["sourceMapURL"] = "",
                ["styleSheetIndex"] = i,
                ["system"] = false,
                ["title"] = title
            };
            sheets.Add(sheet);
        }

        var stylesheets = new JsonArray();
        stylesheets.Add("stylesheet");
        stylesheets.Add(sheets);

        var array = new JsonArray();
        array.Add(stylesheets);

        response["type"] = "resources-available-array";
        response["array"] = array;

        styleSheetsActor.SetStyleSheets(styleSheets);
    }

    // From C++: void console_message_available(i32 message_index)
    private void ConsoleMessageAvailable(int messageIndex)
    {
        if (messageIndex <= _highestReceivedMessageIndex)
        {
            Console.WriteLine("Notified about console message we already have");
            return;
        }

        if (messageIndex <= _highestNotifiedMessageIndex)
        {
            Console.WriteLine("Notified about console message we're already aware of");
            return;
        }

        _highestNotifiedMessageIndex = messageIndex;

        if (!_waitingForMessages)
            RequestConsoleMessages();
    }

    // From C++: void console_messages_received(i32 start_index, Vector<WebView::ConsoleOutput>)
    private void ConsoleMessagesReceived(int startIndex, List<WebView.ConsoleOutput> consoleOutput)
    {
        var endIndex = startIndex + consoleOutput.Count - 1;
        if (endIndex <= _highestReceivedMessageIndex)
        {
            Console.WriteLine("Received old console messages");
            return;
        }

        var consoleMessages = new JsonArray();
        var errorMessages = new JsonArray();

        foreach (var output in consoleOutput)
        {
            var message = new JsonObject();

            output.Output.Visit(
                (WebView.ConsoleLog log) =>
                {
                    string level = log.Level switch
                    {
                        Js.Console.LogLevel.Debug => "debug",
                        Js.Console.LogLevel.Error => "error",
                        Js.Console.LogLevel.Info => "info",
                        Js.Console.LogLevel.Log => "log",
                        Js.Console.LogLevel.Warn => "warn",
                        _ => null // FIXME: Implement remaining console levels.
                    };

                    if (level == null)
                        return;

                    message["level"] = level;
                    message["filename"] = "<eval>";
                    message["lineNumber"] = 1;
                    message["columnNumber"] = 1;
                    message["timeStamp"] = output.Timestamp.ToUnixTimeMilliseconds();
                    message["arguments"] = new JsonArray { log.Arguments };
                    consoleMessages.Add(message);
                },
                (WebView.ConsoleError error) =>
                {
                    var stack = new System.Text.StringBuilder();
                    foreach (var frame in error.Trace)
                    {
                        if (frame.Function != null)
                            stack.Append(frame.Function);
                        stack.Append('@');
                        stack.Append(frame.File ?? "unknown");
                        stack.AppendFormat(":{0}:{1}\n", frame.Line ?? 0, frame.Column ?? 0);
                    }

                    var preview = new JsonObject
                    {
                        ["kind"] = "Error",
                        ["message"] = error.Message,
                        ["name"] = error.Name
                    };

                    if (stack.Length > 0)
                        preview["stack"] = stack.ToString();

                    var exception = new JsonObject
                    {
                        ["class"] = error.Name,
                        ["isError"] = true,
                        ["preview"] = preview
                    };

                    var pageError = new JsonObject
                    {
                        ["error"] = true,
                        ["exception"] = exception,
                        ["hasException"] = error.Trace.Count > 0,
                        ["isPromiseRejection"] = error.InsidePromise,
                        ["timeStamp"] = output.Timestamp.ToUnixTimeMilliseconds()
                    };

                    message["pageError"] = pageError;
                    errorMessages.Add(message);
                });
        }

        var array = new JsonArray();
        if (consoleMessages.Count > 0)
        {
            var consoleMessage = new JsonArray();
            consoleMessage.Add("console-message");
            consoleMessage.Add(consoleMessages);
            array.Add(consoleMessage);
        }

        if (errorMessages.Count > 0)
        {
            var errorMessage = new JsonArray();
            errorMessage.Add("error-message");
            errorMessage.Add(errorMessages);
            array.Add(errorMessage);
        }

        var msg = new JsonObject
        {
            ["type"] = "resources-available-array",
            ["array"] = array
        };
        SendMessage(msg);

        _highestReceivedMessageIndex = endIndex;
        _waitingForMessages = false;

        if (_highestReceivedMessageIndex < _highestNotifiedMessageIndex)
            RequestConsoleMessages();
    }

    // From C++: void request_console_messages()
    private void RequestConsoleMessages()
    {
        System.Diagnostics.Debug.Assert(!_waitingForMessages);

        // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
        if (_tab.TryGetTarget(out var tab))
        {
            Devtools.Delegate.RequestConsoleMessages(tab.Description, _highestReceivedMessageIndex + 1);
            _waitingForMessages = true;
        }
    }
}