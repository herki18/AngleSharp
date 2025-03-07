namespace AngleSharp.LayoutEngine.StyleSystem
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using AngleSharp.Css.Dom;
    using AngleSharp.Dom;

    /// <summary>
    /// Context for CSS variable resolution, including cycle detection and caching.
    /// </summary>
    public class ResolverContext
    {
        private readonly HashSet<string> _resolutionChain = new HashSet<string>();
        private readonly Dictionary<string, ICssValue> _cache = new Dictionary<string, ICssValue>();
        private const string LogPrefix = "[ResolverContext] ";

        /// <summary>
        /// Maximum allowed depth for variable resolution to prevent stack overflow.
        /// </summary>
        public const int MaxResolutionDepth = 32;

        /// <summary>
        /// Gets the current resolution depth.
        /// </summary>
        public int CurrentDepth { get; private set; } = 0;

        /// <summary>
        /// Creates a new resolver context.
        /// </summary>
        public ResolverContext()
        {
            Console.WriteLine($"{LogPrefix}Created new resolver context");
        }

        /// <summary>
        /// Attempts to enter variable resolution for the specified variable.
        /// </summary>
        /// <param name="name">The name of the variable to resolve.</param>
        /// <returns>True if variable resolution can proceed, false if a cycle was detected or max depth reached.</returns>
        public bool TryEnterVariable(string name)
        {
            Console.WriteLine($"{LogPrefix}Attempting to enter variable: {name} (current depth: {CurrentDepth})");

            if (_resolutionChain.Contains(name))
            {
                Console.WriteLine($"{LogPrefix}Circular reference detected: {name} is already in the resolution chain!");
                Console.WriteLine($"{LogPrefix}Current chain: {string.Join(" -> ", _resolutionChain)}");
                return false;
            }

            if (CurrentDepth >= MaxResolutionDepth)
            {
                Console.WriteLine($"{LogPrefix}Maximum resolution depth ({MaxResolutionDepth}) reached. Cannot enter {name}");
                return false;
            }

            _resolutionChain.Add(name);
            CurrentDepth++;
            Console.WriteLine($"{LogPrefix}Entered variable {name}, new depth: {CurrentDepth}");
            Console.WriteLine($"{LogPrefix}Current chain: {string.Join(" -> ", _resolutionChain)}");
            return true;
        }

        /// <summary>
        /// Exits variable resolution for the specified variable.
        /// </summary>
        /// <param name="name">The name of the variable that was being resolved.</param>
        public void ExitVariable(string name)
        {
            Console.WriteLine($"{LogPrefix}Exiting variable: {name} (current depth: {CurrentDepth})");

            if (_resolutionChain.Contains(name))
            {
                _resolutionChain.Remove(name);
                CurrentDepth = Math.Max(0, CurrentDepth - 1);
                Console.WriteLine($"{LogPrefix}Removed {name} from chain, new depth: {CurrentDepth}");

                if (_resolutionChain.Count > 0)
                {
                    Console.WriteLine($"{LogPrefix}Current chain: {string.Join(" -> ", _resolutionChain)}");
                }
                else
                {
                    Console.WriteLine($"{LogPrefix}Resolution chain is now empty");
                }
            }
            else
            {
                Console.WriteLine($"{LogPrefix}Warning: Attempted to exit {name} but it's not in the resolution chain!");
            }
        }

        /// <summary>
        /// Tries to get a value from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cached value, if found.</param>
        /// <returns>True if the value was found in the cache, false otherwise.</returns>
        public bool TryGetCachedValue(string key, out ICssValue value)
        {
            var found = _cache.TryGetValue(key, out value);
            Console.WriteLine($"{LogPrefix}Cache lookup for key '{key}': {(found ? "HIT" : "MISS")}");

            if (found)
            {
                Console.WriteLine($"{LogPrefix}Cached value: {value.CssText}");
            }

            return found;
        }

        /// <summary>
        /// Caches a value with the specified key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        public void CacheValue(string key, ICssValue value)
        {
            Console.WriteLine($"{LogPrefix}Caching value for key '{key}': {value.CssText}");
            _cache[key] = value;
        }

        /// <summary>
        /// Generates a cache key for a variable and element.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="element">The element context.</param>
        /// <returns>A string key for cache lookups.</returns>
        public string GenerateCacheKey(string name, IElement element)
        {
            var key = $"{name}_{element.GetHashCode()}";
            Console.WriteLine($"{LogPrefix}Generated cache key: {key}");
            return key;
        }

        /// <summary>
        /// Detects if adding a variable would create a circular reference.
        /// </summary>
        /// <param name="variableName">The variable name to check.</param>
        /// <returns>A tuple indicating if a cycle was detected and the path of the cycle.</returns>
        public (bool HasCycle, IEnumerable<string> Path) DetectCycle(string variableName)
        {
            Console.WriteLine($"{LogPrefix}Checking for cycles with variable: {variableName}");
            Console.WriteLine($"{LogPrefix}Current chain: {string.Join(" -> ", _resolutionChain)}");

            if (_resolutionChain.Contains(variableName))
            {
                var cyclePath = new List<string>(_resolutionChain);
                cyclePath.Add(variableName);

                Console.WriteLine($"{LogPrefix}CYCLE DETECTED: {string.Join(" -> ", cyclePath)}");
                return (true, cyclePath);
            }

            Console.WriteLine($"{LogPrefix}No cycle detected for {variableName}");
            return (false, Array.Empty<string>());
        }

        /// <summary>
        /// Clears the variable resolution cache.
        /// </summary>
        public void ClearCache()
        {
            Console.WriteLine($"{LogPrefix}Clearing cache with {_cache.Count} entries");
            _cache.Clear();
        }

        /// <summary>
        /// Gets the current variable resolution chain.
        /// </summary>
        /// <returns>The list of variables currently in the resolution process.</returns>
        public IEnumerable<string> GetCurrentResolutionChain()
        {
            return new List<string>(_resolutionChain);
        }
    }
}