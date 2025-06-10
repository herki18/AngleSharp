// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/ConsoleActor.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/ConsoleActor.cpp

namespace LadyBird.Libraries.LibDevTools.Actors;

using System;
using System.Text.Json.Nodes;

public sealed class ConsoleActor : Actor
{
    public const string BaseName = "console";
    private readonly WeakReference<TabActor> _tab; // From C++ WeakPtr<TabActor>
    private ulong _executionId = 0;

    // From C++: static NonnullRefPtr<ConsoleActor> create(DevToolsServer&, String name, WeakPtr<TabActor>)
    public static ConsoleActor Create(DevToolsServer devtools, string name, WeakReference<TabActor> tab)
    {
        return new ConsoleActor(devtools, name, tab);
    }

    private ConsoleActor(DevToolsServer devtools, string name, WeakReference<TabActor> tab)
        : base(devtools, name)
    {
        _tab = tab;
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "autocomplete")
        {
            response["matches"] = new JsonArray();
            response["matchProp"] = string.Empty;
            SendResponse(message, response);
            return;
        }

        if (message.Type == "evaluateJSAsync")
        {
            var textResult = GetRequiredParameter<string>(message, "text");
            if (!textResult.HasValue)
                return;

            var resultId = $"{Name}-{_executionId++}";
            response["resultID"] = resultId;
            SendResponse(message, response);

            // FIXME: We do not support eager evaluation of scripts. Just bail for now.
            var eagerResult = message.Data.TryGetPropertyValue("eager", out var eagerValue) &&
                            eagerValue?.GetValue<bool>() == true;
            if (eagerResult)
            {
                return;
            }

            // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
            if (_tab.TryGetTarget(out var tab))
            {
                Devtools.Delegate.EvaluateJavaScript(
                    tab.Description,
                    textResult.Value,
                    AsyncHandler<ConsoleActor, JsonValue>(null, (self, result, resp) =>
                    {
                        ReceivedConsoleResult(resp, resultId, textResult.Value, result);
                    })
                );
            }
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // From C++: static void received_console_result(JsonObject& response, String result_id, String input, JsonValue result)
    private static void ReceivedConsoleResult(JsonObject response, string resultId, string input, JsonValue result)
    {
        response["type"] = "evaluationResult";
        response["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        response["resultID"] = resultId;
        response["input"] = input;
        response["result"] = result;
        response["exception"] = JsonValue.Create((object)null);
        response["exceptionMessage"] = JsonValue.Create((object)null);
        response["helperResult"] = JsonValue.Create((object)null);
    }
}