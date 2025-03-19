using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.CacheManager.API.Monitoring;

namespace Infrastructure.CacheManager.Internal.Monitoring
{
    /// <summary>
    /// Monitors memory pressure in the application and raises events when thresholds are exceeded.
    /// Optimized for use with the MemoryCache-based caching system.
    /// </summary>
    internal sealed class MemoryPressureMonitor : IMemoryPressureMonitor, IDisposable
    {
        private readonly Timer _monitorTimer;
        private readonly TimeSpan _checkInterval;
        private readonly long _criticalThreshold;
        private readonly long _highThreshold;
        private readonly long _mediumThreshold;
        private readonly long _lowThreshold;
        private readonly Process _currentProcess;
        private readonly Task? _gcNotificationTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isMonitoring;
        private bool _isDisposed;
        private PressureSeverity _currentPressure;

        public bool IsMonitoring => _isMonitoring;
        public PressureSeverity CurrentPressure => _currentPressure;
        public double CurrentMemoryUsagePercentage { get; private set; }
        public long CurrentMemoryUsage { get; private set; }
        public long MemoryThreshold => _highThreshold;

        public event EventHandler<MemoryPressureEventArgs>? MemoryPressureDetected;

        public MemoryPressureMonitor(
            double lowThresholdPercentage = 60,
            double mediumThresholdPercentage = 70,
            double highThresholdPercentage = 80,
            double criticalThresholdPercentage = 90,
            TimeSpan? checkInterval = null)
        {
            _checkInterval = checkInterval ?? TimeSpan.FromSeconds(10);
            _currentProcess = Process.GetCurrentProcess();
            long totalMemory = GetTotalSystemMemory();

            // Calculate thresholds
            _lowThreshold = (long)(totalMemory * lowThresholdPercentage / 100);
            _mediumThreshold = (long)(totalMemory * mediumThresholdPercentage / 100);
            _highThreshold = (long)(totalMemory * highThresholdPercentage / 100);
            _criticalThreshold = (long)(totalMemory * criticalThresholdPercentage / 100);

            // Initialize the timer but don't start it
            _monitorTimer = new Timer(CheckMemoryUsage, null, Timeout.Infinite, Timeout.Infinite);

            // Set up GC notification if available
            _cancellationTokenSource = new CancellationTokenSource();
            if (GCSettings.LatencyMode != GCLatencyMode.NoGCRegion)
            {
                GC.RegisterForFullGCNotification(10, 10);
                bool registered = true;
                if (registered)
                {
                    _gcNotificationTask = Task.Run(() => ListenForGCNotifications(_cancellationTokenSource.Token));
                }
            }
        }

        public void StartMonitoring()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MemoryPressureMonitor));

            if (_isMonitoring)
                return;

            // Perform an initial check of the memory pressure
            CheckMemoryPressure();

            // Start the timer to check periodically
            _monitorTimer.Change(0, (int)_checkInterval.TotalMilliseconds);
            _isMonitoring = true;
        }

        public void StopMonitoring()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MemoryPressureMonitor));

            if (!_isMonitoring)
                return;

            // Stop the timer
            _monitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _isMonitoring = false;
        }

        public PressureSeverity CheckMemoryPressure()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MemoryPressureMonitor));

            UpdateMemoryUsage();
            PressureSeverity newPressure = DeterminePressureSeverity();

            // Only raise an event if the pressure has changed and isn't None
            if (newPressure != _currentPressure && newPressure != PressureSeverity.None)
            {
                _currentPressure = newPressure;
                RaiseMemoryPressureEvent(newPressure);
            }

            return _currentPressure;
        }

        private void UpdateMemoryUsage()
        {
            // Refresh the process information to get updated memory usage
            _currentProcess.Refresh();
            CurrentMemoryUsage = _currentProcess.WorkingSet64;

            // Calculate percentage
            long totalMemory = GetTotalSystemMemory();
            CurrentMemoryUsagePercentage = totalMemory > 0 ?
                (double)CurrentMemoryUsage / totalMemory * 100 : 0;
        }

        private PressureSeverity DeterminePressureSeverity()
        {
            if (CurrentMemoryUsage >= _criticalThreshold)
                return PressureSeverity.Critical;

            if (CurrentMemoryUsage >= _highThreshold)
                return PressureSeverity.High;

            if (CurrentMemoryUsage >= _mediumThreshold)
                return PressureSeverity.Medium;

            if (CurrentMemoryUsage >= _lowThreshold)
                return PressureSeverity.Low;

            return PressureSeverity.None;
        }

        private void RaiseMemoryPressureEvent(PressureSeverity severity)
        {
            var threshold = GetThresholdForSeverity(severity);
            var args = new MemoryPressureEventArgs(
                severity,
                CurrentMemoryUsage,
                threshold,
                CurrentMemoryUsagePercentage);

            MemoryPressureDetected?.Invoke(this, args);
        }

        private long GetThresholdForSeverity(PressureSeverity severity)
        {
            switch (severity)
            {
                case PressureSeverity.Critical:
                    return _criticalThreshold;
                case PressureSeverity.High:
                    return _highThreshold;
                case PressureSeverity.Medium:
                    return _mediumThreshold;
                case PressureSeverity.Low:
                    return _lowThreshold;
                default:
                    return 0;
            }
        }

        private void ListenForGCNotifications(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !_isDisposed)
            {
                try
                {
                    // Wait for a full GC approach notification
                    GCNotificationStatus status = GC.WaitForFullGCApproach();

                    if (status == GCNotificationStatus.Succeeded)
                    {
                        // A full GC is approaching, check memory pressure
                        CheckMemoryPressure();

                        // Wait for the full GC to complete
                        GC.WaitForFullGCComplete();
                    }

                    // Pause to prevent CPU spinning
                    Task.Delay(1000, cancellationToken).Wait(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Cancellation was requested
                    break;
                }
                catch (Exception)
                {
                    // Ignore exceptions and continue listening
                    Task.Delay(1000, cancellationToken).Wait(cancellationToken);
                }
            }
        }

        private void CheckMemoryUsage(object? state)
        {
            try
            {
                // Check current memory pressure
                CheckMemoryPressure();
            }
            catch (Exception)
            {
                // Swallow exceptions in the timer callback
            }
        }

        private long GetTotalSystemMemory()
        {
            return Environment.WorkingSet;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Only call StopMonitoring if we're currently monitoring
            if (_isMonitoring)
            {
                _monitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _isMonitoring = false;
            }

            _cancellationTokenSource?.Cancel();
            _monitorTimer?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

    }
}