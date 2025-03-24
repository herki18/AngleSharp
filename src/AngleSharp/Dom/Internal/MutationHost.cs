namespace AngleSharp.Dom
{
    using AngleSharp.Browser;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Couples the mutation events to mutation observers and the event loop.
    /// </summary>
    sealed class MutationHost : IMutationHost
    {
        #region Fields

        private readonly List<IMutationObserver> _observers;
        private readonly IEventLoop _loop;
        private Boolean _queued;

        #endregion

        #region ctor

        public MutationHost(IEventLoop loop)
        {
            _observers = [];
            _queued = false;
            _loop = loop;
        }

        #endregion

        #region Properties

        public IEnumerable<IMutationObserver> Observers => _observers;

        #endregion

        #region Methods

        public void Register(IMutationObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        public void Unregister(IMutationObserver observer)
        {
            if (_observers.Contains(observer))
            {
                _observers.Remove(observer);
            }
        }

        public void ScheduleCallback()
        {
            if (!_queued)
            {
                _queued = true;
                _loop.Enqueue(DispatchCallback);
            }
        }

        private void DispatchCallback()
        {
            var observers = _observers.ToArray();
            _queued = false;

            foreach (var observer in observers)
            {
                _loop.Enqueue(((MutationObserver)observer).Trigger, TaskPriority.Microtask);
            }
        }

        #endregion
    }
}
