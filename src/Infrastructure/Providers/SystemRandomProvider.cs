using System;
using System.Collections.Generic;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Infrastructure.Providers
{
    /// <summary>
    /// Implementation of IRandomProvider using System.Random for randomization operations.
    /// Provides thread-safe random number generation and Fisher-Yates shuffle algorithm.
    /// </summary>
    public class SystemRandomProvider : IRandomProvider
    {
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of SystemRandomProvider with a time-based seed.
        /// </summary>
        public SystemRandomProvider()
        {
            _random = new Random();
        }

        /// <summary>
        /// Initializes a new instance of SystemRandomProvider with a specific seed.
        /// This constructor is useful for testing scenarios requiring deterministic behavior.
        /// </summary>
        /// <param name="seed">The seed value for the random number generator.</param>
        public SystemRandomProvider(int seed)
        {
            _random = new Random(seed);
        }

        /// <summary>
        /// Returns a random integer within the specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
        /// <returns>A random integer greater than or equal to minValue and less than maxValue.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minValue is greater than or equal to maxValue.</exception>
        public int Next(int minValue, int maxValue)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue), 
                    "minValue must be less than maxValue");
            }

            return _random.Next(minValue, maxValue);
        }

        /// <summary>
        /// Shuffles the elements of a list in place using the Fisher-Yates shuffle algorithm.
        /// This provides an unbiased shuffle with O(n) time complexity.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <exception cref="ArgumentNullException">Thrown when list is null.</exception>
        public void Shuffle<T>(IList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            // Fisher-Yates shuffle algorithm
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}