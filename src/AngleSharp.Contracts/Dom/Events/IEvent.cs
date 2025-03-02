namespace AngleSharp.Dom.Events;

using System;
using System.Collections.Generic;
using Attributes;

[DomName("Event")]
public interface IEvent
{
    [DomName("type")] String Type { get; }

    [DomName("target")] IEventTarget? OriginalTarget { get; }

    [DomName("currentTarget")] IEventTarget? CurrentTarget { get; }

    [DomName("eventPhase")] EventPhase Phase { get; }

    /// <summary>
    ///     Gets if the event is propagating across the shadow DOM boundary into the standard DOM.
    /// </summary>
    [DomName("composed")]
    Boolean IsComposed { get; }

    [DomName("bubbles")] Boolean IsBubbling { get; }

    [DomName("cancelable")] Boolean IsCancelable { get; }

    [DomName("defaultPrevented")] Boolean IsDefaultPrevented { get; }

    [DomName("isTrusted")] Boolean IsTrusted { get; }

    [DomName("timeStamp")] DateTime Time { get; }

    ///Returns the event's path which is an array of the objects on which listeners will be invoked.
    ///     See https://dom.spec.whatwg.org/#dom-event-composedpath.
    /// </summary>
    [DomName("composedPath")]
    IEnumerable<IEventTarget> GetComposedPath();

    [DomName("stopPropagation")]
    void Stop();

    [DomName("stopImmediatePropagation")]
    void StopImmediately();

    [DomName("preventDefault")]
    void Cancel();

    /// <summary>
    ///     Initializes the event.
    /// </summary>
    /// <param name="type">The type of the event.</param>
    /// <param name="bubbles">If the event is bubbling.</param>
    /// <param name="cancelable">If the event is cancelable.</param>
    [DomName("initEvent")]
    void Init(String type, Boolean bubbles, Boolean cancelable);
}