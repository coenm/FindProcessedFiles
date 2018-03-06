﻿namespace EagleEye.Picasa.Test.IniParser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using FluentAssertions;

    using Xunit;

    using Sut = EagleEye.Picasa.IniParser.SimpleIniParser;

    public class SimpleIniParserTest
    {
        [Fact]
        public void ParseNullStreamShouldThrowArgumentNullExceptionTest()
        {
            Assert.Throws<ArgumentNullException>(() => Sut.Parse(null));
        }

        [Fact]
        public void EmptyIniFileShouldResultInAnEmptyResultTest()
        {
            // arrange
            using (var stream = GenerateStreamFromString(string.Empty))
            {
                // act
                var result = Sut.Parse(stream);

                // assert
                result.Should().BeEmpty();
            }
        }

        [Fact]
        public void CommentLinesAreIgnoredTest()
        {
            // arrange
            const string CONTENT = @"
[Section1]
  key=value
a = b  
; comment
b=c
";
            using (var stream = GenerateStreamFromString(CONTENT))
            {
                // act
                var result = Sut.Parse(stream);

                // assert
                result.Count.Should().Be(1);
                result[0].Section.Should().Be("Section1");
                result[0].Content.Should()
                         .BeEquivalentTo(new Dictionary<string, string>
                                             {
                                                 { "key", "value" },
                                                 { "a", "b" },
                                                 { "b", "c" }
                                             });
            }
        }

        [Fact]
        public void InvalidSectionShouldThrowExceptionTest()
        {
            // arrange
            const string CONTENT = "[Abc\r\nkey=value\r\n";
            using (var stream = GenerateStreamFromString(CONTENT))
            {
                // act
                // assert
                Assert.Throws<ArgumentException>(() => Sut.Parse(stream));
            }
        }

        private static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }
    }
}