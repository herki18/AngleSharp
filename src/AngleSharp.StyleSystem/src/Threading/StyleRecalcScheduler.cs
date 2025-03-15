namespace AngleSharp.StyleSystem.Threading;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Events;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Tasks;

public class StyleRecalcScheduler : IStyleRecalcScheduler, IDisposable
{
    private readonly IBrowsingContext _context;
    private readonly IStyleEngine _styleEngine;
    private readonly IStyleTaskScheduler _taskScheduler;
    private readonly IMainThreadStyleWork? _mainThreadWork;
    private readonly IWorkerThreadStylePool? _workerThreadPool;
    private readonly ViewportDetector _viewportDetector;
    private readonly object _processingLock = new();
    private readonly ConcurrentDictionary<IElement, StyleRecalcWork> _pendingWork = new();
    private readonly SemaphoreSlim _workSemaphore = new(1, 1);
    private readonly List<IElement> _deviceDependentElements = new();
    private readonly object _configLock = new();
    private readonly ISubscriptionToken[] _subscriptionTokens;

    private int _throttleIntervalMs = 16;
    private int _maxBatchSize = 100;
    private bool _useWorkerThreads = true;
    private Timer? _throttleTimer;
    private Timer? _frameEndTimer;
    private CancellationTokenSource? _processingCts;
    private bool _isProcessing;
    private Action? _completionCallback;
    private bool _isDisposed;
    private int _maxProcessingTime = 5;

    public StyleRecalcScheduler(
        IStyleEngine styleEngine,
        IBrowsingContext context,
        IStyleTaskScheduler taskScheduler,
        IEventAggregator eventAggregator,
        IMainThreadStyleWork? mainThreadWork = null,
        IWorkerThreadStylePool? workerThreadPool = null,
        int throttleIntervalMs = 16,
        int maxBatchSize = 100)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        _mainThreadWork = mainThreadWork;
        _workerThreadPool = workerThreadPool;
        _throttleIntervalMs = throttleIntervalMs;
        _maxBatchSize = maxBatchSize;
        _viewportDetector = new ViewportDetector(_styleEngine.RenderDevice);

