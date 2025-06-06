// Main implementation here, using file-scoped namespace and C# naming conventions

namespace LadyBird.Libraries.LibCore;

using System;
using System.Net.Sockets;

// Copyright (c) 2018-2020, Andreas Kling <andreas@ladybird.org>
// Copyright (c) 2021, Sam Atkins <atkinssj@serenityos.org>
//
// SPDX-License-Identifier: BSD-2-Clause

public class TCPServer : EventReceiver
{
    // C++: C_OBJECT_ABSTRACT(TCPServer)
    // (No direct equivalent in C#; handled by inheritance and constructors)

    // C++: static ErrorOr<NonnullRefPtr<TCPServer>> try_create(EventReceiver* parent = nullptr);
    public static Result<TCPServer> TryCreate(EventReceiver parent = null)
    {
#if SOCK_NONBLOCK
        // C++: int fd = TRY(Core::System::socket(AF_INET, SOCK_STREAM | SOCK_NONBLOCK | SOCK_CLOEXEC, 0));
        var fdResult = SystemStub.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, true);
        if (fdResult.IsFailure)
            return Result<TCPServer>.Failure(fdResult.Error);
        var fd = fdResult.Value;
#else
        // C++: int fd = TRY(Core::System::socket(AF_INET, SOCK_STREAM, 0));
        var fdResult = SystemStub.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, false);
        if (fdResult.IsFailure)
            return Result<TCPServer>.Failure(fdResult.Error);
        var fd = fdResult.Value;
        // C++: int option = 1;
        // TRY(Core::System::ioctl(fd, FIONBIO, &option));
        // TRY(Core::System::fcntl(fd, F_SETFD, FD_CLOEXEC));
        var ioctlResult = SystemStub.Ioctl(fd, SystemStub.Fionbio, 1);
        if (ioctlResult.IsFailure)
            return Result<TCPServer>.Failure(ioctlResult.Error);
        var fcntlResult = SystemStub.Fcntl(fd, SystemStub.FdCloexec);
        if (fcntlResult.IsFailure)
            return Result<TCPServer>.Failure(fcntlResult.Error);
