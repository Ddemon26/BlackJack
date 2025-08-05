using System.Collections.Generic;

namespace GroupProject.Domain.Interfaces
{
    /// <summary>
    /// Provides abstraction for random number generation and collection shuffling operations.
    /// This interface enables testable randomization by allowing mock implementations.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>
        /// Returns a random integer within the specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
        /// <returns>A random integer greater than or equal to minValue and less than maxValue.</returns>
        int Next(int minValue, int maxValue);

        /// <summary>
        /// Shuffles the elements of a list in place using a randomization algorithm.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        void Shuffle<T>(IList<T> list);
    }
}