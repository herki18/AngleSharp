namespace AngleSharp.LayoutEngine.StyleSystem
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using AngleSharp.Css;
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Dom;

    /// <summary>
    /// Resolves CSS variables (custom properties) to their computed values.
    /// </summary>
    public class VariableResolver
    {
        private readonly VariableRegistry _registry;
        private readonly IBrowsingContext _context;
        private readonly IRenderDevice _device;

        /// <summary>
        /// Creates a new variable resolver.
        /// </summary>
        /// <param name="registry">The variable registry containing defined variables</param>
        /// <param name="context">The browsing context</param>
        /// <param name="device">The render device for unit conversion</param>
        public VariableResolver(VariableRegistry registry, IBrowsingContext context, IRenderDevice device)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>
        /// Resolves a CSS variable reference to its computed value.
        /// </summary>
        /// <param name="varValue">The var() function to resolve</param>
        /// <param name="element">The context element</param>
        /// <param name="resolverContext">The resolution context for tracking references</param>
        /// <returns>The resolved CSS value, or null if unresolvable</returns>
        public ICssValue? ResolveVariable(CssVarValue varValue, IElement element, ResolverContext resolverContext)
        {
            // Extract variable name
            string name = varValue.VariableName;

            // Check for circular reference
            if (!resolverContext.TryEnterVariable(name))
            {
                // Circular reference detected, use fallback if available
                var fallbackValue = ResolveFallback(varValue.DefaultValue, element, resolverContext);

                // Log debugging information about the cycle
                var (hasCycle, path) = resolverContext.DetectCycle(name);
                if (hasCycle)
                {
                    Debug.WriteLine($"Circular CSS variable reference detected: {string.Join(" -> ", path)}");
                }

                return fallbackValue;
            }

            try
            {
                // Try to get value from registry
                var value = _registry.GetVariableValue(name);

                // If not found in registry, look in inheritance chain
                if (value == null && element.ParentElement != null)
                {
                    value = TryGetInheritedValue(name, element);
                }

                // If still not found, use fallback if available
                if (value == null)
                {
                    return ResolveFallback(varValue.DefaultValue, element, resolverContext);
                }

                // Resolve any nested variables in the value
                return ResolveNestedReferences(value, element, resolverContext);
            }
            finally
            {
                resolverContext.ExitVariable(name);
            }
        }

        /// <summary>
        /// Safely resolves a variable reference with error handling.
        /// </summary>
        /// <param name="varValue">The var() function to resolve</param>
        /// <param name="element">The context element</param>
        /// <param name="resolverContext">The resolution context for tracking references</param>
        /// <returns>The resolved CSS value, or null if resolution failed</returns>
        public ICssValue? SafeResolveVariable(CssVarValue varValue, IElement element, ResolverContext resolverContext)
        {
            try
            {
                return ResolveVariable(varValue, element, resolverContext);
            }
            catch (Exception ex)
            {
                // Log error
                Debug.WriteLine($"Error resolving CSS variable {varValue.VariableName}: {ex.Message}");

                // Try fallback if available
                if (varValue.DefaultValue != null)
                {
                    try
                    {
                        return ResolveFallback(varValue.DefaultValue, element, resolverContext);
                    }
                    catch
                    {
                        // If fallback fails too, return null
                        return null;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Resolves the fallback value of a var() function.
        /// </summary>
        /// <param name="fallback">The fallback value to resolve</param>
        /// <param name="element">The context element</param>
        /// <param name="context">The resolution context</param>
        /// <returns>The resolved fallback value, or null if none or unresolvable</returns>
        private ICssValue? ResolveFallback(ICssValue fallback, IElement element, ResolverContext context)
        {
            if (fallback == null)
                return null;

            // If fallback is another var(), resolve it
            if (fallback is CssVarValue nestedVar)
            {
                return ResolveVariable(nestedVar, element, context);
            }

            // Otherwise use as is (after resolving any nested references)
            return ResolveNestedReferences(fallback, element, context);
        }

        /// <summary>
        /// Resolves any nested variable references within a CSS value.
        /// </summary>
        /// <param name="value">The CSS value that may contain variable references</param>
        /// <param name="element">The context element</param>
        /// <param name="context">The resolution context</param>
        /// <returns>The value with all nested references resolved</returns>
        private ICssValue? ResolveNestedReferences(ICssValue value, IElement element, ResolverContext context)
        {
            // Handle different value types
            if (value is CssVarValue nestedVar)
            {
                return ResolveVariable(nestedVar, element, context);
            }

            if (value is CssCalcValue calcValue)
            {
                return ResolveCalcExpression(calcValue, element, context);
            }

            if (value is ICssMultipleValue multiValue)
            {
                var resolvedItems = new List<ICssValue>();

                for (var i = 0; i < multiValue.Count; i++)
                {
                    var item = multiValue[i];
                    var resolvedItem = ResolveNestedReferences(item, element, context);

                    if (resolvedItem != null)
                    {
                        resolvedItems.Add(resolvedItem);
                    }
                }

                // Return new list with resolved values
                return new CssListValue(resolvedItems.ToArray());
            }

            // For other value types, return as is
            return value;
        }

        /// <summary>
        /// Resolves variables within a calc() expression.
        /// </summary>
        /// <param name="calcValue">The calc() expression to resolve</param>
        /// <param name="element">The context element</param>
        /// <param name="context">The resolution context</param>
        /// <returns>A calc expression with resolved variables</returns>
        public ICssValue ResolveCalcExpression(CssCalcValue calcValue, IElement element, ResolverContext context)
        {
            // Resolve any variables in the expression
            var resolvedExpression = ResolveNestedReferences(calcValue.Expression, element, context);

            // Return a new calc with the resolved expression
            if (resolvedExpression != calcValue.Expression)
            {
                return new CssCalcValue(resolvedExpression);
            }

            return calcValue;
        }

        /// <summary>
        /// Gets variable value from the inheritance chain.
        /// </summary>
        /// <param name="name">The variable name</param>
        /// <param name="element">The element to start looking from</param>
        /// <returns>The inherited variable value, or null if not found</returns>
        private ICssValue? TryGetInheritedValue(string name, IElement element)
        {
            var parent = element.ParentElement;
            if (parent == null)
                return null;

            // Try the parent's registry value
            var value = _registry.GetVariableValue(name);
            if (value != null)
                return value;

            // Recursively check further up the tree
            return TryGetInheritedValue(name, parent);
        }
    }
}