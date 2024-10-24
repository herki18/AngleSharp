namespace AngleSharp.Dom
{
    using AngleSharp.Attributes;
    using AngleSharp.Dom.Events;
    using System;
    using ViewSync;

    /// <summary>
    /// EventTarget is a DOM interface implemented by objects that can receive
    /// DOM events and have listeners for them.
    /// </summary>
    [DomName("EventTarget")]
    public interface IEventTarget
    {
        /// <summary>
        /// Register an event handler of a specific event type on the
        /// EventTarget.
        /// </summary>
        /// <param name="type">
        /// A string representing the event type to listen for.
        /// </param>
        /// <param name="callback">
        /// The listener parameter indicates the EventListener function to be
        /// added.
        /// </param>
        /// <param name="capture">
        /// True indicates that the user wishes to initiate capture. After
        /// initiating capture, all events of the specified type will be
        /// dispatched to the registered listener before being dispatched to
        /// any EventTarget beneath it in the DOM tree. Events which are
        /// bubbling upward through the tree will not trigger a listener
        /// designated to use capture.
        /// </param>
        [DomName("addEventListener")]
        void AddEventListener(String type, DomEventHandler? callback = null, Boolean capture = false);

        /// <summary>
        /// Removes an event listener from the EventTarget.
        /// </summary>
        /// <param name="type">
        /// A string representing the event type being removed.
        /// </param>
        /// <param name="callback">
        /// The listener parameter indicates the EventListener function to be
        /// removed.
        /// </param>
        /// <param name="capture">
        /// Specifies whether the EventListener being removed was registered as
        /// a capturing listener or not.
        /// </param>
        [DomName("removeEventListener")]
        void RemoveEventListener(String type, DomEventHandler? callback = null, Boolean capture = false);

        /// <summary>
        /// Calls the listener registered for the given event.
        /// </summary>
        /// <param name="ev">The event that asks for the listeners.</param>
        void InvokeEventListener(Event ev);

        /// <summary>
        /// Dispatch an event to this EventTarget.
        /// </summary>
        /// <param name="ev">The event to dispatch.</param>
        /// <returns>
        /// False if at least one of the event handlers, which handled this
        /// event called preventDefault(). Otherwise true.
        /// </returns>
        [DomName("dispatchEvent")]
        Boolean Dispatch(Event ev);

        /// <summary>
        /// An event triggered when an event is synced (added) for synchronization purposes.
        /// </summary>
        event EventHandler<EventSyncedArgs>? EventSynced;

        /// <summary>
        /// An event triggered when an event is unregistered (removed) from synchronization.
        /// </summary>
        event EventHandler<EventUnregisteredArgs>? EventUnregistered;
    }

    /// <summary>
    /// Event arguments for the EventSynced event, providing the event type and callback.
    /// </summary>
    public class EventSyncedArgs : EventArgs
    {
        /// <summary>
        /// Gets the event type that was synced.
        /// </summary>
        public String EventType { get; }

        /// <summary>
        /// Gets the callback function associated with the synced event.
        /// </summary>
        public DomEventHandler Callback { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSyncedArgs"/> class.
        /// </summary>
        /// <param name="eventType">The event type that was synced.</param>
        /// <param name="callback">The callback function associated with the synced event.</param>
        public EventSyncedArgs(String eventType, DomEventHandler callback)
        {
            EventType = eventType;
            Callback = callback;
        }
    }

    /// <summary>
    /// Event arguments for the EventUnregistered event, providing the event type.
    /// </summary>
    public class EventUnregisteredArgs : EventArgs
    {
        /// <summary>
        /// Gets the event type that was unregistered.
        /// </summary>
        public string EventType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventUnregisteredArgs"/> class.
        /// </summary>
        /// <param name="eventType">The event type that was unregistered.</param>
        public EventUnregisteredArgs(String eventType)
        {
            EventType = eventType;
        }
    }
}
