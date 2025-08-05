using System.Collections.Generic;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for collection operations, particularly shuffling.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Shuffles the elements of a list in place using the provided random provider.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <param name="randomProvider">The random provider to use for shuffling.</param>
        public static void Shuffle<T>(this IList<T> list, IRandomProvider randomProvider)
        {
            randomProvider.Shuffle(list);
        }

        /// <summary>
        /// Returns a new shuffled copy of the list using the provided random provider.
        /// The original list remains unchanged.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="source">The source list to shuffle.</param>
        /// <param name="randomProvider">The random provider to use for shuffling.</param>
        /// <returns>A new list containing the shuffled elements.</returns>
        public static List<T> ToShuffledList<T>(this IEnumerable<T> source, IRandomProvider randomProvider)
        {
            var list = new List<T>(source);
            randomProvider.Shuffle(list);
            return list;
        }
    }
}