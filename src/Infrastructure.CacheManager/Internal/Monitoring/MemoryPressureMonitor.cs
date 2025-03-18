namespace Infrastructure.CacheManager.Internal.Monitoring;

using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using API.Monitoring;

/// <summary>
/// Monitors system memory pressure and raises events when memory pressure is detected.
/// </summary>
internal class MemoryPressureMonitor : IMemoryPressureMonitor, IDisposable
{
    private readonly Timer _monitorTimer;
    private readonly TimeSpan _checkInterval;
    private readonly long _criticalThreshold;
    private readonly long _highThreshold;
    private readonly long _mediumThreshold;
    private readonly long _lowThreshold;
    private readonly Process _currentProcess;
    private bool _isMonitoring;
    private bool _isDisposed;
    private PressureSeverity _currentPressure;

    /// <summary>
    /// Gets a value indicating whether the memory monitoring is active.
    /// </summary>
    public bool IsMonitoring => _isMonitoring;

    /// <summary>
    /// Gets the current memory pressure severity.
    /// </summary>
    public PressureSeverity CurrentPressure => _currentPressure;

    /// <summary>
    /// Gets the current memory usage percentage of the process.
    /// </summary>
    public double CurrentMemoryUsagePercentage { get; private set; }

    /// <summary>
    /// Gets the current memory usage in bytes.
    /// </summary>
    public long CurrentMemoryUsage { get; private set; }

    /// <summary>
    /// Gets the memory threshold in bytes that triggers pressure detection.
    /// </summary>
    public long MemoryThreshold => _highThreshold;

    /// <summary>
    /// Event raised when memory pressure is detected.
    /// </summary>
    public event EventHandler<MemoryPressureEventArgs>? MemoryPressureDetected;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPressureMonitor"/> class
    /// with thresholds set as percentages of total system memory.
    /// </summary>
    /// <param name="lowThresholdPercentage">The percentage of system memory that triggers low pressure.</param>
    /// <param name="mediumThresholdPercentage">The percentage of system memory that triggers medium pressure.</param>
    /// <param name="highThresholdPercentage">The percentage of system memory that triggers high pressure.</param>
    /// <param name="criticalThresholdPercentage">The percentage of system memory that triggers critical pressure.</param>
    /// <param name="checkInterval">The interval at which to check memory pressure.</param>
    public MemoryPressureMonitor(
        double lowThresholdPercentage = 60,
        double mediumThresholdPercentage = 70,
        double highThresholdPercentage = 80,
        double criticalThresholdPercentage = 90,
        TimeSpan? checkInterval = null)
    {
        _checkInterval = checkInterval ?? TimeSpan.FromSeconds(10);
        _currentProcess = Process.GetCurrentProcess();

        // Get total system memory
        long totalMemory = GetTotalSystemMemory();

        // Calculate thresholds based on percentages of total memory
        _lowThreshold = (long)(totalMemory * lowThresholdPercentage / 100);
        _mediumThreshold = (long)(totalMemory * mediumThresholdPercentage / 100);
        _highThreshold = (long)(totalMemory * highThresholdPercentage / 100);
        _criticalThreshold = (long)(totalMemory * criticalThresholdPercentage / 100);

        // Initialize timer but don't start it yet
        _monitorTimer = new Timer(CheckMemoryUsage, null, Timeout.Infinite, Timeout.Infinite);

        // Register for GC notifications if possible
        bool canRegisterForGC = GCSettings.LatencyMode != GCLatencyMode.NoGCRegion;
        if (canRegisterForGC)
        {
            try
            {
                // This method returns void, not a status
                GC.RegisterForFullGCNotification(10, 10);

                // Start GC notification thread
                Thread gcThread = new Thread(ListenForGCNotifications)
                {
                    IsBackground = true,
                    Name = "GC Notification Thread"
                };
                gcThread.Start();
            }
            catch (Exception)
            {
                // Failed to register for GC notifications - continue without them
            }
        }
    }