        // Subscribe to events
        _subscriptionTokens = new ISubscriptionToken[]
        {
            eventAggregator.Subscribe<ElementInvalidatedEvent>(OnElementInvalidated),
            eventAggregator.Subscribe<PropertiesInvalidatedEvent>(OnPropertiesInvalidated),
            eventAggregator.Subscribe<SubtreeInvalidatedEvent>(OnSubtreeInvalidated),
            eventAggregator.Subscribe<DeviceDependentElementsInvalidatedEvent>(OnDeviceDependentElementsInvalidated)
        };
    }

    #region IStyleRecalcScheduler Implementation
    public int ThrottleIntervalMs
    {
        get => _throttleIntervalMs;
        set
        {
            lock (_configLock)
            {
                _throttleIntervalMs = Math.Max(1, value);
            }
        }
    }

    public int MaxBatchSize
    {
        get => _maxBatchSize;
        set
        {
            lock (_configLock)
            {
                _maxBatchSize = Math.Max(1, value);
            }
        }
    }

    public bool UseWorkerThreads
    {
        get => _useWorkerThreads;
        set
        {
            lock (_configLock)
            {
                _useWorkerThreads = value;
            }
        }
    }

    public void ScheduleElementRecalc(IElement element, RecalcPriority priority = RecalcPriority.Normal)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        var work = new StyleRecalcWork(element, priority, StyleWorkType.Element);
        _pendingWork.AddOrUpdate(element, work, (_, existing) =>
            new StyleRecalcWork(element,
                (RecalcPriority)Math.Max((byte)existing.Priority, (byte)priority),
                existing.WorkType == StyleWorkType.Subtree ? StyleWorkType.Subtree : StyleWorkType.Element));
        ScheduleProcessing();
    }

    public void ScheduleSubtreeRecalc(IElement rootElement, RecalcPriority priority = RecalcPriority.Normal)
    {
        if (rootElement == null)
            throw new ArgumentNullException(nameof(rootElement));
        var work = new StyleRecalcWork(rootElement, priority, StyleWorkType.Subtree);
        _pendingWork.AddOrUpdate(rootElement, work, (_, existing) =>
            new StyleRecalcWork(rootElement,
                (RecalcPriority)Math.Max((byte)existing.Priority, (byte)priority),
                StyleWorkType.Subtree));
        ScheduleProcessing();
    }

    public void ProcessImmediately()
    {
        lock (_processingLock)
        {
            if (_isProcessing)
                return;
            _isProcessing = true;
            _processingCts = new CancellationTokenSource();
        }
        try
        {
            ProcessPendingWork(_processingCts.Token);
            _completionCallback?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing style work: {ex.Message}");
        }
        finally
        {
            lock (_processingLock)
            {
                _isProcessing = false;
                _processingCts?.Dispose();
                _processingCts = null;
            }
        }
    }

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await _workSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CancellationTokenSource? linkedCts = null;
            lock (_processingLock)
            {
                if (_isProcessing)
                    return;
                _isProcessing = true;
                _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linkedCts = _processingCts;
            }
            try
            {
                await Task.Run(() => ProcessPendingWork(linkedCts.Token), linkedCts.Token)
                    .ConfigureAwait(false);
                _completionCallback?.Invoke();
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    Console.WriteLine($"Error processing style work: {ex.Message}");
                }
            }
            finally
            {
                lock (_processingLock)
                {
                    _isProcessing = false;
                    linkedCts?.Dispose();
                    _processingCts = null;
                }
            }
        }
        finally
        {
            _workSemaphore.Release();
        }
    }

    public void CancelPendingWork()
    {
        lock (_processingLock)
        {
            _processingCts?.Cancel();
        }
        _throttleTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _frameEndTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public bool HasPendingWork =>
        _pendingWork.Count > 0 ||
        (_mainThreadWork?.HasPendingWork ?? false) ||
        (_workerThreadPool?.HasPendingWork ?? false) ||
        _taskScheduler.HasPendingTasks;

    public void FlushPendingStylesAtEndOfFrame()
    {
        if (_frameEndTimer == null)
        {
            _frameEndTimer = new Timer(
                _ => ProcessImmediately(),
                null,
                _throttleIntervalMs,
                Timeout.Infinite);
        }
        else
        {
            _frameEndTimer.Change(_throttleIntervalMs, Timeout.Infinite);
        }
    }

    public void SetCompletionCallback(Action callback)
    {
        _completionCallback = callback ?? throw new ArgumentNullException(nameof(callback));
    }
    #endregion

    #region Event Handlers
    private void OnElementInvalidated(ElementInvalidatedEvent eventData)
    {
        RecalcPriority priority = DeterminePriority(eventData.Element);
        ScheduleElementRecalc(eventData.Element, priority);
    }

    private void OnPropertiesInvalidated(PropertiesInvalidatedEvent eventData)
    {
        RecalcPriority priority = DeterminePriority(eventData.Element);
        bool hasLayoutProperties = eventData.Properties.Any(IsLayoutProperty);
        bool hasVisualProperties = eventData.Properties.Any(IsVisualProperty);

        if (hasLayoutProperties)
        {
            priority = (RecalcPriority)Math.Max((byte)priority, (byte)RecalcPriority.High);
        }

        ScheduleElementRecalc(eventData.Element, priority);

        if (hasLayoutProperties && CouldAffectChildren(eventData.Element, eventData.Properties))
        {
            ScheduleSubtreeRecalc(eventData.Element, priority);
        }
    }

    private void OnSubtreeInvalidated(SubtreeInvalidatedEvent eventData)
    {
        RecalcPriority priority = DeterminePriority(eventData.RootElement);
        ScheduleSubtreeRecalc(eventData.RootElement, priority);
    }

    private void OnDeviceDependentElementsInvalidated(DeviceDependentElementsInvalidatedEvent eventData)
    {
        foreach (var element in _deviceDependentElements)
        {
            ScheduleElementRecalc(element, RecalcPriority.High);
        }

        if (_context.Active?.DocumentElement != null)
        {
            ScheduleSubtreeRecalc(_context.Active.DocumentElement, RecalcPriority.High);
        }
    }
    #endregion

    #region Processing Logic
    private void ProcessPendingWork(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var timeLimit = TimeSpan.FromMilliseconds(_maxProcessingTime);
        try
        {
            ProcessElementsByPriority(RecalcPriority.Critical, cancellationToken);
            if (!cancellationToken.IsCancellationRequested &&
                DateTime.UtcNow - startTime < timeLimit)
            {
                ProcessElementsByPriority(RecalcPriority.High, cancellationToken);
            }
            if (!cancellationToken.IsCancellationRequested &&
                DateTime.UtcNow - startTime < timeLimit)
            {
                ProcessElementsByPriority(RecalcPriority.Normal, cancellationToken);
            }
            if (!cancellationToken.IsCancellationRequested &&
                DateTime.UtcNow - startTime < timeLimit)
            {
                ProcessElementsByPriority(RecalcPriority.Low, cancellationToken);
            }
            if (_pendingWork.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                ScheduleProcessing();
            }
            if (_workerThreadPool != null && _workerThreadPool.HasPendingWork &&
                !cancellationToken.IsCancellationRequested)
            {
                _workerThreadPool.StartProcessingAsync(cancellationToken).Wait(cancellationToken);
                _workerThreadPool.SynchronizeResultsAsync(cancellationToken).Wait(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ProcessElementsByPriority(RecalcPriority priority, CancellationToken cancellationToken)
    {
        var elements = _pendingWork.Values
            .Where(w => w.Priority == priority)
            .OrderBy(GetElementSortOrder)
            .Take(_maxBatchSize)
            .ToList();
        if (elements.Count == 0)
            return;
        foreach (var work in elements)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            _pendingWork.TryRemove(work.Element, out _);
            if (work.WorkType == StyleWorkType.Subtree)
            {
                ProcessSubtreeWork(work, cancellationToken);
            }
            else
            {
                ProcessElementWork(work, cancellationToken);
            }
        }
    }

    private void ProcessElementWork(StyleRecalcWork work, CancellationToken cancellationToken)
    {
        try
        {
            if (work.Priority == RecalcPriority.Critical && _mainThreadWork != null)
            {
                _mainThreadWork.EnqueueElement(work.Element, work.Priority);
                _mainThreadWork.ProcessSync();
            }
            else if (_useWorkerThreads && _workerThreadPool != null && work.Priority != RecalcPriority.Critical)
            {
                _workerThreadPool.EnqueueElement(work.Element, work.Priority);
            }
            else
            {
                var task = StyleTasks.ComputeElementStyle(work.Element, work.Priority);
                _taskScheduler.EnqueueTask(task);
                _taskScheduler.ProcessTasks();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing element style: {ex.Message}");
        }
    }

    private void ProcessSubtreeWork(StyleRecalcWork work, CancellationToken cancellationToken)
    {
        try
        {
            if (_useWorkerThreads && _workerThreadPool != null && work.Priority != RecalcPriority.Critical)
            {
                _workerThreadPool.EnqueueElement(work.Element, work.Priority);
            }
            else
            {
                var task = StyleTasks.UpdateSubtreeStyles(work.Element, work.Priority);
                _taskScheduler.EnqueueTask(task);
                _taskScheduler.ProcessTasks();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing subtree styles: {ex.Message}");
        }
    }

    private int GetElementSortOrder(StyleRecalcWork work)
    {
        if (_viewportDetector.IsInViewport(work.Element))
            return 0;
        if (_viewportDetector.IsNearViewport(work.Element))
            return 10;
        var depth = 0;
        var element = work.Element;
        while (element.ParentElement != null)
        {
            depth++;
            element = element.ParentElement;
        }
        return 100 + depth;
    }

    private void ScheduleProcessing()
    {
        if (_throttleTimer == null)
        {
            _throttleTimer = new Timer(
                _ => ProcessImmediately(),
                null,
                _throttleIntervalMs,
                Timeout.Infinite);
        }
        else
        {
            _throttleTimer.Change(_throttleIntervalMs, Timeout.Infinite);
        }
    }
    #endregion

    #region Helper Methods
    private RecalcPriority DeterminePriority(IElement element)
    {
        if (_viewportDetector.IsInViewport(element))
        {
            _deviceDependentElements.Add(element);
            return RecalcPriority.Critical;
        }
        if (_viewportDetector.IsNearViewport(element))
        {
            return RecalcPriority.High;
        }
        if (ElementIsLikelyToBeVisibleSoon(element))
        {
            return RecalcPriority.Normal;
        }
        return RecalcPriority.Low;
    }

    private bool ElementIsLikelyToBeVisibleSoon(IElement element)
    {
        var parent = element.ParentElement;
        while (parent != null)
        {
            if (_viewportDetector.IsInViewport(parent) || _viewportDetector.IsNearViewport(parent))
                return true;
            parent = parent.ParentElement;
        }
        return false;
    }

    private bool IsLayoutProperty(string propertyName)
    {
        return propertyName.StartsWith("width") ||
               propertyName.StartsWith("height") ||
               propertyName.StartsWith("margin") ||
               propertyName.StartsWith("padding") ||
               propertyName.StartsWith("border") ||
               propertyName.StartsWith("position") ||
               propertyName.StartsWith("display") ||
               propertyName.StartsWith("flex") ||
               propertyName.StartsWith("grid") ||
               propertyName.StartsWith("top") ||
               propertyName.StartsWith("right") ||
               propertyName.StartsWith("bottom") ||
               propertyName.StartsWith("left");
    }

    private bool IsVisualProperty(string propertyName)
    {
        return propertyName.StartsWith("color") ||
               propertyName.StartsWith("background") ||
               propertyName.StartsWith("font") ||
               propertyName.StartsWith("text") ||
               propertyName.StartsWith("box-shadow") ||
               propertyName.StartsWith("opacity") ||
               propertyName.StartsWith("transform") ||
               propertyName.StartsWith("animation") ||
               propertyName.StartsWith("transition");
    }

    private bool CouldAffectChildren(IElement element, IEnumerable<string> properties)
    {
        return properties.Any(p =>
            p == "display" ||
            p == "position" ||
            p == "font-size" ||
            p == "line-height" ||
            p.StartsWith("flex") ||
            p.StartsWith("grid"));
    }
    #endregion

    #region IDisposable Implementation
    public void Dispose()
    {
        if (_isDisposed)
            return;

        CancelPendingWork();
        _throttleTimer?.Dispose();
        _frameEndTimer?.Dispose();
        _workSemaphore.Dispose();
        _processingCts?.Dispose();
        _completionCallback = null;

        // Unsubscribe from events
        foreach (var token in _subscriptionTokens)
        {
            token.Dispose();
        }

        _isDisposed = true;
    }
    #endregion
}