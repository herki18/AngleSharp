namespace AngleSharp.Dom.Events;

using Attributes;

[DomName("WindowEventHandlers")]
[DomNoInterfaceObject]
public interface IWindowEventHandlers
{
    [DomName("onafterprint")] event DomEventHandler Printed;

    [DomName("onbeforeprint")] event DomEventHandler Printing;

    [DomName("onbeforeunload")] event DomEventHandler Unloading;

    [DomName("onhashchange")] event DomEventHandler HashChanged;

    [DomName("onmessage")] event DomEventHandler MessageReceived;

    [DomName("onoffline")] event DomEventHandler WentOffline;

    [DomName("ononline")] event DomEventHandler WentOnline;

    [DomName("onpagehide")] event DomEventHandler PageHidden;

    [DomName("onpageshow")] event DomEventHandler PageShown;

    [DomName("onpopstate")] event DomEventHandler PopState;

    [DomName("onstorage")] event DomEventHandler Storage;

    [DomName("onunload")] event DomEventHandler Unloaded;
}