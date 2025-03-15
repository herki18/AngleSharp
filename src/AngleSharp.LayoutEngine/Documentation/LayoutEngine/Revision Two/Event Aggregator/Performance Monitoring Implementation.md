```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using AngleSharp.StyleSystem.Events;
using AngleSharp.StyleSystem.Interfaces;

namespace AngleSharp.StyleSystem.Monitoring
{
    /// <summary>
    /// A performance monitor for the style system that uses the EventAggregator
    /// </summary>
    public class StyleSystemPerformanceMonitor : IDisposable
    {
        private readonly IStyleEventAggregator _eventAggregator;
        private readonly Dictionary<string, Stopwatch> _timers = new Dictionary<string, Stopwatch>();
        private readonly Dictionary<string, List<TimeSpan>> _measurements = new Dictionary<string, List<TimeSpan>>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        
        public StyleSystemPerformanceMonitor(IStyleEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            Initialize();
        }
        
        private void Initialize()
        {
            // Create timers
            _timers["StyleComputation"] = new Stopwatch();
            _timers["StyleInvalidation"] = new Stopwatch();
            _timers["DocumentLifecycle"] = new Stopwatch();
            
            // Initialize measurement lists
            _measurements["StyleComputation"] = new List<TimeSpan>();
            _measurements["StyleInvalidation"] = new List<TimeSpan>();
            _measurements["DocumentLifecycle"] = new List<TimeSpan>();
            
            // Subscribe to events
            
            // Style computation events
            _subscriptions.Add(_eventAggregator.Subscribe<StyleComputedEvent>(evt => {
                StartTimer("StyleComputation");
            }));
            
            _subscriptions.Add(_eventAggregator.Subscribe<SubtreeStylesUpdatedEvent>(evt => {
                StopTimerAndRecord("StyleComputation");
            }));
            
            // Style invalidation events
            _subscriptions.Add(_eventAggregator.Subscribe<ElementInvalidatedEvent>(evt => {
                StartTimer("StyleInvalidation");
            }));
            
            _subscriptions.Add(_eventAggregator.Subscribe<SubtreeInvalidatedEvent>(evt => {
                StopTimerAndRecord("StyleInvalidation");
            }));
            
            // Document lifecycle events
            _subscriptions.Add(_eventAggregator.Subscribe<DocumentAttachedEvent>(evt => {
                StartTimer("DocumentLifecycle");
            }));
            
            _subscriptions.Add(_eventAggregator.Subscribe<ReadyStateChangedEvent>(evt => {
                if (evt.ReadyState == Dom.DocumentReadyState.Complete)
                {
                    StopTimerAndRecord("DocumentLifecycle");
                }
            }));
        }
        
        private void StartTimer(string key)
        {
            if (_timers.TryGetValue(key, out var timer) && !timer.IsRunning)
            {
                timer.Start();
            }
        }
        
        private void StopTimerAndRecord(string key)
        {
            if (_timers.TryGetValue(key, out var timer) && timer.IsRunning)
            {
                timer.Stop();
                
                if (_measurements.TryGetValue(key, out var measurements))
                {
                    measurements.Add(timer.Elapsed);
                }
                
                timer.Reset();
            }
        }
        
        /// <summary>
        /// Gets the average time for a specific operation
        /// </summary>
        public TimeSpan GetAverageTime(string operation)
        {
            if (_measurements.TryGetValue(operation, out var measurements) && measurements.Count > 0)
            {
                var total = TimeSpan.Zero;
                foreach (var measurement in measurements)
                {
                    total += measurement;
                }
                
                return TimeSpan.FromTicks(total.Ticks / measurements.Count);
            }
            
            return TimeSpan.Zero;
        }
        
        /// <summary>
        /// Gets the total time spent on a specific operation
        /// </summary>
        public TimeSpan GetTotalTime(string operation)
        {
            if (_measurements.TryGetValue(operation, out var measurements) && measurements.Count > 0)
            {
                var total = TimeSpan.Zero;
                foreach (var measurement in measurements)
                {
                    total += measurement;
                }
                
                return total;
            }
            
            return TimeSpan.Zero;
        }
        
        /// <summary>
        /// Gets the number of measurements for a specific operation
        /// </summary>
        public int GetMeasurementCount(string operation)
        {
            if (_measurements.TryGetValue(operation, out var measurements))
            {
                return measurements.Count;
            }
            
            return 0;
        }
        
        /// <summary>
        /// Clears all measurements
        /// </summary>
        public void Reset()
        {
            foreach (var key in _measurements.Keys)
            {
                _measurements[key].Clear();
                _timers[key].Reset();
            }
        }
        
        /// <summary>
        /// Gets a performance report as a string
        /// </summary>
        public string GetReport()
        {
            var report = "StyleSystem Performance Report\n";
            report += "-----------------------------\n";
            
            foreach (var key in _measurements.Keys)
            {
                var count = GetMeasurementCount(key);
                var total = GetTotalTime(key);
                var average = GetAverageTime(key);
                
                report += $"{key}:\n";
                report += $"  Count: {count}\n";
                report += $"  Total: {total.TotalMilliseconds:F2}ms\n";
                report += $"  Average: {average.TotalMilliseconds:F2}ms\n";
            }
            
            return report;
        }
        
        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            
            _subscriptions.Clear();
        }
    }
}
```