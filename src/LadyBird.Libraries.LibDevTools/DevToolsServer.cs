// Main implementation here, using file-scoped namespace and C# naming conventions

namespace LadyBird.Libraries.LibDevTools;

using System;
using System.Collections.Generic;

public class DevToolsServer
{
    private static ulong _serverCount = 0;

    private readonly CoreTcpServer _server;
    private Connection _connection;
    private readonly IDevToolsDelegate _delegate;
    private readonly Dictionary<string, Actor> _actorRegistry = new();
    private RootActor _rootActor = null;
    private readonly ulong _serverId;
    private ulong _actorCount = 0;

    // C++: static ErrorOr<NonnullOwnPtr<DevToolsServer>> create(DevToolsDelegate&, u16 port);
    public static Result<DevToolsServer> Create(IDevToolsDelegate devToolsDelegate, ushort port)
    {
        // C++: auto address = IPv4Address::from_string("0.0.0.0"sv).release_value();
        var address = IPv4Address.FromString("0.0.0.0"); // Stub

        // C++: auto server = TRY(Core::TCPServer::try_create());
        var serverResult = CoreTcpServer.TryCreate();
        if (serverResult.IsFailure)
            return Result<DevToolsServer>.Failure(serverResult.Error);

        var server = serverResult.Value;

        // C++: TRY(server->listen(address, port, Core::TCPServer::AllowAddressReuse::Yes));
        var listenResult = server.Listen(address, port, allowAddressReuse: true);
        if (listenResult.IsFailure)
            return Result<DevToolsServer>.Failure(listenResult.Error);

        return Result<DevToolsServer>.Success(new DevToolsServer(devToolsDelegate, server));
    }

    // C++: explicit DevToolsServer(DevToolsDelegate&, NonnullRefPtr<Core::TCPServer>);
    private DevToolsServer(IDevToolsDelegate devToolsDelegate, CoreTcpServer server)
    {
        _server = server;
        _delegate = devToolsDelegate;
        _serverId = _serverCount++;

        _server.OnReadyToAccept = () =>
        {
            var result = OnNewClient();
            if (result.IsFailure)
            {
                // C++: warnln("Failed to accept DevTools client: {}", result.error());
                Console.WriteLine($"Failed to accept DevTools client: {result.Error}");
            }
        };
    }

    // C++: ~DevToolsServer()
    ~DevToolsServer()
    {
        // No explicit cleanup needed in C#
    }

    // C++: RefPtr<Connection>& connection() { return m_connection; }
    public Connection Connection => _connection;

    // C++: DevToolsDelegate const& delegate() const { return m_delegate; }
    public IDevToolsDelegate Delegate => _delegate;

    // C++: ActorRegistry const& actor_registry() const { return m_actor_registry; }
    public IReadOnlyDictionary<string, Actor> ActorRegistry => _actorRegistry;

    // C++: template<typename ActorType, typename... Args>
    //       ActorType& register_actor(Args&&... args)
    public T RegisterActor<T>(params object[] args) where T : Actor
    {
        string name;
        var id = _actorCount++;

        if (typeof(T) == typeof(RootActor))
        {
            // C++: name = String::from_utf8_without_validation(ActorType::base_name.bytes());
            name = T.BaseName;
        }
        else
        {
            // C++: name = MUST(String::formatted("server{}-{}{}", m_server_id, ActorType::base_name, id));
            name = $"server{_serverId}-{T.BaseName}{id}";
        }

        // C++: auto actor = ActorType::create(*this, name, forward<Args>(args)...);
        var actor = (T)Activator.CreateInstance(typeof(T), this, name, args)!;
        _actorRegistry[name] = actor;

        return actor;
    }

    // C++: void refresh_tab_list()
    public void RefreshTabList()
    {
        if (_rootActor == null)
            return;

        // Remove all TabActor entries from the registry
        var keysToRemove = new List<string>();
        foreach (var kvp in _actorRegistry)
        {
            if (kvp.Value is TabActor)
                keysToRemove.Add(kvp.Key);
        }
        foreach (var key in keysToRemove)
            _actorRegistry.Remove(key);

        _rootActor.SendTabListChangedMessage();
    }

    // C++: ErrorOr<void> on_new_client()
    private Result<object> OnNewClient()
    {
        if (_connection != null)
            return Result<object>.Failure(new InvalidOperationException("Only one active DevTools connection is currently allowed"));

        var clientResult = _server.Accept();
        if (clientResult.IsFailure)
            return Result<object>.Failure(clientResult.Error);

        var client = clientResult.Value;

        var bufferedSocketResult = CoreBufferedTcpSocket.Create(client);
        if (bufferedSocketResult.IsFailure)
            return Result<object>.Failure(bufferedSocketResult.Error);

        var bufferedSocket = bufferedSocketResult.Value;

        _connection = Connection.Create(bufferedSocket);

        _connection.OnConnectionClosed = () => CloseConnection();

        _connection.OnMessageReceived = message => OnMessageReceived(message);

        _rootActor = RegisterActor<RootActor>();

        RegisterActor<DeviceActor>();
        RegisterActor<PreferenceActor>();
        RegisterActor<ProcessActor>(new ProcessDescription { IsParent = true });

        return Result<object>.Success(null);
    }

    // C++: void on_message_received(JsonObject message)
    private void OnMessageReceived(JsonObject message)
    {
        var to = message.GetString("to");
        if (to == null)
        {
            _rootActor?.SendMissingParameterError(null, "to");
            return;
        }

        if (!_actorRegistry.TryGetValue(to, out var actor))
        {
            _rootActor?.SendUnknownActorError(null, to);
            return;
        }

        var type = message.GetString("type");
        if (type == null)
        {
            actor.SendMissingParameterError(null, "type");
            return;
        }

        actor.MessageReceived(type, message);
    }

    // C++: void close_connection()
    private void CloseConnection()
    {
        // C++: dbgln_if(DEVTOOLS_DEBUG, "Lost connection to the DevTools client");
        Console.WriteLine("Lost connection to the DevTools client");

        // C++: Core::deferred_invoke([this]() { ... });
        // In C#, just clear immediately (no event loop/deferred invoke)
        _connection = null;
        _actorRegistry.Clear();
        _rootActor = null;
    }
}