namespace AngleSharp.Dom.Events;

using AngleSharp.Attributes;
using System;
using System.Collections.Generic;
using Html;

/// <summary>
/// Represents an event argument in the DOM event model.
/// This interface defines the standard properties and methods for all DOM events
/// according to the W3C DOM specification.
/// </summary>
[DomName("Event")]
public interface IEvent
{
    /// <summary>
    /// Gets the associated flags that describe the current state of the event.
    /// </summary>
    EventFlags Flags { get; }

    /// <summary>
    /// Gets the type of event, which is a case-sensitive string representing the name of the event.
    /// </summary>
    [DomName("type")]
    String Type { get; }

    /// <summary>
    /// Gets the original target of the event, which is the event target to which the event was originally dispatched.
    /// </summary>
    [DomName("target")]
    IEventTarget? OriginalTarget { get; }

    /// <summary>
    /// Gets the current target, which is the object to which the event listener is currently attached
    /// as the event traverses the DOM during the propagation phase.
    /// </summary>
    [DomName("currentTarget")]
    IEventTarget? CurrentTarget { get; }

    /// <summary>
    /// Gets the phase of the event, which indicates which phase of the event flow is currently being processed
    /// (capturing, at target, or bubbling).
    /// </summary>
    [DomName("eventPhase")]
    EventPhase Phase { get; }

    /// <summary>
    /// Gets if the event is propagating across the shadow DOM boundary into the standard DOM.
    /// When true, the event can propagate across the shadow DOM boundary into the standard DOM.
    /// </summary>
    [DomName("composed")]
    Boolean IsComposed { get; }

    /// <summary>
    /// Gets if the event is actually bubbling. When true, the event propagates
    /// from the target element up through its ancestors.
    /// </summary>
    [DomName("bubbles")]
    Boolean IsBubbling { get; }

    /// <summary>
    /// Gets if the event is cancelable. When true, the default action of the event
    /// can be prevented by calling the Cancel() method.
    /// </summary>
    [DomName("cancelable")]
    Boolean IsCancelable { get; }

    /// <summary>
    /// Gets if the default behavior has been prevented by calling the Cancel() method.
    /// </summary>
    [DomName("defaultPrevented")]
    Boolean IsDefaultPrevented { get; }

    /// <summary>
    /// Gets if the event is trusted. Events created by the user agent are trusted,
    /// while events created via script are not.
    /// </summary>
    [DomName("isTrusted")]
    Boolean IsTrusted { get; set; }

    /// <summary>
    /// Gets the originating timestamp when the event was created,
    /// measured in milliseconds since the epoch.
    /// </summary>
    [DomName("timeStamp")]
    DateTime Time { get; }

    /// <summary>
    /// Returns the event's path which is an array of the objects on which listeners will be invoked.
    /// This method returns the full path the event follows during its propagation, including
    /// objects in shadow trees if the event propagates through them.
    /// See https://dom.spec.whatwg.org/#dom-event-composedpath.
    /// </summary>
    /// <returns>An enumerable collection of event targets along the propagation path.</returns>
    [DomName("composedPath")]
    IEnumerable<IEventTarget> GetComposedPath();

    /// <summary>
    /// Prevents further propagation of the event in the capturing and bubbling phases.
    /// Once called, the event will continue to propagate to listeners on the current target,
    /// but not to any other objects.
    /// </summary>
    [DomName("stopPropagation")]
    void Stop();

    /// <summary>
    /// Stops the immediate propagation of the event, preventing any other event listeners
    /// from being called, including those on the current target.
    /// This is a stronger version of Stop() that prevents remaining event listeners
    /// on the current target from being called.
    /// </summary>
    [DomName("stopImmediatePropagation")]
    void StopImmediately();

    /// <summary>
    /// Prevents the default behavior associated with the event from occurring.
    /// This only works if the event is cancelable (IsCancelable returns true).
    /// For example, this can be used to prevent a form submission or a link navigation.
    /// </summary>
    [DomName("preventDefault")]
    void Cancel();

    /// <summary>
    /// Initializes the event with the specified parameters.
    /// This method can be called before the event is dispatched to configure
    /// its basic properties.
    /// </summary>
    /// <param name="type">The type of the event (e.g., "click", "load", "keydown").</param>
    /// <param name="bubbles">If true, the event bubbles up through the DOM tree.</param>
    /// <param name="cancelable">If true, the default action can be prevented.</param>
    [DomName("initEvent")]
    void Init(String type, Boolean bubbles, Boolean cancelable);

    /// <summary>
    /// Dispatches the event to the specified target according to the DOM event propagation rules.
    /// The event goes through capturing phase, target phase, and bubbling phase if bubbling is enabled.
    /// Implementation follows the W3C specification for event dispatch.
    /// See https://dom.spec.whatwg.org/#dispatching-events
    /// </summary>
    /// <param name="target">The target DOM element to which the event will be dispatched.</param>
    /// <returns>Returns false if the event was canceled during propagation (by calling Cancel()), otherwise true.</returns>
    Boolean Dispatch(IEventTarget target);
}