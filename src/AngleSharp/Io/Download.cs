namespace AngleSharp.Io
{
    using AngleSharp.Dom;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a download in progress.
    /// </summary>
    sealed class Download : IDownload
    {
        #region Fields

        private readonly CancellationTokenSource _cts;
        private readonly Task<IResponse> _task;
        private readonly IUrl _target;
        private readonly Object? _source;

        #endregion

        #region ctor

        public Download(Task<IResponse> task, CancellationTokenSource cts, IUrl target, Object? source)
        {
            _task = task;
            _cts = cts;
            _target = target;
            _source = source;
        }

        #endregion

        #region Properties

        public Object? Source => _source;

        public IUrl Target => _target;

        public Task<IResponse> Task => _task;

        public Boolean IsRunning => _task.Status == TaskStatus.Running;

        public Boolean IsCompleted => _task.Status == TaskStatus.Faulted ||
            _task.Status == TaskStatus.RanToCompletion ||
            _task.Status == TaskStatus.Canceled;

        #endregion

        #region Methods

        public void Cancel() => _cts.Cancel();

        #endregion
    }
}
