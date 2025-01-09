namespace AngleSharp.Renderer.Tests.Mocks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AngleSharp.Io;
    using AngleSharp.Scripting;

    class CallbackScriptEngine : IScriptingService
    {
        private String _type;

        public CallbackScriptEngine(Action<ScriptOptions> callback, String type = null)
        {
            Callback = callback;
            _type = type ?? "c-sharp";
        }

        public String Type => _type;

        public Boolean SupportsType(String mimeType)
        {
            return mimeType.Equals(_type, StringComparison.OrdinalIgnoreCase);
        }

        public Action<ScriptOptions> Callback
        {
            get;
            private set;
        }

        public Task EvaluateScriptAsync(IResponse response, ScriptOptions options, CancellationToken cancel)
        {
            Callback?.Invoke(options);
            return Task.FromResult(true);
        }
    }
}
