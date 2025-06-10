// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/Notifier.h
// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/Notifier.cpp
namespace LadyBird.Libraries.LibCore.Events;

using System;
using System.Threading;
using AK;

// Copyright (c) 2018-2023, Andreas Kling <andreas@ladybird.org>
//
// SPDX-License-Identifier: BSD-2-Clause

public sealed class Notifier : EventReceiver
{
    // C++: using Type = NotificationType;
    public NotificationType Type => _type;

    private int _fd = -1;
    private bool _isEnabled = false;
    private Thread? _ownerThread;
    private NotificationType _type = NotificationType.None;

    // C++: Function<void()> on_activation;
    public Action? OnActivation { get; set; }

    // C++: Notifier(int fd, Type type, EventReceiver* parent = nullptr);
    public Notifier(int fd, NotificationType type, EventReceiver? parent = null)
        : base(parent)
    {
        _fd = fd;
        _type = type;
        SetEnabled(true);
    }

    // C++: virtual ~Notifier() override;
    ~Notifier()
    {
        // C++: set_enabled(false);
        SetEnabled(false);
    }

    // C++: void set_enabled(bool enabled);
    public void SetEnabled(bool enabled)
    {
        if (_fd < 0)
            return;
        if (enabled == _isEnabled)
            return;
        _isEnabled = enabled;
        if (enabled)
            EventLoop.RegisterNotifier(Badge<Notifier>.Create(), this);
        else
            EventLoop.UnregisterNotifier(Badge<Notifier>.Create(), this);
    }

    // C++: void close();
    public void Close()
    {
        if (_fd < 0)
            return;
        SetEnabled(false);
        _fd = -1;
    }

    // C++: int fd() const { return m_fd; }
    public int Fd => _fd;

    // C++: void set_type(Type type);
    public void SetType(NotificationType type)
    {
        if (_isEnabled)
        {
            // FIXME: Directly communicate intent to the EventLoop.
            SetEnabled(false);
            _type = type;
            SetEnabled(true);
        }
        else
        {
            _type = type;
        }
    }

    // C++: void event(Core::Event&) override;
    public override void Event(Event evt)
    {
        if (evt.Type == (uint)Events.Event.EventType.NotifierActivation)
        {
            OnActivation?.Invoke();
            return;
        }
        base.Event(evt);
    }

    // C++: void set_owner_thread(pthread_t owner_thread) { m_owner_thread = owner_thread; }
    public void SetOwnerThread(Thread ownerThread) => _ownerThread = ownerThread;

    // C++: pthread_t owner_thread() const { return m_owner_thread; }
    public Thread? OwnerThread => _ownerThread;
}