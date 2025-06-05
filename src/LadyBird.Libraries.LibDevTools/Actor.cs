// Copyright (c) 2025, Tim Flynn <trflynn89@ladybird.org>
// SPDX-License-Identifier: BSD-2-Clause

namespace LadyBird.Libraries.LibDevTools;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Nodes;

// C# translation of C++ class Actor
public abstract class Actor
{
    public class Message
    {
        public string Type { get; set; } = string.Empty;
        public JsonObject Data { get; set; } = new JsonObject();
        public ulong Id { get; set; } = 0;
    }

    private DevToolsServer _devtools;
    private string _name;
    private List<PendingResponse> _pendingResponses = new List<PendingResponse>();
    private ulong _nextMessageId = 0;

    protected Actor(DevToolsServer devtools, string name)
    {
        _devtools = devtools;
        _name = name;
    }

    // C++: virtual ~Actor();
    ~Actor()
    {
        // Destructor logic if needed
    }

    public string Name => _name;

    // C++: void message_received(StringView type, JsonObject message)
    public void MessageReceived(string type, JsonObject message)
    {
        var messageId = _nextMessageId++;
        _pendingResponses.Add(new PendingResponse { Id = messageId, Response = null });

        HandleMessage(new Message { Type = type, Data = message, Id = messageId });
    }

    // C++: void send_response(Message const&, JsonObject)
    public void SendResponse(Message message, JsonObject response)
    {
        var connection = Devtools().Connection();
        if (connection == null)
            return;

        response.Set("from", Name);

        for (int i = 0; i < _pendingResponses.Count; ++i)
        {
            var pendingResponse = _pendingResponses[i];
            if (pendingResponse.Id != message.Id)
                continue;

            pendingResponse.Response = response;

            if (i != 0)
                return;
        }

        int numberOfSentMessages = 0;

        foreach (var pendingResponse in _pendingResponses)
        {
            if (!pendingResponse.Response.HasValue)
                break;

            connection.SendMessage(pendingResponse.Response.Value);
            ++numberOfSentMessages;
        }

        if (numberOfSentMessages > 0)
            _pendingResponses.RemoveRange(0, numberOfSentMessages);
    }

    // C++: void send_message(JsonObject)
    public void SendMessage(JsonObject message)
    {
        var connection = Devtools().Connection();
        if (connection == null)
            return;

        message.Set("from", Name);

        if (_pendingResponses.Count == 0)
        {
            connection.SendMessage(message);
            return;
        }

        _pendingResponses.Add(new PendingResponse { Id = null, Response = message });
    }

    // C++: void send_missing_parameter_error(Optional<Message const&>, StringView parameter)
    public void SendMissingParameterError(Message? message, string parameter)
    {
        var error = new JsonObject();
        error.Set("error", "missingParameter");
        error.Set("message", String.Format("Missing parameter: '{0}'", parameter));

        if (message != null)
            SendResponse(message, error);
        else
            SendMessage(error);
    }

    // C++: void send_unrecognized_packet_type_error(Message const&)
    public void SendUnrecognizedPacketTypeError(Message message)
    {
        var error = new JsonObject();
        error.Set("error", "unrecognizedPacketType");
        error.Set("message", String.Format("Unrecognized packet type: '{0}'", message.Type));
        SendResponse(message, error);
    }

    // C++: void send_unknown_actor_error(Optional<Message const&>, StringView actor)
    public void SendUnknownActorError(Message? message, string actor)
    {
        var error = new JsonObject();
        error.Set("error", "unknownActor");
        error.Set("message", String.Format("Unknown actor: '{0}'", actor));

        if (message != null)
            SendResponse(message, error);
        else
            SendMessage(error);
    }

    // C++: protected virtual void handle_message(Message const&) = 0;
    protected abstract void HandleMessage(Message message);

    // C++: DevToolsServer& devtools()
    protected DevToolsServer Devtools() => _devtools;

    // C++: template<typename ParameterType> auto get_required_parameter(Message const& message, StringView parameter)
    protected T? GetRequiredParameter<T>(Message message, string parameter)
    {
        object? result = null;
        if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
            result = message.Data.GetInteger<T>(parameter);
        else if (typeof(T) == typeof(bool))
            result = message.Data.GetBool(parameter);
        else if (typeof(T) == typeof(string))
            result = message.Data.GetString(parameter);
        else if (typeof(T) == typeof(JsonObject))
            result = message.Data.GetObject(parameter);
        else if (typeof(T) == typeof(JsonArray))
            result = message.Data.GetArray(parameter);
        else
            throw new NotImplementedException("Type not supported in GetRequiredParameter");

        if (result == null)
            SendMissingParameterError(message, parameter);

        return (T?)result;
    }

    // C++: template<typename ActorType = Actor, typename Handler> auto async_handler(Optional<Message const&> message, Handler&& handler)
    protected Action<Result> AsyncHandler<TActor>(Message? message, Action<TActor, object, JsonObject> handler)
        where TActor : Actor
    {
        ulong? messageId = message?.Id;

        // Weak reference to self
        var weakSelf = new WeakReference<TActor>((TActor)this);

        return (result) =>
        {
            if (result.IsError)
            {
                Debug.DbgLnIf(Debug.DevtoolsDebug, "Error performing async action: {0}", result.Error);
                return;
            }

            if (weakSelf.TryGetTarget(out var self))
            {
                var response = new JsonObject();
                handler(self, result.ReleaseValue(), response);

                if (messageId.HasValue)
                    self.SendResponse(new Message { Id = messageId.Value }, response);
                else
                    self.SendMessage(response);
            }
        };
    }

    // C++: auto default_async_handler(Message const& message)
    protected Action<Result> DefaultAsyncHandler(Message message)
    {
        return AsyncHandler<Actor>(message, (self, value, response) => { });
    }

    // C++: struct PendingResponse
    private class PendingResponse
    {
        public ulong? Id;
        public JsonObject? Response;

        public bool HasValue => Response != null;
    }
}