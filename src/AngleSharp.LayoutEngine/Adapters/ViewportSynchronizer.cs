namespace AngleSharp.LayoutEngine.Adapters
{
    using AngleSharp.Css;
    using AngleSharp.Dom;
    using AngleSharp.LayoutEngine.Core;
    using System;

    /// <summary>
    /// Synchronizes viewport dimensions between AngleSharp and the layout engine.
    /// </summary>
    public class ViewportSynchronizer
    {
        private readonly IBrowsingContext _browsingContext;
        private float _viewportWidth;
        private float _viewportHeight;

        /// <summary>
        /// Creates a new viewport synchronizer.
        /// </summary>
        /// <param name="browsingContext">The browsing context to synchronize with.</param>
        /// <param name="initialWidth">The initial viewport width.</param>
        /// <param name="initialHeight">The initial viewport height.</param>
        public ViewportSynchronizer(IBrowsingContext browsingContext, float initialWidth = 1024, float initialHeight = 768)
        {
            _browsingContext = browsingContext ?? throw new ArgumentNullException(nameof(browsingContext));
            _viewportWidth = initialWidth;
            _viewportHeight = initialHeight;

            // Apply initial viewport dimensions to AngleSharp
            ApplyToAngleSharp();
        }

        /// <summary>
        /// Gets or sets the viewport width.
        /// </summary>
        public float ViewportWidth
        {
            get => _viewportWidth;
            set
            {
                if (_viewportWidth != value)
                {
                    _viewportWidth = value;
                    ApplyToAngleSharp();
                }
            }
        }

        /// <summary>
        /// Gets or sets the viewport height.
        /// </summary>
        public float ViewportHeight
        {
            get => _viewportHeight;
            set
            {
                if (_viewportHeight != value)
                {
                    _viewportHeight = value;
                    ApplyToAngleSharp();
                }
            }
        }

        /// <summary>
        /// Creates a layout context with synchronized viewport dimensions.
        /// </summary>
        /// <returns>A new layout context with current viewport dimensions.</returns>
        public LayoutContext CreateLayoutContext()
        {
            return new LayoutContext(_viewportWidth, _viewportHeight);
        }

        /// <summary>
        /// Updates an existing layout context with synchronized viewport dimensions.
        /// </summary>
        /// <param name="context">The layout context to update.</param>
        public void UpdateLayoutContext(LayoutContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.ViewportWidth = _viewportWidth;
            context.ViewportHeight = _viewportHeight;
        }

        /// <summary>
        /// Applies the current viewport dimensions to AngleSharp.
        /// </summary>
        private void ApplyToAngleSharp()
        {
            var renderDevice = _browsingContext.GetService<IRenderDevice>();
            if (renderDevice != null)
            {
                renderDevice.SetViewport((int)_viewportWidth, (int)_viewportHeight);
            }
        }

        /// <summary>
        /// Gets dimensions from AngleSharp and synchronizes them with the layout engine.
        /// </summary>
        public void SynchronizeFromAngleSharp()
        {
            var renderDevice = _browsingContext.GetService<IRenderDevice>();
            if (renderDevice != null)
            {
                _viewportWidth = renderDevice.ViewPortWidth;
                _viewportHeight = renderDevice.ViewPortHeight;
            }
        }
    }
}