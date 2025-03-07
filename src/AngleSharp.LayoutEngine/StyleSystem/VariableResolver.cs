namespace AngleSharp.LayoutEngine.StyleSystem
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using AngleSharp.Css;
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Dom;

    /// <summary>
    /// Resolves CSS variable references according to the CSS specification.
    /// </summary>
    public class VariableResolver
    {
        private readonly VariableRegistry _registry;
        private readonly IBrowsingContext _context;
        private readonly IRenderDevice _device;
        private const int MaxInheritanceDepth = 100;
        private const string LogPrefix = "[VariableResolver] ";
        private readonly Dictionary<string, ICssValue> _originalFallbacks = new Dictionary<string, ICssValue>();


        /// <summary>
        /// Creates a new variable resolver.
        /// </summary>
        public VariableResolver(VariableRegistry registry, IBrowsingContext context, IRenderDevice device)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _device = device ?? throw new ArgumentNullException(nameof(device));
            Console.WriteLine($"{LogPrefix}Initialized with registry containing {_registry.GetVariableNames().Count()} variables");
        }

        /// <summary>
        /// Resolves a CSS variable reference to its computed value.
        /// </summary>
        public ICssValue? ResolveVariable(CssVarValue varValue, IElement element, ResolverContext resolverContext)
        {
            string name = varValue.VariableName;

            // Store the fallback for this variable if it has one
            if (varValue.DefaultValue != null && !_originalFallbacks.ContainsKey(name))
            {
                _originalFallbacks[name] = varValue.DefaultValue;
                Debug.WriteLine($"{LogPrefix}Storing fallback for {name}: {varValue.DefaultValue.CssText}");
            }

            string fallbackDesc = varValue.DefaultValue != null ? $"with fallback: {varValue.DefaultValue.CssText}" : "without fallback";
            Debug.WriteLine($"{LogPrefix}Resolving variable {name} {fallbackDesc} | Element: {element.NodeName} | Depth: {resolverContext.CurrentDepth}");

            // Log the current resolution chain
            if (resolverContext.CurrentDepth > 0)
            {
                var chain = string.Join(" -> ", resolverContext.GetCurrentResolutionChain());
                Debug.WriteLine($"{LogPrefix}Current resolution chain: {chain}");
            }

            // Check for circular reference before attempting to enter variable resolution
            var (hasCycle, path) = resolverContext.DetectCycle(name);
            if (hasCycle)
            {
                Debug.WriteLine($"{LogPrefix}CIRCULAR REFERENCE DETECTED: {string.Join(" -> ", path)} -> {name}");

                // First check immediate fallback
                if (varValue.DefaultValue != null)
                {
                    Debug.WriteLine($"{LogPrefix}Using immediate fallback due to circular reference: {varValue.DefaultValue.CssText}");
                    return varValue.DefaultValue;
                }

                // Then check stored fallback
                if (_originalFallbacks.TryGetValue(name, out var storedFallback))
                {
                    Debug.WriteLine($"{LogPrefix}Using stored fallback for {name}: {storedFallback.CssText}");
                    return storedFallback;
                }

                Debug.WriteLine($"{LogPrefix}No fallback available for circular reference");
                return null;
            }

            if (!resolverContext.TryEnterVariable(name))
            {
                Console.WriteLine($"{LogPrefix}Failed to enter variable {name} - max depth exceeded or already in resolution chain");
                Console.WriteLine($"{LogPrefix}Returning fallback value: {varValue.DefaultValue?.CssText ?? "null"}");
                return varValue.DefaultValue; // Return fallback value
            }

            Console.WriteLine($"{LogPrefix}Successfully entered variable {name}, depth now: {resolverContext.CurrentDepth}");

            try
            {
                // Check registry
                var value = _registry.GetVariableValue(name);
                if (value != null)
                {
                    Console.WriteLine($"{LogPrefix}Found {name} in registry: {value.CssText}");
                }
                else
                {
                    Console.WriteLine($"{LogPrefix}{name} not found in registry, checking inheritance");
                }

                // Try inheritance if not in registry
                if (value == null && element.ParentElement != null)
                {
                    Console.WriteLine($"{LogPrefix}Trying to inherit {name} from parent");
                    value = TryGetInheritedValue(name, element, 0);
                    if (value != null)
                    {
                        Console.WriteLine($"{LogPrefix}Inherited {name} with value: {value.CssText}");
                    }
                    else
                    {
                        Console.WriteLine($"{LogPrefix}Failed to inherit {name}");
                    }
                }

                if (value == null)
                {
                    Console.WriteLine($"{LogPrefix}No value found for {name}, resolving fallback");
                    var result = ResolveFallback(varValue.DefaultValue!, element, resolverContext);
                    Console.WriteLine($"{LogPrefix}Fallback for {name} resolved to: {result?.CssText ?? "null"}");
                    return result;
                }

                Console.WriteLine($"{LogPrefix}Resolving nested references in value for {name}");
                var resolved = ResolveNestedReferences(value, element, resolverContext);
                Console.WriteLine($"{LogPrefix}Finished resolving {name}: {resolved?.CssText ?? "null"}");
                return resolved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{LogPrefix}ERROR resolving {name}: {ex.Message}\n{ex.StackTrace}");
                return varValue.DefaultValue;
            }
            finally
            {
                resolverContext.ExitVariable(name);
                Console.WriteLine($"{LogPrefix}Exited variable {name}, depth now: {resolverContext.CurrentDepth}");
            }
        }

        /// <summary>
        /// Safely resolves a CSS variable with error handling.
        /// </summary>
        public ICssValue? SafeResolveVariable(CssVarValue varValue, IElement element, ResolverContext resolverContext)
        {
            try
            {
                Console.WriteLine($"{LogPrefix}SafeResolveVariable called for {varValue.VariableName}");
                var result = ResolveVariable(varValue, element, resolverContext);
                Console.WriteLine($"{LogPrefix}SafeResolveVariable result for {varValue.VariableName}: {result?.CssText ?? "null"}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{LogPrefix}CRITICAL ERROR in SafeResolveVariable for {varValue.VariableName}: {ex.Message}\n{ex.StackTrace}");

                if (varValue.DefaultValue != null)
                {
                    Console.WriteLine($"{LogPrefix}Using fallback value: {varValue.DefaultValue.CssText}");
                    try
                    {
                        return varValue.DefaultValue;
                    }
                    catch (Exception fallbackEx)
                    {
                        Console.WriteLine($"{LogPrefix}Error using fallback value: {fallbackEx.Message}");
                        return null;
                    }
                }

                Console.WriteLine($"{LogPrefix}No fallback available, returning null");
                return null;
            }
        }

        /// <summary>
        /// Resolves a fallback value, which may itself be a variable reference.
        /// </summary>
        private ICssValue? ResolveFallback(ICssValue fallback, IElement element, ResolverContext context)
        {
            if (fallback == null)
            {
                Console.WriteLine($"{LogPrefix}Fallback is null");
                return null;
            }

            Console.WriteLine($"{LogPrefix}Resolving fallback value: {fallback.CssText}");

            try
            {
                if (fallback is CssVarValue nestedVar)
                {
                    Console.WriteLine($"{LogPrefix}Fallback is itself a variable reference: {nestedVar.VariableName}");

                    // Check for cycles in the fallback before attempting to resolve
                    if (context.DetectCycle(nestedVar.VariableName).HasCycle)
                    {
                        Console.WriteLine($"{LogPrefix}Cycle detected in fallback variable {nestedVar.VariableName}");
                        Console.WriteLine($"{LogPrefix}Using nested fallback: {nestedVar.DefaultValue?.CssText ?? "null"}");
                        return nestedVar.DefaultValue;
                    }

                    Console.WriteLine($"{LogPrefix}Resolving nested variable in fallback");
                    var result = ResolveVariable(nestedVar, element, context);
                    Console.WriteLine($"{LogPrefix}Nested variable in fallback resolved to: {result?.CssText ?? "null"}");
                    return result;
                }

                Console.WriteLine($"{LogPrefix}Checking for nested references in fallback");
                var resolved = ResolveNestedReferences(fallback, element, context);
                Console.WriteLine($"{LogPrefix}Fallback resolved to: {resolved?.CssText ?? "null"}");
                return resolved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{LogPrefix}Error resolving fallback: {ex.Message}");
                return fallback;
            }
        }

        /// <summary>
        /// Resolves any nested variable references within a CSS value.
        /// </summary>
        private ICssValue? ResolveNestedReferences(ICssValue value, IElement element, ResolverContext context)
        {
            if (value == null)
            {
                Console.WriteLine($"{LogPrefix}Value is null in ResolveNestedReferences");
                return null;
            }

            if (context.CurrentDepth >= ResolverContext.MaxResolutionDepth)
            {
                Console.WriteLine($"{LogPrefix}Max depth reached in ResolveNestedReferences: {context.CurrentDepth}");
                return value;
            }

            Console.WriteLine($"{LogPrefix}ResolveNestedReferences for value type: {value.GetType().Name}, text: {value.CssText}");

            try
            {
                if (value is CssVarValue nestedVar)
                {
                    Console.WriteLine($"{LogPrefix}Found nested var() reference: {nestedVar.VariableName}");
                    var result = ResolveVariable(nestedVar, element, context);
                    Console.WriteLine($"{LogPrefix}Nested var() resolved to: {result?.CssText ?? "null"}");
                    return result;
                }

                if (value is CssCalcValue calcValue)
                {
                    Console.WriteLine($"{LogPrefix}Found calc() expression");
                    var result = ResolveCalcExpression(calcValue, element, context);
                    Console.WriteLine($"{LogPrefix}calc() expression resolved to: {result.CssText}");
                    return result;
                }

                if (value is ICssMultipleValue multiValue)
                {
                    Console.WriteLine($"{LogPrefix}Found multiple value with {multiValue.Count} items");
                    var resolvedItems = new List<ICssValue>();

                    for (var i = 0; i < multiValue.Count; i++)
                    {
                        var item = multiValue[i];
                        Console.WriteLine($"{LogPrefix}Resolving item {i}: {item.CssText}");
                        var resolvedItem = ResolveNestedReferences(item, element, context);

                        if (resolvedItem != null)
                        {
                            Console.WriteLine($"{LogPrefix}Item {i} resolved to: {resolvedItem.CssText}");
                            resolvedItems.Add(resolvedItem);
                        }
                        else
                        {
                            Console.WriteLine($"{LogPrefix}Item {i} resolved to null, keeping original");
                            resolvedItems.Add(item);
                        }
                    }

                    var result = new CssListValue(resolvedItems.ToArray());
                    Console.WriteLine($"{LogPrefix}Multiple value resolved to: {result.CssText}");
                    return result;
                }

                Console.WriteLine($"{LogPrefix}No nested references found, returning original value");
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{LogPrefix}Error resolving nested references: {ex.Message}");
                return value;
            }
        }

        /// <summary>
        /// Resolves variable references within a calc() expression.
        /// </summary>
        public ICssValue ResolveCalcExpression(CssCalcValue calcValue, IElement element, ResolverContext context)
        {
            if (context.CurrentDepth >= ResolverContext.MaxResolutionDepth)
            {
                Console.WriteLine($"{LogPrefix}Max depth reached in ResolveCalcExpression: {context.CurrentDepth}");
                return calcValue;
            }

            Console.WriteLine($"{LogPrefix}Resolving calc expression: {calcValue.CssText}");
            Console.WriteLine($"{LogPrefix}Expression type: {calcValue.Expression.GetType().Name}, value: {calcValue.Expression.CssText}");

            try
            {
                var resolvedExpression = ResolveNestedReferences(calcValue.Expression, element, context);

                if (resolvedExpression != null && !ReferenceEquals(resolvedExpression, calcValue.Expression))
                {
                    Console.WriteLine($"{LogPrefix}Expression changed, creating new calc() with: {resolvedExpression.CssText}");
                    return new CssCalcValue(resolvedExpression);
                }

                Console.WriteLine($"{LogPrefix}Expression unchanged, returning original calc()");
                return calcValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{LogPrefix}Error resolving calc expression: {ex.Message}");
                return calcValue;
            }
        }

        /// <summary>
        /// Attempts to get an inherited CSS variable value from parent elements.
        /// </summary>
        private ICssValue? TryGetInheritedValue(string name, IElement element, int depth)
        {
            if (depth >= MaxInheritanceDepth)
            {
                Console.WriteLine($"{LogPrefix}Max inheritance depth reached ({depth}) for {name}");
                return null;
            }

            if (element == null || element.ParentElement == null)
            {
                Console.WriteLine($"{LogPrefix}No more parent elements to check for {name}");
                return null;
            }

            var parent = element.ParentElement;
            Console.WriteLine($"{LogPrefix}Checking parent {parent.NodeName} for {name} (depth: {depth})");

            var value = _registry.GetVariableValue(name);
            if (value != null)
            {
                Console.WriteLine($"{LogPrefix}Found {name} in registry at parent level {depth}: {value.CssText}");
                return value;
            }

            Console.WriteLine($"{LogPrefix}Not found at current level, checking next parent");
            return TryGetInheritedValue(name, parent, depth + 1);
        }
    }
}