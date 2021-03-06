﻿namespace Photo.ReadModel.SearchEngineLucene.Test.Internal.LuceneDirectoryFactories
{
    using EagleEye.Photo.ReadModel.SearchEngineLucene.Internal.LuceneDirectoryFactories;
    using FluentAssertions;
    using Lucene.Net.Store;
    using Xunit;

    public class RamLuceneDirectoryFactoryTest
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