#endif

        // C++: return adopt_nonnull_ref_or_enomem(new (nothrow) TCPServer(fd, parent));
        var server = new TCPServer(fd, parent);
        return Result<TCPServer>.Success(server);
    }

    // C++: virtual ~TCPServer() override;
    public override void Dispose()
    {
        // C++: MUST(Core::System::close(m_fd));
        SystemStub.Close(_fd);
        base.Dispose();
    }

    public enum AllowAddressReuse
    {
        Yes,
        No,
    }

    // C++: bool is_listening() const { return m_listening; }
    public bool IsListening => _listening;

    // C++: ErrorOr<void> listen(IPv4Address const& address, u16 port, AllowAddressReuse = AllowAddressReuse::No);
    public Result<object> Listen(IPv4Address address, ushort port, AllowAddressReuse allowAddressReuse = AllowAddressReuse.No)
    {
        if (_listening)
            return Result<object>.Failure(new SocketException((int)SocketError.AddressAlreadyInUse));

        // C++: auto socket_address = SocketAddress(address, port);
        // auto in = socket_address.to_sockaddr_in();
        var socketAddress = new SocketAddressStub(address, port);
        var inAddr = socketAddress.ToSockAddrIn();

        if (allowAddressReuse == AllowAddressReuse.Yes)
        {
            // C++: int option = 1;
            // TRY(Core::System::setsockopt(m_fd, SOL_SOCKET, SO_REUSEADDR, &option, sizeof(option)));
            var setsockoptResult = SystemStub.SetSockOpt(_fd, SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            if (setsockoptResult.IsFailure)
                return Result<object>.Failure(setsockoptResult.Error);
        }

        // C++: TRY(Core::System::bind(m_fd, (sockaddr const*)&in, sizeof(in)));
        var bindResult = SystemStub.Bind(_fd, inAddr);
        if (bindResult.IsFailure)
            return Result<object>.Failure(bindResult.Error);

        // C++: TRY(Core::System::listen(m_fd, 5));
        var listenResult = SystemStub.Listen(_fd, 5);
        if (listenResult.IsFailure)
            return Result<object>.Failure(listenResult.Error);

        _listening = true;

        // C++: m_notifier = Notifier::construct(m_fd, Notifier::Type::Read, this);
        _notifier = Notifier.Construct(_fd, NotifierType.Read, this);

        // C++: m_notifier->on_activation = [this] { if (on_ready_to_accept) on_ready_to_accept(); };
        _notifier.OnActivation = () =>
        {
            OnReadyToAccept?.Invoke();
        };

        return Result<object>.Success(null);
    }

    // C++: ErrorOr<void> set_blocking(bool blocking);
    public Result<object> SetBlocking(bool blocking)
    {
        // C++: int flags = TRY(Core::System::fcntl(m_fd, F_GETFL, 0));
        var flagsResult = SystemStub.FcntlGetFlags(_fd);
        if (flagsResult.IsFailure)
            return Result<object>.Failure(flagsResult.Error);
        var flags = flagsResult.Value;

        if (blocking)
        {
            // C++: TRY(Core::System::fcntl(m_fd, F_SETFL, flags & ~O_NONBLOCK));
            var setResult = SystemStub.FcntlSetFlags(_fd, flags & ~SystemStub.O_Nonblock);
            if (setResult.IsFailure)
                return Result<object>.Failure(setResult.Error);
        }
        else
        {
            // C++: TRY(Core::System::fcntl(m_fd, F_SETFL, flags | O_NONBLOCK));
            var setResult = SystemStub.FcntlSetFlags(_fd, flags | SystemStub.O_Nonblock);
            if (setResult.IsFailure)
                return Result<object>.Failure(setResult.Error);
        }
        return Result<object>.Success(null);
    }

    // C++: ErrorOr<NonnullOwnPtr<TCPSocket>> accept();
    public Result<TCPSocket> Accept()
    {
        if (!_listening)
            throw new InvalidOperationException("Server is not listening");

        // C++: sockaddr_in in;
        // socklen_t in_size = sizeof(in);
        var inAddr = new SockAddrInStub();
        var inSize = inAddr.Size;

#if !defined(AK_OS_MACOS) && !defined(AK_OS_IOS) && !defined(AK_OS_HAIKU)
        // C++: int accepted_fd = TRY(Core::System::accept4(m_fd, (sockaddr*)&in, &in_size, SOCK_NONBLOCK | SOCK_CLOEXEC));
        var acceptResult = SystemStub.Accept4(_fd, inAddr, inSize, SystemStub.SockNonblock | SystemStub.SockCloexec);
        if (acceptResult.IsFailure)
            return Result<TCPSocket>.Failure(acceptResult.Error);
        var acceptedFd = acceptResult.Value;
#else
        // C++: int accepted_fd = TRY(Core::System::accept(m_fd, (sockaddr*)&in, &in_size));
        var acceptResult = SystemStub.Accept(_fd, inAddr, inSize);
        if (acceptResult.IsFailure)
            return Result<TCPSocket>.Failure(acceptResult.Error);
        var acceptedFd = acceptResult.Value;
#endif

        // C++: auto socket = TRY(TCPSocket::adopt_fd(accepted_fd));
        var socketResult = TCPSocket.AdoptFd(acceptedFd);
        if (socketResult.IsFailure)
            return Result<TCPSocket>.Failure(socketResult.Error);
        var socket = socketResult.Value;

#if defined(AK_OS_MACOS) || defined(AK_OS_IOS) || defined(AK_OS_HAIKU)
        // FIXME: Ideally, we should let the caller decide whether it wants the
        //        socket to be nonblocking or not, but there are currently places
        //        which depend on this.
        var setBlockingResult = socket.SetBlocking(false);
        if (setBlockingResult.IsFailure)
            return Result<TCPSocket>.Failure(setBlockingResult.Error);
        var setCloseOnExecResult = socket.SetCloseOnExec(true);
        if (setCloseOnExecResult.IsFailure)
            return Result<TCPSocket>.Failure(setCloseOnExecResult.Error);
#endif

        return Result<TCPSocket>.Success(socket);
    }

    // C++: Optional<IPv4Address> local_address() const;
    public IPv4Address? LocalAddress()
    {
        if (_fd == -1)
            return null;

        var address = new SockAddrInStub();
        var len = address.Size;
        if (!SystemStub.GetSockName(_fd, address, ref len))
            return null;

        return new IPv4Address(address.SinAddrSAddr);
    }

    // C++: Optional<u16> local_port() const;
    public ushort? LocalPort()
    {
        if (_fd == -1)
            return null;

        var address = new SockAddrInStub();
        var len = address.Size;
        if (!SystemStub.GetSockName(_fd, address, ref len))
            return null;

        return SystemStub.Ntohs(address.SinPort);
    }

    // C++: Function<void()> on_ready_to_accept;
    public Action OnReadyToAccept { get; set; }

    // C++: explicit TCPServer(int fd, EventReceiver* parent = nullptr);
    private TCPServer(int fd, EventReceiver parent = null)
        : base(parent)
    {
        _fd = fd;
        if (_fd < 0)
            throw new ArgumentException("File descriptor must be >= 0", nameof(fd));
    }

    // C++: int m_fd { -1 };
    private int _fd = -1;

    // C++: bool m_listening { false };
    private bool _listening = false;

    // C++: RefPtr<Notifier> m_notifier;
    private Notifier _notifier;
}