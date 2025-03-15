```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using AngleSharp.StyleSystem.Events;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.DependencyInjection;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Services;
using AngleSharp.Dom;

namespace AngleSharp.StyleSystem.Examples
{
    public static class EventAggregatorExtensions
    {
        /// <summary>
        /// Adds the EventAggregator to the service collection
        /// </summary>
        public static IServiceCollection AddStyleEventAggregator(this IServiceCollection services)
        {
            // Register the event aggregator as a singleton
            services.AddSingleton<IStyleEventAggregator, StyleEventAggregator>();
            
            // Register the adapter as a singleton
            services.AddSingleton<StyleEventAdapter>();
            
            return services;
        }
        
        /// <summary>
        /// Configures the StyleSystem to use the EventAggregator
        /// </summary>
        public static IBrowsingContext UseStyleEventAggregator(this IBrowsingContext context)
        {
            // Get the event aggregator 
            var eventAggregator = context.GetStyleSystemService<IStyleEventAggregator>();
            if (eventAggregator == null)
            {
                throw new InvalidOperationException("The IStyleEventAggregator service is not registered. " +
                    "Make sure to call AddStyleEventAggregator() when registering services.");
            }
            
            // Get the adapter
            var adapter = context.GetStyleSystemService<StyleEventAdapter>();
            if (adapter == null)
            {
                throw new InvalidOperationException("The StyleEventAdapter service is not registered. " +
                    "Make sure to call AddStyleEventAggregator() when registering services.");
            }
            
            // Connect the adapter to the observer patterns
            var invalidationTracker = context.GetStyleSystemService<IStyleInvalidationTracker>();
            if (invalidationTracker != null)
            {
                invalidationTracker.AddObserver(adapter);
            }
            
            var styleEngine = context.GetStyleSystemService<IStyleEngine>();
            if (styleEngine != null)
            {
                styleEngine.AddComputationObserver(adapter);
            }
            
            var lifecycleCoordinator = context.GetStyleSystemService<IDocumentLifecycleCoordinator>();
            if (lifecycleCoordinator != null)
            {
                lifecycleCoordinator.AddObserver(adapter);
            }
            
            return context;
        }
    }
    
    public class UsageExample
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            // Add AngleSharp services
            services.AddAngleSharpServices(/* browsing context */);
            
            // Add StyleSystem services
            services.AddStyleSystem(options => {
                options.EnableOptimization = true;
                options.ThrottleIntervalMs = 16;
            });
            
            // Add event aggregator
            services.AddStyleEventAggregator();
        }
        
        public static void ConfigureEvents(IBrowsingContext context)
        {
            // Get the event aggregator
            var eventAggregator = context.GetStyleSystemService<IStyleEventAggregator>();
            
            // Example: Subscribe to style computation events
            eventAggregator.Subscribe<StyleComputedEvent>(evt => {
                Console.WriteLine($"Style computed for element: {evt.Element.NodeName}");
            });
            
            // Example: Subscribe to specific elements being invalidated
            eventAggregator.Subscribe<ElementInvalidatedEvent>(
                evt => evt.Element.Id == "main-content",
                evt => {
                    Console.WriteLine("Main content was invalidated");
                }
            );
            
            // Example: Subscribe to document ready state changes
            eventAggregator.Subscribe<ReadyStateChangedEvent>(evt => {
                if (evt.ReadyState == DocumentReadyState.Complete)
                {
                    Console.WriteLine("Document loading complete");
                }
            });
            
            // Example: Performance monitoring for style operations
            var styleComputationTimes = new System.Diagnostics.Stopwatch();
            
            eventAggregator.Subscribe<StyleComputedEvent>(evt => {
                styleComputationTimes.Start();
            });
            
            eventAggregator.Subscribe<SubtreeStylesUpdatedEvent>(evt => {
                styleComputationTimes.Stop();
                Console.WriteLine($"Style computation took: {styleComputationTimes.ElapsedMilliseconds}ms");
                styleComputationTimes.Reset();
            });
        }
        
        public static void Initialize()
        {
            // Create service provider
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            
            // Create browsing context
            var config = Configuration.Default.WithServices(serviceProvider);
            var context = BrowsingContext.New(config);
            
            // Use StyleSystem with EventAggregator
            context.UseStyleSystem();
            context.UseStyleEventAggregator();
            
            // Configure event subscriptions
            ConfigureEvents(context);
        }
    }
}
```