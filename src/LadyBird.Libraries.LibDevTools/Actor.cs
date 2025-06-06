namespace LadyBird.Libraries.LibDevTools;

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

public abstract class Actor
{
    public struct Message
    {
        public string Type { get; set; }
        public JsonObject Data { get; set; }
        public ulong Id { get; set; }
    }

    private readonly DevToolsServer _devtools;
    private readonly string _name;
    private readonly List<PendingResponse> _pendingResponses = new();
    private ulong _nextMessageId = 0;

    protected Actor(DevToolsServer devtools, string name)
    {
        _devtools = devtools ?? throw new ArgumentNullException(nameof(devtools));
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name => _name;

    // From C++: void message_received(StringView type, JsonObject message)
    public void MessageReceived(string type, JsonObject message)
    {
        var messageId = _nextMessageId++;
        _pendingResponses.Add(new PendingResponse { Id = messageId });
        HandleMessage(new Message { Type = type, Data = message, Id = messageId });
    }

    // From C++: void send_response(Message const& message, JsonObject response)
    public void SendResponse(Message message, JsonObject response)
    {
        var connection = Devtools.Connection;
        if (connection == null)
            return;

        response["from"] = Name;

        for (int i = 0; i < _pendingResponses.Count; i++)
        {
            var pendingResponse = _pendingResponses[i];
            if (pendingResponse.Id != message.Id)
                continue;

            _pendingResponses[i] = new PendingResponse { Id = pendingResponse.Id, Response = response };
            if (i != 0)
                return;
        }

        int numberOfSentMessages = 0;
        foreach (var pendingResponse in _pendingResponses)
        {
            if (!pendingResponse.Response.HasValue)
                break;

            connection.SendMessage(pendingResponse.Response.Value);
            numberOfSentMessages++;
        }

        _pendingResponses.RemoveRange(0, numberOfSentMessages);
    }

    // From C++: void send_message(JsonObject message)
    public void SendMessage(JsonObject message)
    {
        var connection = Devtools.Connection;
        if (connection == null)
            return;

        message["from"] = Name;

        if (_pendingResponses.Count == 0)
        {
            connection.SendMessage(message);
            return;
        }

        _pendingResponses.Add(new PendingResponse { Response = message });
    }

    // From C++: void send_missing_parameter_error(Optional<Message const&> message, StringView parameter)
    public void SendMissingParameterError(Message? message, string parameter)
    {
        var error = new JsonObject
        {
            ["error"] = "missingParameter",
            ["message"] = $"Missing parameter: '{parameter}'"
        };

        if (message.HasValue)
            SendResponse(message.Value, error);
        else
            SendMessage(error);
    }

    // From C++: void send_unrecognized_packet_type_error(Message const& message)
    public void SendUnrecognizedPacketTypeError(Message message)
    {
        var error = new JsonObject
        {
            ["error"] = "unrecognizedPacketType",
            ["message"] = $"Unrecognized packet type: '{message.Type}'"
        };

        SendResponse(message, error);
    }

    // From C++: void send_unknown_actor_error(Optional<Message const&> message, StringView actor)
    public void SendUnknownActorError(Message? message, string actor)
    {
        var error = new JsonObject
        {
            ["error"] = "unknownActor",
            ["message"] = $"Unknown actor: '{actor}'"
        };

        if (message.HasValue)
            SendResponse(message.Value, error);
        else
            SendMessage(error);
    }

    protected abstract void HandleMessage(Message message);

    protected DevToolsServer Devtools => _devtools;

    // From C++: template<typename ParameterType> auto get_required_parameter(Message const& message, StringView parameter)
    protected Result<T> GetRequiredParameter<T>(Message message, string parameter)
    {
        try
        {
            if (!message.Data.ContainsKey(parameter))
            {
                SendMissingParameterError(message, parameter);
                return Result<T>.Failure(new Exception($"Missing parameter: {parameter}"));
            }

            var value = message.Data[parameter];

            if (typeof(T) == typeof(string))
            {
                return Result<T>.Success((T)(object)value.GetValue<string>());
            }
            else if (typeof(T) == typeof(bool))
            {
                return Result<T>.Success((T)(object)value.GetValue<bool>());
            }
            else if (typeof(T) == typeof(int))
            {
                return Result<T>.Success((T)(object)value.GetValue<int>());
            }
            else if (typeof(T) == typeof(ulong))
            {
                return Result<T>.Success((T)(object)value.GetValue<ulong>());
            }
            else if (typeof(T) == typeof(JsonObject))
            {
                return Result<T>.Success((T)(object)value.AsObject());
            }
            else if (typeof(T) == typeof(JsonArray))
            {
                return Result<T>.Success((T)(object)value.AsArray());
            }
            else
            {
                throw new NotSupportedException($"Type {typeof(T)} is not supported");
            }
        }
        catch (Exception ex)
        {
            SendMissingParameterError(message, parameter);
            return Result<T>.Failure(ex);
        }
    }

    // From C++: template<typename ActorType = Actor, typename Handler> auto async_handler(Optional<Message const&> message, Handler&& handler)
    protected Func<Result<TResult>, Task> AsyncHandler<TActorType, TResult>(Message? message, Action<TActorType, TResult, JsonObject> handler)
        where TActorType : Actor
    {
        var messageId = message?.Id;
        var weakSelf = new WeakReference<TActorType>((TActorType)this); // Mapping from C++ WeakPtr

        return async (result) =>
        {
            if (result.IsFailure)
            {
                // C++ equivalent: dbgln_if(DEVTOOLS_DEBUG, "Error performing async action: {}", result.error());
                Console.WriteLine($"Error performing async action: {result.Error}");
                return;
            }

            // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
            if (weakSelf.TryGetTarget(out var self))
            {
                var response = new JsonObject();
                handler(self, result.Value, response);

                if (messageId.HasValue)
                    self.SendResponse(new Message { Id = messageId.Value }, response);
                else
                    self.SendMessage(response);
            }
        };
    }

    // From C++: auto default_async_handler(Message const& message)
    protected Func<Result<object>, Task> DefaultAsyncHandler(Message message)
    {
        return AsyncHandler<Actor, object>(message, (self, result, response) => { });
    }

    private struct PendingResponse
    {
        public ulong? Id { get; set; }
        public JsonObject? Response { get; set; }

        public bool HasValue => Response != null;
    }
}