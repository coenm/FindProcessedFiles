namespace SearchEngine.LuceneNet.Core.Test
{
    using FluentAssertions;

    using Lucene.Net.Store;

    using Xunit;

    public class RamDirectoryFactoryTest
    {
        [Fact]
        public void Create_ReturnsRAMDirectoryTest()
        {
            // arrange
            var sut = new RamLuceneDirectoryFactory();

            // act
            var result = sut.Create();

            // assert
            result.Should().BeOfType<RAMDirectory>();
        }
    }
}
