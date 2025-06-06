namespace LadyBird.Libraries.LibDevTools;

using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

public class Connection
{
    private readonly Core.BufferedTcpSocket _socket;

    public event Action ConnectionClosed;
    public event Action<JsonObject> MessageReceived;

    // From C++: static NonnullRefPtr<Connection> create(NonnullOwnPtr<Core::BufferedTCPSocket>)
    public static Connection Create(Core.BufferedTcpSocket socket)
    {
        return new Connection(socket);
    }

    private Connection(Core.BufferedTcpSocket socket)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _socket.ReadyToRead += OnReadyToRead;
    }

    // From C++: void send_message(JsonValue const&)
    // https://firefox-source-docs.mozilla.org/devtools/backend/protocol.html#packets
    public void SendMessage(JsonValue message)
    {
        var serialized = message.ToJsonString();

        // C++ equivalent: dbgln_if(DEVTOOLS_DEBUG, ...)
        if (message is JsonObject obj && obj.ContainsKey("error"))
            Console.WriteLine($"\x1b[1;31m<<\x1b[0m {serialized}");
        else
            Console.WriteLine($"\x1b[1;32m<<\x1b[0m {serialized}");

        try
        {
            var formattedMessage = $"{serialized.Length}:{serialized}";
            _socket.Write(Encoding.UTF8.GetBytes(formattedMessage));
        }
        catch
        {
            ConnectionClosed?.Invoke();
        }
    }

    // From C++: ErrorOr<JsonValue> read_message()
    // https://firefox-source-docs.mozilla.org/devtools/backend/protocol.html#packets
    private Result<JsonValue> ReadMessage()
    {
        try
        {
            var lengthBuffer = new System.Collections.Generic.List<byte>();

            // FIXME: `read_until(':')` would be nicer here, but that seems to return immediately without receiving any data.
            while (true)
            {
                var b = _socket.ReadByte();
                if (b == -1)
                    return Result<JsonValue>.Failure(new EndOfStreamException("Unexpected end of stream"));

                if (b == ':')
                    break;

                lengthBuffer.Add((byte)b);
            }

            var lengthStr = Encoding.UTF8.GetString(lengthBuffer.ToArray());
            if (!int.TryParse(lengthStr, out var length))
                return Result<JsonValue>.Failure(new Exception("Could not read message length from DevTools client"));

            var messageBuffer = new byte[length];
            var bytesRead = _socket.Read(messageBuffer, 0, length);
            if (bytesRead != length)
                return Result<JsonValue>.Failure(new Exception($"Expected {length} bytes but got {bytesRead}"));

            var messageStr = Encoding.UTF8.GetString(messageBuffer);
            var message = JsonNode.Parse(messageStr);

            // C++ equivalent: dbgln_if(DEVTOOLS_DEBUG, ...)
            Console.WriteLine($"\x1b[1;33m>>\x1b[0m {message}");

            return Result<JsonValue>.Success(message);
        }
        catch (Exception ex)
        {
            return Result<JsonValue>.Failure(ex);
        }
    }

    // From C++: ErrorOr<void> on_ready_to_read()
    private void OnReadyToRead()
    {
        try
        {
            // https://firefox-source-docs.mozilla.org/devtools/backend/protocol.html#the-request-reply-pattern
            // Note that it is correct for a client to send several requests to a request/reply actor without waiting for a
            // reply to each request before sending the next; requests can be pipelined.
            while (_socket.CanReadWithoutBlocking())
            {
                var messageResult = ReadMessage();
                if (messageResult.IsFailure)
                {
                    ConnectionClosed?.Invoke();
                    return;
                }

                if (messageResult.Value is not JsonObject obj)
                    continue;

                // Using Core.DeferredInvoke equivalent
                Task.Run(() => MessageReceived?.Invoke(obj));
            }
        }
        catch
        {
            ConnectionClosed?.Invoke();
        }
    }
}