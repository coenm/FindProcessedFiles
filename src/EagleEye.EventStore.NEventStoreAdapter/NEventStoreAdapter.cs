﻿namespace EagleEye.EventStore.NEventStoreAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using CQRSlite.Events;
    using Dawn;
    using JetBrains.Annotations;
    using NEventStore;

    public class NEventStoreAdapter : IEventStore
    {
        private readonly IEventPublisher publisher;
        private readonly IStoreEvents store;

        public NEventStoreAdapter([NotNull] IEventPublisher publisher, NEventStore.IStoreEvents store)
        {
            Guard.Argument(publisher, nameof(publisher)).NotNull();
            Guard.Argument(store, nameof(store)).NotNull();

            this.publisher = publisher;
            this.store = store;
        }

        public async Task Save(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var @event in events)
            {
                using (var stream = store.OpenStream(@event.Id))
                {
                    stream.Add(new EventMessage { Body = @event, });

                    // not sure yet what it means to have a commit id.
                    stream.CommitChanges(Guid.NewGuid());
                }

                await publisher.Publish(@event, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<IEnumerable<IEvent>> Get(Guid aggregateId, int fromVersion, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var stream = store.OpenStream(aggregateId, 0))
            {
                return Task.FromResult((IEnumerable<IEvent>)stream.CommittedEvents
                                                                  .Select(x => x.Body as IEvent)
                                                                  .Where(y => y != null && y.Version > fromVersion)
                                                                  .OrderBy(y => y.Version)
                                                                  .ToArray());
            }

/*
            using (var stream = store.OpenStream(aggregateId, fromVersion))
            {
                return Task.FromResult((IEnumerable<IEvent>)stream.CommittedEvents
                                                                  .Select(x => x.Body as IEvent)
                                                                  .Where(y => y != null)
                                                                  .ToArray());
            }
*/
        }
    }
}
