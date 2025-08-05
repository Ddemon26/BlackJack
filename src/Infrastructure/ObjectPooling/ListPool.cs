using System;
using System.Collections.Generic;

namespace GroupProject.Infrastructure.ObjectPooling;

/// <summary>
/// Specialized object pool for List&lt;T&gt; instances to reduce allocations in hot paths.
/// Provides static methods for getting and returning List instances with automatic clearing.
/// </summary>
/// <typeparam name="T">The type of elements in the lists.</typeparam>
/// <remarks>
/// This pool is particularly useful for temporary collections that are frequently created
/// and discarded. Lists are automatically cleared when returned to the pool, ensuring
/// they are ready for reuse without any residual data.
/// </remarks>
/// <example>
/// <code>
/// var list = ListPool&lt;string&gt;.Get();
/// try
/// {
///     list.Add("item1");
///     list.Add("item2");
///     // Use the list...
/// }
/// finally
/// {
///     ListPool&lt;string&gt;.Return(list);
/// }
/// </code>
/// </example>
public static class ListPool<T>
{
    private static readonly DefaultObjectPool<List<T>> Pool = new(
        objectFactory: () => new List<T>(),
        resetAction: list => list.Clear(),
        maxCapacity: 50
    );

    /// <summary>
    /// Gets a list from the pool.
    /// </summary>
    /// <returns>A cleared list instance.</returns>
    public static List<T> Get()
    {
        return Pool.Get();
    }

    /// <summary>
    /// Returns a list to the pool after clearing it.
    /// </summary>
    /// <param name="list">The list to return to the pool.</param>
    public static void Return(List<T> list)
    {
        Pool.Return(list);
    }

    /// <summary>
    /// Gets the current number of lists in the pool.
    /// </summary>
    public static int Count => Pool.Count;

    /// <summary>
    /// Clears all lists from the pool.
    /// </summary>
    public static void Clear()
    {
        Pool.Clear();
    }
}