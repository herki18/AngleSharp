namespace AngleSharp.StyleSystem.Core;

using System.Collections.Generic;

/// <summary>
/// Contains metrics about property tree optimization effectiveness.
/// </summary>
public class OptimizationMetrics
{
    /// <summary>
    /// Gets the total number of properties before optimization.
    /// </summary>
    public long TotalPropertiesBeforeOptimization { get; set; }

    /// <summary>
    /// Gets the total number of properties after optimization.
    /// </summary>
    public long TotalPropertiesAfterOptimization { get; set; }

    /// <summary>
    /// Gets the number of shared property tree nodes.
    /// </summary>
    public int SharedNodeCount { get; set; }

    /// <summary>
    /// Gets the number of unique property tree nodes.
    /// </summary>
    public int UniqueNodeCount { get; set; }

    /// <summary>
    /// Gets the percentage of memory saved through optimization.
    /// </summary>
    public long MemorySavingsPercentage { get; set; }

    /// <summary>
    /// Gets the most frequently used properties.
    /// </summary>
    public Dictionary<string, int> MostFrequentProperties { get; set; } = new();

    /// <summary>
    /// Returns a string representation of the optimization metrics.
    /// </summary>
    public override string ToString()
    {
        return $"Memory Savings: {MemorySavingsPercentage}% " +
               $"(Before: {TotalPropertiesBeforeOptimization}, After: {TotalPropertiesAfterOptimization}) | " +
               $"Nodes: {UniqueNodeCount} unique, {SharedNodeCount} shared";
    }
}