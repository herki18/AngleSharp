using System;
using System.Collections.Generic;
using AngleSharp.Browser;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AngleSharp.StyleSystem.DependencyInjection
{
    /// <summary>
    /// Service that bridges AngleSharp's service system with a DI container for StyleSystem.
    /// This class acts as a central access point for all StyleSystem services and handles
    /// initialization and lifecycle management.
    /// </summary>
    public class AngleSharpServiceIntegration
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, object> _serviceCache = new Dictionary<Type, object>();
        private readonly object _initLock = new object();
        private IBrowsingContext? _currentContext;
        private bool _isInitialized;

        /// <summary>
        /// Creates a new AngleSharpServiceIntegration with the specified service provider.
        /// </summary>
        /// <param name="serviceProvider">The DI container's service provider.</param>
        public AngleSharpServiceIntegration(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets a service from the DI container by type.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <returns>The service instance, or null if not registered.</returns>
        public T? GetService<T>() where T : class
        {
            // Ensure initialization before providing services
            EnsureInitialized();

            var type = typeof(T);

            // Check cache first
            if (_serviceCache.TryGetValue(type, out var cachedService))
            {
                return (T)cachedService;
            }

            // Try to resolve from container
            var service = _serviceProvider.GetService<T>();

            // Cache if found
            if (service != null)
            {
                _serviceCache[type] = service;
            }

            return service;
        }

        /// <summary>
        /// Gets a required service from the DI container by type.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <returns>The service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the service isn't registered.</exception>
        public T GetRequiredService<T>() where T : class
        {
            var service = GetService<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"Required service of type {typeof(T).Name} was not found in the StyleSystem service container.");
            }
            return service;
        }

        /// <summary>
        /// Initializes StyleSystem with the specified context.
        /// </summary>
        /// <param name="context">The browsing context to initialize with.</param>
        public void Initialize(IBrowsingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            lock (_initLock)
            {
                if (_currentContext == context && _isInitialized)
                    return;

                StyleSystemService? styleSystemService;
                if (_currentContext != null && _currentContext != context)
                {
                    // Clean up previous context
                    styleSystemService = _serviceProvider.GetService<StyleSystemService>();
                    styleSystemService?.CleanupContext(_currentContext);
                    _serviceCache.Clear();
                }

                _currentContext = context;

                // Initialize StyleSystem with the context
                styleSystemService = _serviceProvider.GetService<StyleSystemService>();
                if (styleSystemService != null)
                {
                    styleSystemService.Initialize(context);
                    _isInitialized = true;
                }
            }
        }

        /// <summary>
        /// Ensures the integration service is initialized with the current context.
        /// </summary>
        private void EnsureInitialized()
        {
            lock (_initLock)
            {
                if (_currentContext != null && !_isInitialized)
                {
                    Initialize(_currentContext);
                }
            }
        }
    }
}