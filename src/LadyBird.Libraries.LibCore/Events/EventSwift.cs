// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/EventSwift.h

namespace LadyBird.Libraries.LibCore.Events;

using System;

// Swift interop support - in C# we just use Action delegates
public static class EventSwift
{
    public static void DeferredInvokeBlock(EventLoop eventLoop, Action invokee)
    {
        eventLoop.DeferredInvoke(invokee);
    }
}