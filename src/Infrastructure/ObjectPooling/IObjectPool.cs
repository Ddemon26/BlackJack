using System;

namespace GroupProject.Infrastructure.ObjectPooling;

/// <summary>
/// Generic interface for object pooling to reduce memory allocations and improve performance.
/// Provides a contract for managing reusable object instances in a thread-safe manner.
/// </summary>
/// <typeparam name="T">The type of objects to pool. Must be a reference type.</typeparam>
/// <remarks>
/// Object pools are used to reduce garbage collection pressure by reusing objects
/// instead of creating new instances. This is particularly beneficial for frequently
/// allocated objects like collections, StringBuilder instances, and other temporary objects.
/// </remarks>
public interface IObjectPool<T> where T : class
{
    /// <summary>
    /// Gets an object from the pool or creates a new one if the pool is empty.
    /// </summary>
    /// <returns>An object instance.</returns>
    T Get();

    /// <summary>
    /// Returns an object to the pool for reuse.
    /// </summary>
    /// <param name="item">The object to return to the pool.</param>
    void Return(T item);

    /// <summary>
    /// Gets the current number of objects in the pool.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Clears all objects from the pool.
    /// </summary>
    void Clear();
}