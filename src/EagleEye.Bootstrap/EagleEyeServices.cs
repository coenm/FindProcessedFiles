﻿namespace EagleEye.Bootstrap
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using EagleEye.Core.Interfaces.Module;
    using Helpers.Guards;
    using JetBrains.Annotations;
    using SimpleInjector;

    [PublicAPI]
    public class EagleEyeServices : IDisposable
    {
        [NotNull] private readonly Container container;

        private State state;

        [NotNull] private IEagleEyeProcess[] startedServices;

        public EagleEyeServices([NotNull] Container container)
        {
            Guard.NotNull(container, nameof(container));

            this.container = container;
            state = State.Empty;
            startedServices = new IEagleEyeProcess[0];
        }

        public async Task InitializeServices()
        {
            if (state == State.Empty)
            {
                var instancesToInitialize = GetAllInstancesOrEmpty<IEagleEyeInitialize>(container).ToArray();
                if (instancesToInitialize.Any())
                    await Task.WhenAll(instancesToInitialize.Select(instance => instance.InitializeAsync())).ConfigureAwait(false);

                state = State.Initialized;
            }
        }

        public void StartServices()
        {
            if (state != State.Initialized)
                return;

            startedServices = GetAllInstancesOrEmpty<IEagleEyeProcess>(container).ToArray();
            foreach (var eagleEyeProcess in startedServices)
            {
                eagleEyeProcess.Start();
            }

            state = State.Started;
        }

        public void StopServices()
        {
            if (state != State.Started)
                return;

            foreach (var eagleEyeProcess in startedServices)
            {
                eagleEyeProcess.Stop();
            }

            state = State.Stopped;
        }

        public void Dispose()
        {
            StopServices();

            // do not dispose the container as we don not own it.
        }

        [NotNull]
        private static IEnumerable<T> GetAllInstancesOrEmpty<T>([NotNull] Container container)
            where T : class
        {
            DebugGuard.NotNull(container, nameof(container));

            try
            {
                return container.GetAllInstances<T>();
            }
            catch (ActivationException)
            {
                return Enumerable.Empty<T>();
            }
        }

        private enum State
        {
            Empty = 0,
            Initialized = 1,
            Started = 2,
            Stopped = 3,
        }
    }
}