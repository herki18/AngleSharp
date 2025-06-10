// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/ThreadActor.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/master/Libraries/LibDevTools/Actors/ThreadActor.cpp

namespace LadyBird.Libraries.LibDevTools.Actors;

public sealed class ThreadActor : Actor
{
    public const string BaseName = "thread";

    // From C++: static NonnullRefPtr<ThreadActor> create(DevToolsServer&, String name)
    public static ThreadActor Create(DevToolsServer devtools, string name)
    {
        return new ThreadActor(devtools, name);
    }

    private ThreadActor(DevToolsServer devtools, string name)
        : base(devtools, name)
    {
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        SendUnrecognizedPacketTypeError(message);
    }
}