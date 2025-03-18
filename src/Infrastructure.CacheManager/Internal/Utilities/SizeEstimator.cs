namespace Infrastructure.CacheManager.Internal.Utilities;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides methods to estimate the memory size of objects.
/// This is a heuristic approach to size estimation, not exact measurement.
/// </summary>
internal static class SizeEstimator
{
    // Common sizes in bytes
    private const int ReferenceSize = 8; // 64-bit system reference size
    private const int ObjectHeaderSize = 16; // Base object overhead
    private const int ArrayHeaderSize = 24; // Array overhead
    private const int StringBaseSize = 24; // String overhead without chars
    private const int BoolSize = 1;
    private const int ByteSize = 1;
    private const int CharSize = 2;
    private const int Int16Size = 2;
    private const int Int32Size = 4;
    private const int Int64Size = 8;
    private const int SingleSize = 4;
    private const int DoubleSize = 8;
    private const int DecimalSize = 16;
    private const int DateTimeSize = 8;
    private const int GuidSize = 16;

    // Caches for reflection info to improve performance
    private static readonly ConditionalWeakTable<Type, TypeInfo> TypeInfoCache = new ConditionalWeakTable<Type, TypeInfo>();

    /// <summary>
    /// Estimates the memory size of an object in bytes.
    /// </summary>
    /// <param name="obj">The object to measure.</param>
    /// <returns>The estimated size in bytes.</returns>
    public static long EstimateSize(object? obj)
    {
        if (obj == null)
            return 0;

        // Use object tracking to handle circular references
        var visited = new HashSet<object>(new ReferenceEqualityComparer());
        return EstimateSizeRecursive(obj, visited, 1);
    }

    private static long EstimateSizeRecursive(object obj, HashSet<object> visited, int depth, int maxDepth = 10)
    {
        if (obj == null)
            return 0;

        // For very large or complex objects, apply depth limit to prevent stack overflow
        if (depth > maxDepth)
            return ReferenceSize;

        // Handle circular references
        if (!visited.Add(obj))
            return 0;

        Type type = obj.GetType();

        // Handle primitive types
        if (IsPrimitiveType(type))
            return GetPrimitiveSize(obj, type);

        // Handle string
        if (obj is string str)
            return StringBaseSize + str.Length * CharSize;

        // Handle arrays
        if (type.IsArray)
            return EstimateArraySize((Array)obj, visited, depth);

        // Handle collections
        if (obj is ICollection collection)
            return EstimateCollectionSize(collection, visited, depth);

        // Get or create type info
        var typeInfo = GetTypeInfo(type);

        // Start with object header size
        long size = ObjectHeaderSize;

        // Add size of fields
        foreach (var field in typeInfo.Fields)
        {
            object fieldValue = field.GetValue(obj);

            if (fieldValue == null)
                continue;

            if (field.FieldType.IsPrimitive || field.FieldType == typeof(string))
            {
                size += GetPrimitiveSize(fieldValue, field.FieldType);
            }
            else
            {
                size += EstimateSizeRecursive(fieldValue, visited, depth + 1, maxDepth);
            }
        }

        // Apply padding to align with memory allocation blocks (usually 8 bytes)
        return AlignSize(size);
    }

    private static long EstimateArraySize(Array array, HashSet<object> visited, int depth)
    {
        long size = ArrayHeaderSize;

        Type elementType = array.GetType().GetElementType();

        // For primitive arrays, use direct calculation
        if (IsPrimitiveType(elementType))
        {
            size += array.Length * GetPrimitiveTypeSize(elementType);
            return AlignSize(size);
        }

        // For reference arrays, add size of elements
        for (int i = 0; i < array.Length; i++)
        {
            object element = array.GetValue(i);
            if (element != null)
            {
                size += ReferenceSize;
                size += EstimateSizeRecursive(element, visited, depth + 1);
            }
        }

        return AlignSize(size);
    }

    private static long EstimateCollectionSize(ICollection collection, HashSet<object> visited, int depth)
    {
        // Start with collection object size
        long size = ObjectHeaderSize;

        // Add size for internal array/buckets/nodes (approximation)
        size += 8 * collection.Count; // Assuming each entry is about 8 bytes of overhead

        // Add size of elements
        foreach (object item in collection)
        {
            if (item != null)
            {
                size += ReferenceSize;
                size += EstimateSizeRecursive(item, visited, depth + 1);
            }
        }

        return AlignSize(size);
    }

    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(DateTime) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               type == typeof(decimal);
    }

    private static long GetPrimitiveSize(object obj, Type type)
    {
        if (obj == null)
            return 0;

        if (type == typeof(bool))
            return BoolSize;
        if (type == typeof(byte) || type == typeof(sbyte))
            return ByteSize;
        if (type == typeof(char))
            return CharSize;
        if (type == typeof(short) || type == typeof(ushort))
            return Int16Size;
        if (type == typeof(int) || type == typeof(uint))
            return Int32Size;
        if (type == typeof(long) || type == typeof(ulong))
            return Int64Size;
        if (type == typeof(float))
            return SingleSize;
        if (type == typeof(double))
            return DoubleSize;
        if (type == typeof(decimal))
            return DecimalSize;
        if (type == typeof(DateTime) || type == typeof(TimeSpan))
            return DateTimeSize;
        if (type == typeof(Guid))
            return GuidSize;
        if (type == typeof(string))
        {
            string str = (string)obj;
            return StringBaseSize + str.Length * CharSize;
        }

        // Default case
        return ReferenceSize;
    }

    private static int GetPrimitiveTypeSize(Type type)
    {
        if (type == typeof(bool))
            return BoolSize;
        if (type == typeof(byte) || type == typeof(sbyte))
            return ByteSize;
        if (type == typeof(char))
            return CharSize;
        if (type == typeof(short) || type == typeof(ushort))
            return Int16Size;
        if (type == typeof(int) || type == typeof(uint))
            return Int32Size;
        if (type == typeof(long) || type == typeof(ulong))
            return Int64Size;
        if (type == typeof(float))
            return SingleSize;
        if (type == typeof(double))
            return DoubleSize;
        if (type == typeof(decimal))
            return DecimalSize;
        if (type == typeof(DateTime) || type == typeof(TimeSpan))
            return DateTimeSize;
        if (type == typeof(Guid))
            return GuidSize;

        // Default case
        return ReferenceSize;
    }

    private static TypeInfo GetTypeInfo(Type type)
    {
        if (!TypeInfoCache.TryGetValue(type, out var typeInfo))
        {
            typeInfo = new TypeInfo(type);
            TypeInfoCache.Add(type, typeInfo);
        }

        return typeInfo;
    }

    private static long AlignSize(long size)
    {
        // Align to 8 bytes (typical memory allocation alignment)
        return (size + 7) & ~7;
    }

    /// <summary>
    /// Encapsulates reflection information about a type to improve performance.
    /// </summary>
    private class TypeInfo
    {
        public List<FieldInfo> Fields { get; }

        public TypeInfo(Type type)
        {
            // Get all instance fields, including private and inherited
            Fields = new List<FieldInfo>();
            CollectFields(type);
        }

        private void CollectFields(Type type)
        {
            if (type == null || type == typeof(object))
                return;

            // Get fields of the current type
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Fields.AddRange(fields);

            // Get fields from base type
            CollectFields(type.BaseType);
        }
    }

    /// <summary>
    /// Compares objects by reference equality for use in HashSet.
    /// </summary>
    private class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}