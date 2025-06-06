namespace LadyBird.Libraries.LibDevTools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Actors;

public class DevToolsServer
{
    private static ulong s_serverCount = 0;

    private readonly Core.TcpServer _server;
    private readonly DevToolsDelegate _delegate;
    private readonly ulong _serverId;
    private Connection _connection;
    private readonly Dictionary<string, Actor> _actorRegistry = new();
    private RootActor _rootActor;
    private ulong _actorCount = 0;

    // From C++: static ErrorOr<NonnullOwnPtr<DevToolsServer>> create(DevToolsDelegate&, u16 port)
    public static Result<DevToolsServer> Create(DevToolsDelegate devToolsDelegate, ushort port)
    {
        try
        {
            var address = IPAddress.Parse("0.0.0.0");
            var server = Core.TcpServer.TryCreate();
            server.Listen(address, port, Core.TcpServer.AllowAddressReuse.Yes);
            return Result<DevToolsServer>.Success(new DevToolsServer(devToolsDelegate, server));
        }
        catch (Exception ex)
        {
            return Result<DevToolsServer>.Failure(ex);
        }
    }

    private DevToolsServer(DevToolsDelegate devToolsDelegate, Core.TcpServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _delegate = devToolsDelegate ?? throw new ArgumentNullException(nameof(devToolsDelegate));
        _serverId = s_serverCount++;

        _server.ReadyToAccept += OnNewClient;
    }

    public Connection Connection => _connection;
    public DevToolsDelegate Delegate => _delegate;
    public IReadOnlyDictionary<string, Actor> ActorRegistry => _actorRegistry;

    // From C++: void refresh_tab_list()
    public void RefreshTabList()
    {
        if (_rootActor == null)
            return;

        // Remove all TabActor instances
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

    // From C++: template<typename ActorType, typename... Args> ActorType& register_actor(Args&&... args)
    public TActorType RegisterActor<TActorType>(params object[] args) where TActorType : Actor
    {
        string name;
        var id = _actorCount++;

        if (typeof(TActorType) == typeof(RootActor))
        {
            name = RootActor.BaseName;
        }
        else
        {
            var baseName = typeof(TActorType).GetField("BaseName")?.GetValue(null) as string ?? "unknown";
            name = $"server{_serverId}-{baseName}{id}";
        }

        // Create actor using reflection to call the static Create method
        var createMethod = typeof(TActorType).GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (createMethod == null)
            throw new InvalidOperationException($"Actor type {typeof(TActorType)} must have a static Create method");

        var allArgs = new object[] { this, name }.Concat(args).ToArray();
        var actor = (TActorType)createMethod.Invoke(null, allArgs);

        _actorRegistry[name] = actor;
        return actor;
    }

    // From C++: ErrorOr<void> on_new_client()
    private void OnNewClient()
    {
        try
        {
            if (_connection != null)
            {
                Console.WriteLine("Only one active DevTools connection is currently allowed");
                return;
            }

            var client = _server.Accept();
            var bufferedSocket = Core.BufferedTcpSocket.Create(client);
            _connection = this.Connection.Create(bufferedSocket);

            _connection.ConnectionClosed += CloseConnection;
            _connection.MessageReceived += OnMessageReceived;

            _rootActor = RegisterActor<RootActor>();
            RegisterActor<DeviceActor>();
            RegisterActor<PreferenceActor>();
            RegisterActor<ProcessActor>(new ProcessDescription { IsParent = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to accept DevTools client: {ex}");
        }
    }

    // From C++: void on_message_received(JsonObject)
    private void OnMessageReceived(JsonObject message)
    {
        if (!message.TryGetPropertyValue("to", out var toValue) ||
            toValue?.GetValue<string>() is not string to)
        {
            _rootActor?.SendMissingParameterError(null, "to");
            return;
        }

        if (!_actorRegistry.TryGetValue(to, out var actor))
        {
            _rootActor?.SendUnknownActorError(null, to);
            return;
        }

        if (!message.TryGetPropertyValue("type", out var typeValue) ||
            typeValue?.GetValue<string>() is not string type)
        {
            actor.SendMissingParameterError(null, "type");
            return;
        }

        actor.MessageReceived(type, message);
    }

    // From C++: void close_connection()
    private void CloseConnection()
    {
        // C++ equivalent: dbgln_if(DEVTOOLS_DEBUG, "Lost connection to the DevTools client")
        Console.WriteLine("Lost connection to the DevTools client");

        // Using Task.Run as equivalent to Core::deferred_invoke
        Task.Run(() =>
        {
            _connection = null;
            _actorRegistry.Clear();
            _rootActor = null;
        });
    }

    // Helper method to concatenate arrays
    private static T[] Concat<T>(T[] first, T[] second)
    {
        var result = new T[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}