﻿namespace EagleEye.Core.Domain.Events
{
    using System;

    using CQRSlite.Events;

    using EagleEye.Core.Domain.Entities;

    public class LocationSetToMediaItem : IEvent
    {
        public LocationSetToMediaItem(Guid id, Location location)
        {
            Id = id;
            Location = location;
        }

        public Guid Id { get; set; }

        public Location Location { get; }

        public int Version { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}