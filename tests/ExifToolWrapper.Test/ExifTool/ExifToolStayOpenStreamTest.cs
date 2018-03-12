﻿namespace EagleEye.ExifToolWrapper.Test.ExifTool
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using EagleEye.ExifToolWrapper.ExifTool;

    using FluentAssertions;

    using Xunit;

    public class ExifToolStayOpenStreamTest : IDisposable
    {
        private readonly ExifToolStayOpenStream _sut;
        private readonly List<DataCapturedArgs> _capturedEvents;

        public ExifToolStayOpenStreamTest()
        {
            _capturedEvents = new List<DataCapturedArgs>();
            _sut = new ExifToolStayOpenStream(Encoding.UTF8);
            _sut.Update += SutOnUpdate;
        }

        public void Dispose()
        {
            _sut.Update -= SutOnUpdate;
            _sut?.Dispose();
        }

        [Fact]
        public void ExifToolStayOpenStreamCtorThrowsArgumentOutOfRangeWhenBuffersizeIsNegativeTest()
        {
            // arrange

            // act
            Action act = () => _ = new ExifToolStayOpenStream(null, -1);

            // assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void DefaultPropertiesShouldNoThrowAndDoNotDoAnythingTest()
        {
            _sut.CanWrite.Should().BeTrue();
            _sut.CanRead.Should().BeFalse();
            _sut.CanSeek.Should().BeFalse();

            // nothing is written yet.
            _sut.Length.Should().Be(0);
            _sut.Position.Should().Be(0);
        }

        [Fact]
        public void SetPositionShouldNotDoAnythingTest()
        {
            // arrange

            // assume
            _sut.Position.Should().Be(0);

            // act
            _sut.Position = 100;

            // assert
            _sut.Position.Should().Be(0);
        }

        [Fact]
        public void FlushShouldNotDoAnythingAndDefinitelyNotThrowTest()
        {
            _sut.Flush();
        }

        [Fact]
        public void SeekAlwaysReturnsZeroTest()
        {
            // arrange

            // act
            var result = _sut.Seek(0, SeekOrigin.Begin);

            // assert
            result.Should().Be(0);
        }

        [Fact]
        public void SetLengthDoesntDoAnythingTest()
        {
            // arrange

            // assume
            _sut.Length.Should().Be(0);

            // act
            _sut.SetLength(100);

            // assert
            _sut.Length.Should().Be(0);
        }


        [Fact]
        public void ReadThrowsTest()
        {
            // arrange
            var buffer = new byte[100];

            // act
            Action act = () => _ = _sut.Read(buffer, 0, 100);

            // assert
            act.Should().Throw<NotSupportedException>();
        }


        [Fact]
        public void SingleWriteShouldNotFireEvent()
        {
            // arrange
            const string MSG = "dummy data without delimitor";

            // act
            WriteMessageToSut(MSG);

            // assert
            _capturedEvents.Should().BeEmpty();
        }

        [Fact]
        public void ParseSingleMessage()
        {
            // arrange
            const string MSG = "a b c\r\nd e f\r\n{ready0}\r\n";

            // act
            WriteMessageToSut(MSG);

            // assert
            _capturedEvents.Should().HaveCount(1);
            _capturedEvents.First().Key.Should().Be("0");
            _capturedEvents.First().Data.Should().Be("a b c\r\nd e f".ConvertToOsString());
        }

        [Fact]
        public void ParseTwoMessagesInSingleWrite()
        {
            // arrange
            const string MSG = "a b c\r\n{ready0}\r\nd e f\r\n{ready1}\r\nxyz";

            // act
            WriteMessageToSut(MSG);

            // assert
            _capturedEvents.Should().HaveCount(2);

            _capturedEvents[0].Key.Should().Be("0");
            _capturedEvents[0].Data.Should().Be("a b c");

            _capturedEvents[1].Key.Should().Be("1");
            _capturedEvents[1].Data.Should().Be("d e f");
        }

        [Fact]
        public void ParseTwoMessagesOverFourWrites()
        {
            // arrange
            const string MSG1 = "a b c\r\nd e f\r\n{ready0}\r\nghi";
            const string MSG2 = " jkl\r\n{re";
            const string MSG3 = "ady";
            const string MSG4 = "213";
            const string MSG5 = "3}\r\n";

            // act
            WriteMessageToSut(MSG1);
            WriteMessageToSut(MSG2);
            WriteMessageToSut(MSG3);
            WriteMessageToSut(MSG4);
            WriteMessageToSut(MSG5);

            // assert
            _capturedEvents.Should().HaveCount(2)
                           .And.Contain(x => x.Key == "0" && x.Data == "a b c\r\nd e f".ConvertToOsString())
                           .And.Contain(x => x.Key == "2133" && x.Data == "ghi jkl".ConvertToOsString());
        }

        private void WriteMessageToSut(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message.ConvertToOsString());
            _sut.Write(buffer, 0, buffer.Length);
        }

        private void SutOnUpdate(object sender, DataCapturedArgs dataCapturedArgs)
        {
            _capturedEvents.Add(dataCapturedArgs);
        }
    }
}