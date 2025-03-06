namespace AngleSharp.LayoutEngine.StyleSystem
{
    using System;
    using System.Collections.Generic;
    using AngleSharp.Css.Dom;
    using AngleSharp.Dom;

    /// <summary>
    /// Maintains state during CSS variable resolution to handle circular references
    /// and provide caching.
    /// </summary>
    public class ResolverContext
    {
        private readonly HashSet<string> _resolutionChain = new HashSet<string>();
        private readonly Dictionary<string, ICssValue> _cache = new Dictionary<string, ICssValue>();
        private const int MaxResolutionDepth = 32; // Prevent excessive recursion

        /// <summary>
        /// Gets the current resolution depth (number of nested variables being resolved).
        /// </summary>
        public int CurrentDepth { get; private set; } = 0;

        /// <summary>
        /// Attempts to enter variable resolution for the given variable name.
        /// Returns false if entering would create a circular reference or exceed max depth.
        /// </summary>
        /// <param name="name">The variable name to resolve</param>
        /// <returns>True if resolution can proceed, false if circular reference detected</returns>
        public bool TryEnterVariable(string name)
        {
            // Check for circular reference
            if (_resolutionChain.Contains(name))
                return false;

            // Check for excessive resolution depth
            if (CurrentDepth >= MaxResolutionDepth)
                return false;

            // Enter variable resolution
            _resolutionChain.Add(name);
            CurrentDepth++;
            return true;
        }

        /// <summary>
        /// Exits variable resolution for the given variable name.
        /// </summary>
        /// <param name="name">The variable name that was being resolved</param>
        public void ExitVariable(string name)
        {
            _resolutionChain.Remove(name);
            CurrentDepth--;
        }

        /// <summary>
        /// Tries to get a cached resolved value.
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="value">The cached value, if found</param>
        /// <returns>True if value was found in cache, otherwise false</returns>
        public bool TryGetCachedValue(string key, out ICssValue value)
        {
            return _cache.TryGetValue(key, out value);
        }

        /// <summary>
        /// Caches a resolved variable value.
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="value">The resolved value to cache</param>
        public void CacheValue(string key, ICssValue value)
        {
            _cache[key] = value;
        }

        /// <summary>
        /// Generates a cache key combining variable name and element.
        /// </summary>
        /// <param name="name">The variable name</param>
        /// <param name="element">The element context</param>
        /// <returns>A unique cache key</returns>
        public string GenerateCacheKey(string name, IElement element)
        {
            // Create unique key combining variable name and element identity
            return $"{name}_{element.GetHashCode()}";
        }

        /// <summary>
        /// Detects if adding the specified variable name would create a cycle.
        /// </summary>
        /// <param name="variableName">The variable name to check</param>
        /// <returns>Tuple indicating if cycle exists and the resolution path</returns>
        public (bool HasCycle, IEnumerable<string> Path) DetectCycle(string variableName)
        {
            if (_resolutionChain.Contains(variableName))
            {
                // Create path for debugging
                var cyclePath = new List<string>(_resolutionChain);
                cyclePath.Add(variableName);
                return (true, cyclePath);
            }

            return (false, Array.Empty<string>());
        }

        /// <summary>
        /// Clears the resolution cache.
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }
    }
}