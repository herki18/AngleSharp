using System;

namespace LayoutEngine.StyleSystem
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AngleSharp.Dom;
    using Contracts.Platform.Lifecycle;
    using Contracts.StyleSystem;

    public class StyleEngine : IStyleEngine
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public DocumentLifecyclePhase CurrentPhase { get; }
        public bool HasPendingUpdates { get; }
        public Task InitializeAsync(IDocument document)
        {
            throw new NotImplementedException();
        }

        public Task ShutdownAsync()
        {
            throw new NotImplementedException();
        }

        public Task ProcessUpdatesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IComputedStyle> ComputeStyleAsync(IElement element)
        {
            throw new NotImplementedException();
        }

        public void InvalidateStyles(IReadOnlyList<IElement> elements)
        {
            throw new NotImplementedException();
        }

        public void InvalidateAllStyles()
        {
            throw new NotImplementedException();
        }

        public IComputedStyle? GetCachedStyle(IElement element)
        {
            throw new NotImplementedException();
        }

        public Task<String> AddStyleSheetAsync(string styleSheet, StyleSheetOrigin origin, string? mediaQuery = null)
        {
            throw new NotImplementedException();
        }

        public Boolean RemoveStyleSheet(string styleSheetId)
        {
            throw new NotImplementedException();
        }
    }
}