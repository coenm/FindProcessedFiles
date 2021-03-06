﻿namespace EagleEye.Photo.Domain.Test.Events
{
    using System;

    using EagleEye.Photo.Domain.Aggregates;
    using EagleEye.Photo.Domain.Events;
    using FluentAssertions;
    using Newtonsoft.Json;
    using Xunit;

    public class JsonSerializable
    {
        [Fact]
        public void DateTimeTakenChanged_ShouldBeSerializeAndDeserialize()
        {
            // arrange
            var sut = new DateTimeTakenChanged
            {
                DateTimeTaken = Timestamp.Create(2000, 12, 21, 23),
                Id = Guid.Parse("D44C6C8F-D2DA-4CAE-B5FD-770421541152"),
                Version = 23,
                TimeStamp = new DateTimeOffset(),
            };

            // act
            var result = JsonConvert.DeserializeObject<DateTimeTakenChanged>(JsonConvert.SerializeObject(sut));

            // assert
            result.Should().BeEquivalentTo(sut);
        }

        [Fact]
        public void TagsRemovedFromPhoto_ShouldBeSerializableAndDeserializable()
        {
            // arrange
            var sut = new TagsRemovedFromPhoto(Guid.Parse("D44C6C8F-D2DA-4CAE-B5FD-770421541152"), "a", "b")
            {
                Version = 23,
                TimeStamp = new DateTimeOffset(),
            };

            // act
            var result = JsonConvert.DeserializeObject<TagsRemovedFromPhoto>(JsonConvert.SerializeObject(sut));

            // assert
            result.Should().BeEquivalentTo(sut);
        }
    }
}
