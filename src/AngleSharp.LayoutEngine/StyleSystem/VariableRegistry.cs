namespace AngleSharp.LayoutEngine.StyleSystem
{
    using System;
    using System.Collections.Generic;
    using AngleSharp.Css;
    using AngleSharp.Css.Dom;

    /// <summary>
    /// Tracks and manages CSS custom property (variable) definitions in the style cascade.
    /// </summary>
    public class VariableRegistry
    {
        private readonly Dictionary<string, VariableDefinition> _variables = new Dictionary<string, VariableDefinition>();

        /// <summary>
        /// Registers a CSS custom property with cascade information.
        /// </summary>
        /// <param name="name">The variable name (starting with --)</param>
        /// <param name="value">The variable value</param>
        /// <param name="origin">The stylesheet origin (user agent, user, author)</param>
        /// <param name="specificity">The selector specificity</param>
        /// <param name="isImportant">Whether the declaration has !important flag</param>
        public void RegisterVariable(string name, ICssValue value, StylesheetOrigin origin, Priority specificity, bool isImportant = false)
        {
            if (string.IsNullOrEmpty(name) || !name.StartsWith("--"))
            {
                throw new ArgumentException("Variable name must start with -- prefix", nameof(name));
            }

            var definition = new VariableDefinition(name, value, origin, specificity, isImportant);

            // Check if this definition should replace an existing one (cascade rules)
            if (_variables.TryGetValue(name, out var existing))
            {
                if (ShouldOverrideDefinition(existing, definition))
                {
                    _variables[name] = definition;
                }
            }
            else
            {
                _variables[name] = definition;
            }
        }

        /// <summary>
        /// Gets a variable value by name.
        /// </summary>
        /// <param name="name">The variable name (starting with --)</param>
        /// <returns>The variable value if found, otherwise null</returns>
        public ICssValue? GetVariableValue(string name)
        {
            return _variables.TryGetValue(name, out var definition)
                ? definition.Value
                : null;
        }

        /// <summary>
        /// Returns all registered variable names.
        /// </summary>
        /// <returns>A collection of variable names</returns>
        public IEnumerable<string> GetVariableNames()
        {
            return _variables.Keys;
        }

        /// <summary>
        /// Clears all registered variables.
        /// </summary>
        public void Clear()
        {
            _variables.Clear();
        }

        /// <summary>
        /// Determines if a new variable definition should override an existing one
        /// based on CSS cascade rules.
        /// </summary>
        private bool ShouldOverrideDefinition(VariableDefinition existing, VariableDefinition newDef)
        {
            // Important flag has highest priority
            if (newDef.IsImportant && !existing.IsImportant)
                return true;
            if (existing.IsImportant && !newDef.IsImportant)
                return false;

            // Then compare origin (Author > User > UserAgent)
            if (newDef.Origin > existing.Origin)
                return true;
            if (existing.Origin > newDef.Origin)
                return false;

            // Then compare specificity
            if (newDef.Specificity > existing.Specificity)
                return true;
            if (existing.Specificity > newDef.Specificity)
                return false;

            // Finally, use document order
            return true; // Last one wins
        }

        /// <summary>
        /// Container for variable definition information including cascade data.
        /// </summary>
        private class VariableDefinition
        {
            public string Name { get; }
            public ICssValue Value { get; }
            public StylesheetOrigin Origin { get; }
            public Priority Specificity { get; }
            public bool IsImportant { get; }

            public VariableDefinition(string name, ICssValue value, StylesheetOrigin origin, Priority specificity, bool isImportant)
            {
                Name = name;
                Value = value;
                Origin = origin;
                Specificity = specificity;
                IsImportant = isImportant;
            }
        }
    }
}