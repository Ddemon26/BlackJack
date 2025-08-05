using System.Collections.Generic;
using System.Linq;
using GroupProject.Domain.Interfaces;
using GroupProject.Infrastructure.Extensions;
using Moq;
using Xunit;

namespace GroupProject.Tests.Infrastructure
{
    public class CollectionExtensionsTests
    {
        [Fact]
        public void Shuffle_CallsRandomProviderShuffle()
        {
            // Arrange
            var mockRandomProvider = new Mock<IRandomProvider>();
            var list = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            list.Shuffle(mockRandomProvider.Object);

            // Assert
            mockRandomProvider.Verify(x => x.Shuffle(list), Times.Once);
        }

        [Fact]
        public void ToShuffledList_ReturnsNewListWithSameElements()
        {
            // Arrange
            var mockRandomProvider = new Mock<IRandomProvider>();
            var originalList = new List<int> { 1, 2, 3, 4, 5 };
            
            // Setup mock to not actually shuffle (for predictable test)
            mockRandomProvider.Setup(x => x.Shuffle(It.IsAny<IList<int>>()));

            // Act
            var shuffledList = originalList.ToShuffledList(mockRandomProvider.Object);

            // Assert
            Assert.NotSame(originalList, shuffledList);
            Assert.Equal(originalList.Count, shuffledList.Count);
            Assert.True(originalList.All(x => shuffledList.Contains(x)));
        }

        [Fact]
        public void ToShuffledList_DoesNotModifyOriginalList()
        {
            // Arrange
            var mockRandomProvider = new Mock<IRandomProvider>();
            var originalList = new List<int> { 1, 2, 3, 4, 5 };
            var expectedOriginal = new List<int>(originalList);

            // Setup mock to reverse the list for a predictable change
            mockRandomProvider.Setup(x => x.Shuffle(It.IsAny<IList<int>>()))
                .Callback<IList<int>>(list =>
                {
                    var temp = list.ToList();
                    list.Clear();
                    for (int i = temp.Count - 1; i >= 0; i--)
                    {
                        list.Add(temp[i]);
                    }
                });

            // Act
            var shuffledList = originalList.ToShuffledList(mockRandomProvider.Object);

            // Assert
            Assert.Equal(expectedOriginal, originalList);
            Assert.NotEqual(originalList, shuffledList);
        }

        [Fact]
        public void ToShuffledList_CallsRandomProviderShuffle()
        {
            // Arrange
            var mockRandomProvider = new Mock<IRandomProvider>();
            var originalList = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            var shuffledList = originalList.ToShuffledList(mockRandomProvider.Object);

            // Assert
            mockRandomProvider.Verify(x => x.Shuffle(It.IsAny<IList<int>>()), Times.Once);
        }

        [Fact]
        public void ToShuffledList_WithEmptySource_ReturnsEmptyList()
        {
            // Arrange
            var mockRandomProvider = new Mock<IRandomProvider>();
            var emptyList = new List<int>();

            // Act
            var result = emptyList.ToShuffledList(mockRandomProvider.Object);

            // Assert
            Assert.Empty(result);
            Assert.NotSame(emptyList, result);
        }
    }
}