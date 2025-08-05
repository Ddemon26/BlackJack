using System;
using System.Text;

namespace GroupProject.Infrastructure.ObjectPooling;

/// <summary>
/// Specialized object pool for StringBuilder instances to optimize string formatting operations.
/// Provides static methods for getting and returning StringBuilder instances with automatic clearing.
/// </summary>
/// <remarks>
/// This pool helps reduce memory allocations when building strings, particularly in scenarios
/// where many temporary strings are created. StringBuilders are automatically cleared when
/// returned to the pool and have their capacity managed to prevent memory bloat.
/// </remarks>
/// <example>
/// <code>
/// var sb = StringBuilderPool.Get();
/// try
/// {
///     sb.Append("Hello");
///     sb.Append(" ");
///     sb.Append("World");
///     var result = sb.ToString();
/// }
/// finally
/// {
///     StringBuilderPool.Return(sb);
/// }
/// </code>
/// </example>
public static class StringBuilderPool
{
    private static readonly DefaultObjectPool<StringBuilder> Pool = new(
        objectFactory: () => new StringBuilder(256), // Start with reasonable capacity
        resetAction: sb => sb.Clear(),
        maxCapacity: 20
    );

    /// <summary>
    /// Gets a StringBuilder from the pool.
    /// </summary>
    /// <returns>A cleared StringBuilder instance.</returns>
    public static StringBuilder Get()
    {
        return Pool.Get();
    }

    /// <summary>
    /// Returns a StringBuilder to the pool after clearing it.
    /// </summary>
    /// <param name="stringBuilder">The StringBuilder to return to the pool.</param>
    public static void Return(StringBuilder stringBuilder)
    {
        if (stringBuilder == null)
            return;

        // Don't pool very large StringBuilders to avoid memory bloat
        if (stringBuilder.Capacity > 4096)
        {
            return;
        }

        Pool.Return(stringBuilder);
    }

    /// <summary>
    /// Gets the current number of StringBuilders in the pool.
    /// </summary>
    public static int Count => Pool.Count;

    /// <summary>
    /// Clears all StringBuilders from the pool.
    /// </summary>
    public static void Clear()
    {
        Pool.Clear();
    }
}