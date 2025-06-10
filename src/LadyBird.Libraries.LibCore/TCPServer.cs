// namespace LadyBird.Libraries.LibCore;
//
// using System;
// using System.Net;
// using System.Net.Sockets;
// using System.Threading;
// using System.Threading.Tasks;
// using AK;
//
// // Copyright (c) 2018-2020, Andreas Kling <andreas@ladybird.org>
// // Copyright (c) 2021, Sam Atkins <atkinssj@serenityos.org>
// //
// // SPDX-License-Identifier: BSD-2-Clause
//
// public class TCPServer : EventReceiver
// {
//     // C++: enum class AllowAddressReuse { Yes, No };
//     public enum AllowAddressReuse
//     {
//         Yes,
//         No,
//     }
//
//     // C++: int m_fd { -1 };
//     private Socket? _socket;
//     private bool _listening = false;
//     private CancellationTokenSource? _acceptCts;
//     private readonly object _acceptLock = new();
//     private Socket? _pendingAcceptedSocket;
//
//     // C++: Function<void()> on_ready_to_accept;
//     // Event to notify when a connection is ready to accept
//     public event Action? OnReadyToAccept;
//
//     // C++: static ErrorOr<NonnullRefPtr<TCPServer>> try_create(EventReceiver* parent = nullptr);
//     public static Result<TCPServer> TryCreate(EventReceiver? parent = null)
//     {
//         try
//         {
//             // C++: int fd = TRY(Core::System::socket(AF_INET, SOCK_STREAM | SOCK_NONBLOCK | SOCK_CLOEXEC, 0));
//             var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//             return Result<TCPServer>.Success(new TCPServer(socket, parent));
//         }
//         catch (Exception ex)
//         {
//             return Result<TCPServer>.Failure(ex);
//         }
//     }
//
//     // C++: explicit TCPServer(int fd, EventReceiver* parent = nullptr);
//     private TCPServer(Socket socket, EventReceiver? parent = null)
//         : base(parent)
//     {
//         _socket = socket;
//         // C++: VERIFY(m_fd >= 0);
//     }
//
//     // C++: virtual ~TCPServer() override;
//     public override void Dispose()
//     {
//         // C++: MUST(Core::System::close(m_fd));
//         _acceptCts?.Cancel();
//         _acceptCts?.Dispose();
//         _acceptCts = null;
//         _socket?.Dispose();
//         _socket = null;
//         base.Dispose();
//     }
//
//     // C++: bool is_listening() const { return m_listening; }
//     public bool IsListening => _listening;
//
//     // C++: ErrorOr<void> listen(IPv4Address const& address, u16 port, AllowAddressReuse allow_address_reuse)
//     public Result<object?> Listen(IPv4Address address, ushort port, AllowAddressReuse allowAddressReuse = AllowAddressReuse.No)
//     {
//         try
//         {
//             if (_listening)
//                 // C++: if (m_listening) return Error::from_errno(EADDRINUSE);
//                 return Result<object?>.Failure(new InvalidOperationException("Address already in use"));
//
//             // C++: auto socket_address = SocketAddress(address, port);
//             //      auto in = socket_address.to_sockaddr_in();
//             var ip = address.ToIPAddress();
//             var endpoint = new IPEndPoint(ip, port);
//
//             if (allowAddressReuse == AllowAddressReuse.Yes)
//             {
//                 // C++: TRY(Core::System::setsockopt(m_fd, SOL_SOCKET, SO_REUSEADDR, &option, sizeof(option)));
//                 _socket!.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
//             }
//
//             // C++: TRY(Core::System::bind(m_fd, (sockaddr const*)&in, sizeof(in)));
//             _socket!.Bind(endpoint);
//             // C++: TRY(Core::System::listen(m_fd, 5));
//             _socket.Listen(5);
//             _listening = true;
//
//             // C++: m_notifier = Notifier::construct(m_fd, Notifier::Type::Read, this);
//             //      m_notifier->on_activation = [this] { if (on_ready_to_accept) on_ready_to_accept(); };
//             _acceptCts = new CancellationTokenSource();
//             _ = AcceptLoopAsync(_acceptCts.Token);
//
//             return Result<object?>.Success(null);
//         }
//         catch (Exception ex)
//         {
//             return Result<object?>.Failure(ex);
//         }
//     }
//
//     // Accept loop to simulate Notifier and on_ready_to_accept
//     private async Task AcceptLoopAsync(CancellationToken cancellationToken)
//     {
//         while (_listening && !cancellationToken.IsCancellationRequested)
//         {
//             try
//             {
//                 // C++: accept4/accept is non-blocking, so we use AcceptAsync
//                 var acceptedSocket = await _socket!.AcceptAsync(cancellationToken).ConfigureAwait(false);
//
//                 lock (_acceptLock)
//                 {
//                     // Only keep one pending accepted socket at a time
//                     if (_pendingAcceptedSocket == null)
//                     {
//                         _pendingAcceptedSocket = acceptedSocket;
//                         // C++: if (on_ready_to_accept) on_ready_to_accept();
//                         OnReadyToAccept?.Invoke();
//                     }
//                     else
//                     {
//                         // If user hasn't called Accept() yet, close the extra connection
//                         acceptedSocket.Dispose();
//                     }
//                 }
//             }
//             catch (OperationCanceledException)
//             {
//                 // Server is shutting down
//                 break;
//             }
//             catch (ObjectDisposedException)
//             {
//                 // Socket closed
//                 break;
//             }
//             catch
//             {
//                 // Ignore other errors, continue accepting
//             }
//         }
//     }
//
//     // C++: ErrorOr<void> set_blocking(bool blocking)
//     public Result<object?> SetBlocking(bool blocking)
//     {
//         try
//         {
//             if (_socket == null)
//                 return Result<object?>.Failure(new ObjectDisposedException(nameof(TCPServer)));
//             // C++: int flags = TRY(Core::System::fcntl(m_fd, F_GETFL, 0));
//             //      if (blocking) ... else ...
//             _socket.Blocking = blocking;
//             return Result<object?>.Success(null);
//         }
//         catch (Exception ex)
//         {
//             return Result<object?>.Failure(ex);
//         }
//     }
//
//     // C++: ErrorOr<NonnullOwnPtr<TCPSocket>> accept()
//     public Result<TCPSocket> Accept()
//     {
//         lock (_acceptLock)
//         {
//             // C++: VERIFY(m_listening);
//             if (!_listening)
//                 return Result<TCPSocket>.Failure(new InvalidOperationException("Server is not listening"));
//
//             // C++: int accepted_fd = TRY(Core::System::accept4(...));
//             if (_pendingAcceptedSocket == null)
//                 return Result<TCPSocket>.Failure(new InvalidOperationException("No pending connection to accept"));
//
//             var socket = _pendingAcceptedSocket;
//             _pendingAcceptedSocket = null;
//             // C++: auto socket = TRY(TCPSocket::adopt_fd(accepted_fd));
//             return Result<TCPSocket>.Success(new TCPSocket(socket!));
//         }
//     }
//
//     // C++: Optional<IPv4Address> local_address() const
//     public IPv4Address? LocalAddress()
//     {
//         try
//         {
//             if (_socket == null)
//                 return null;
//             // C++: getsockname(m_fd, ...)
//             var endpoint = _socket.LocalEndPoint as IPEndPoint;
//             return endpoint != null ? new IPv4Address(endpoint.Address) : null;
//         }
//         catch
//         {
//             return null;
//         }
//     }
//
//     // C++: Optional<u16> local_port() const
//     public ushort? LocalPort()
//     {
//         try
//         {
//             if (_socket == null)
//                 return null;
//             // C++: getsockname(m_fd, ...)
//             var endpoint = _socket.LocalEndPoint as IPEndPoint;
//             return endpoint != null ? (ushort)endpoint.Port : (ushort?)null;
//         }
//         catch
//         {
//             return null;
//         }
//     }
// }