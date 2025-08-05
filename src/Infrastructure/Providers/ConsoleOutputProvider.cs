using System;
using System.Threading.Tasks;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Infrastructure.Providers
{
    /// <summary>
    /// Console implementation of IOutputProvider for writing output to the console.
    /// Provides thread-safe console output operations with async support.
    /// </summary>
    public class ConsoleOutputProvider : IOutputProvider
    {
        private readonly object _lockObject = new();

        /// <summary>
        /// Writes a line of text to the console followed by a line terminator.
        /// </summary>
        /// <param name="message">The text to write. If null, only the line terminator is written.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public Task WriteLineAsync(string? message = null)
        {
            return Task.Run(() =>
            {
                lock (_lockObject)
                {
                    Console.WriteLine(message);
                }
            });
        }

        /// <summary>
        /// Writes text to the console without a line terminator.
        /// </summary>
        /// <param name="message">The text to write.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public Task WriteAsync(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return Task.Run(() =>
            {
                lock (_lockObject)
                {
                    Console.Write(message);
                }
            });
        }

        /// <summary>
        /// Clears the console display.
        /// </summary>
        /// <returns>A task that represents the asynchronous clear operation.</returns>
        public Task ClearAsync()
        {
            return Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        Console.Clear();
                    }
                    catch (System.IO.IOException)
                    {
                        // Console.Clear() can fail in test environments or when output is redirected
                        // This is acceptable behavior - we'll silently ignore the error
                    }
                }
            });
        }

        /// <summary>
        /// Writes a formatted line of text to the console.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public Task WriteLineAsync(string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return Task.Run(() =>
            {
                lock (_lockObject)
                {
                    Console.WriteLine(format, args);
                }
            });
        }

        /// <summary>
        /// Writes formatted text to the console without a line terminator.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public Task WriteAsync(string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return Task.Run(() =>
            {
                lock (_lockObject)
                {
                    Console.Write(format, args);
                }
            });
        }
    }
}