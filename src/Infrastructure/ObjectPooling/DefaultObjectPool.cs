using System;
using System.Collections.Concurrent;

namespace GroupProject.Infrastructure.ObjectPooling;

/// <summary>
/// Thread-safe object pool implementation with configurable capacity and factory methods.
/// Uses a concurrent queue internally to provide thread-safe access to pooled objects.
/// </summary>
/// <typeparam name="T">The type of objects to pool. Must be a reference type.</typeparam>
/// <remarks>
/// This implementation uses a <see cref="ConcurrentQueue{T}"/> for thread-safe operations
/// and includes capacity management to prevent unbounded growth. Objects are reset
/// using the provided reset action before being returned to the pool.
/// </remarks>
public class DefaultObjectPool<T> : IObjectPool<T> where T : class
{
    private readonly ConcurrentQueue<T> _objects = new();
    private readonly Func<T> _objectFactory;
    private readonly Action<T>? _resetAction;
    private readonly int _maxCapacity;
    private int _currentCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultObjectPool{T}"/> class.
    /// </summary>
    /// <param name="objectFactory">Factory function to create new objects.</param>
    /// <param name="resetAction">Optional action to reset objects before returning to pool.</param>
    /// <param name="maxCapacity">Maximum number of objects to keep in the pool.</param>
    public DefaultObjectPool(Func<T> objectFactory, Action<T>? resetAction = null, int maxCapacity = 100)
    {
        _objectFactory = objectFactory ?? throw new ArgumentNullException(nameof(objectFactory));
        _resetAction = resetAction;
        _maxCapacity = maxCapacity;
    }

    /// <inheritdoc />
    public T Get()
    {
        if (_objects.TryDequeue(out var item))
        {
            Interlocked.Decrement(ref _currentCount);
            return item;
        }

        return _objectFactory();
    }

    /// <inheritdoc />
    public void Return(T item)
    {
        if (item == null)
            return;

        // Don't exceed max capacity
        if (_currentCount >= _maxCapacity)
            return;

        // Reset the object if a reset action is provided
        _resetAction?.Invoke(item);

        _objects.Enqueue(item);
        Interlocked.Increment(ref _currentCount);
    }

    /// <inheritdoc />
    public int Count => _currentCount;

    /// <inheritdoc />
    public void Clear()
    {
        while (_objects.TryDequeue(out _))
        {
            Interlocked.Decrement(ref _currentCount);
        }
    }
}