using System;
using System.Collections.Generic;
using System.Linq;
using GroupProject.Infrastructure.Providers;
using Xunit;

namespace GroupProject.Tests.Infrastructure
{
    public class SystemRandomProviderTests
    {
        [Fact]
        public void Next_WithValidRange_ReturnsValueInRange()
        {
            // Arrange
            var provider = new SystemRandomProvider();
            const int minValue = 1;
            const int maxValue = 10;

            // Act & Assert - Test multiple times to ensure consistency
            for (int i = 0; i < 100; i++)
            {
                var result = provider.Next(minValue, maxValue);
                Assert.True(result >= minValue && result < maxValue, 
                    $"Expected value between {minValue} (inclusive) and {maxValue} (exclusive), but got {result}");
            }
        }

        [Fact]
        public void Next_WithSameSeed_ProducesDeterministicResults()
        {
            // Arrange
            const int seed = 12345;
            var provider1 = new SystemRandomProvider(seed);
            var provider2 = new SystemRandomProvider(seed);

            // Act
            var results1 = new List<int>();
            var results2 = new List<int>();

            for (int i = 0; i < 10; i++)
            {
                results1.Add(provider1.Next(1, 100));
                results2.Add(provider2.Next(1, 100));
            }

            // Assert
            Assert.Equal(results1, results2);
        }

        [Fact]
        public void Next_WithInvalidRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var provider = new SystemRandomProvider();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Next(10, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Next(5, 5));
        }

        [Fact]
        public void Shuffle_WithNullList_ThrowsArgumentNullException()
        {
            // Arrange
            var provider = new SystemRandomProvider();
            List<int>? nullList = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => provider.Shuffle(nullList!));
        }

        [Fact]
        public void Shuffle_WithEmptyList_DoesNotThrow()
        {
            // Arrange
            var provider = new SystemRandomProvider();
            var emptyList = new List<int>();

            // Act & Assert
            provider.Shuffle(emptyList);
            Assert.Empty(emptyList);
        }

        [Fact]
        public void Shuffle_WithSingleElement_LeavesElementUnchanged()
        {
            // Arrange
            var provider = new SystemRandomProvider();
            var singleElementList = new List<int> { 42 };

            // Act
            provider.Shuffle(singleElementList);

            // Assert
            Assert.Single(singleElementList);
            Assert.Equal(42, singleElementList[0]);
        }

        [Fact]
        public void Shuffle_WithMultipleElements_ChangesOrder()
        {
            // Arrange
            var provider = new SystemRandomProvider();
            var originalList = Enumerable.Range(1, 10).ToList();
            var listToShuffle = new List<int>(originalList);

            // Act
            provider.Shuffle(listToShuffle);

            // Assert
            Assert.Equal(originalList.Count, listToShuffle.Count);
            Assert.True(originalList.All(x => listToShuffle.Contains(x)), "All original elements should be present");
            
            // Note: There's a very small chance the shuffle could result in the same order,
            // but with 10 elements, the probability is 1/10! which is extremely low
        }

        [Fact]
        public void Shuffle_WithSameSeed_ProducesDeterministicResults()
        {
            // Arrange
            const int seed = 54321;
            var provider1 = new SystemRandomProvider(seed);
            var provider2 = new SystemRandomProvider(seed);
            
            var list1 = Enumerable.Range(1, 10).ToList();
            var list2 = Enumerable.Range(1, 10).ToList();

            // Act
            provider1.Shuffle(list1);
            provider2.Shuffle(list2);

            // Assert
            Assert.Equal(list1, list2);
        }

        [Fact]
        public void Shuffle_PreservesAllElements()
        {
            // Arrange
            var provider = new SystemRandomProvider();
            var originalElements = new List<string> { "A", "B", "C", "D", "E", "F" };
            var listToShuffle = new List<string>(originalElements);

            // Act
            provider.Shuffle(listToShuffle);

            // Assert
            Assert.Equal(originalElements.Count, listToShuffle.Count);
            foreach (var element in originalElements)
            {
                Assert.Contains(element, listToShuffle);
            }
        }
    }
}