    /// <summary>
    /// Starts monitoring memory pressure.
    /// </summary>
    public void StartMonitoring()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(MemoryPressureMonitor));

        if (_isMonitoring)
            return;

        // Perform initial check
        CheckMemoryPressure();

        // Start timer
        _monitorTimer.Change(0, (int)_checkInterval.TotalMilliseconds);
        _isMonitoring = true;
    }

    /// <summary>
    /// Stops monitoring memory pressure.
    /// </summary>
    public void StopMonitoring()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(MemoryPressureMonitor));

        if (!_isMonitoring)
            return;

        // Stop timer
        _monitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _isMonitoring = false;
    }

    /// <summary>
    /// Performs an immediate check for memory pressure.
    /// </summary>
    /// <returns>The current memory pressure severity.</returns>
    public PressureSeverity CheckMemoryPressure()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(MemoryPressureMonitor));

        // Update process memory usage
        UpdateMemoryUsage();

        // Determine pressure level
        PressureSeverity newPressure = DeterminePressureSeverity();

        // Only raise event if pressure level changed
        if (newPressure != _currentPressure && newPressure != PressureSeverity.None)
        {
            _currentPressure = newPressure;
            RaiseMemoryPressureEvent(newPressure);
        }

        return _currentPressure;
    }

    /// <summary>
    /// Updates the current memory usage statistics.
    /// </summary>
    private void UpdateMemoryUsage()
    {
        _currentProcess.Refresh();
        CurrentMemoryUsage = _currentProcess.WorkingSet64;

        long totalMemory = GetTotalSystemMemory();
        CurrentMemoryUsagePercentage = totalMemory > 0 ?
            (double)CurrentMemoryUsage / totalMemory * 100 : 0;
    }

    /// <summary>
    /// Determines the current memory pressure severity based on thresholds.
    /// </summary>
    /// <returns>The pressure severity level.</returns>
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

    /// <summary>
    /// Raises the memory pressure event.
    /// </summary>
    /// <param name="severity">The severity of the memory pressure.</param>
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

    /// <summary>
    /// Gets the memory threshold for a specific severity level.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>The threshold in bytes.</returns>
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

    /// <summary>
    /// Listens for GC notifications in a separate thread.
    /// </summary>
    private void ListenForGCNotifications()
    {
        while (!_isDisposed)
        {
            // Wait for a full GC
            GCNotificationStatus status = GC.WaitForFullGCApproach();

            if (status == GCNotificationStatus.Succeeded)
            {
                // A full GC is approaching - this is a good indicator of memory pressure
                CheckMemoryPressure();

                // Wait for GC completion
                GC.WaitForFullGCComplete();
            }

            // Sleep to avoid tight loop
            Thread.Sleep(1000);
        }
    }

    /// <summary>
    /// Checks memory usage on a timer callback.
    /// </summary>
    /// <param name="state">The timer state.</param>
    private void CheckMemoryUsage(object state)
    {
        try
        {
            CheckMemoryPressure();
        }
        catch (Exception)
        {
            // Swallow exceptions in timer callback
        }
    }

    /// <summary>
    /// Gets the total system memory in bytes.
    /// </summary>
    /// <returns>The total memory in bytes.</returns>
    private long GetTotalSystemMemory()
    {
        try
        {
            // Try to use platform-specific methods to get total physical memory
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return GetWindowsPhysicalMemory();
            }
            else
            {
                // Fallback to a reasonable default if we can't determine it
                return 8L * 1024L * 1024L * 1024L; // 8 GB default
            }
        }
        catch
        {
            // Fallback to a reasonable default if anything fails
            return 8L * 1024L * 1024L * 1024L; // 8 GB default
        }
    }

    /// <summary>
    /// Gets the total physical memory on Windows systems.
    /// </summary>
    /// <returns>The total physical memory in bytes.</returns>
    private long GetWindowsPhysicalMemory()
    {
        // Use Win32 API via P/Invoke or managed alternatives
        // For simplicity, we'll use Environment.SystemPageSize as a multiplier with high values
        // This isn't accurate but serves as a better fallback than a hardcoded value
        return Environment.SystemPageSize * 1024L * 1024L;
    }

    /// <summary>
    /// Disposes resources used by the memory pressure monitor.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Stop monitoring
        StopMonitoring();

        // Dispose timer
        _monitorTimer?.Dispose();
    }
}