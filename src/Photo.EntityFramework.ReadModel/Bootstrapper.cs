﻿namespace Photo.EntityFramework.ReadModel
{
    using System;

    using EagleEye.Core.Interfaces;
    using Helpers.Guards;
    using JetBrains.Annotations;
    using Microsoft.EntityFrameworkCore;
    using Photo.EntityFramework.ReadModel.Interface;
    using Photo.EntityFramework.ReadModel.Internal;
    using Photo.EntityFramework.ReadModel.Internal.EntityFramework;
    using Photo.EntityFramework.ReadModel.Internal.EventHandlers;
    using SimpleInjector;

    public static class Bootstrapper
    {
        /// <summary> Bootstrap this module.</summary>
        /// <param name="container">The IOC container. Cannot be <c>null</c>.</param>
        /// <param name="connectionString">Connection string to be used in EntityFramework. Cannot be <c>null</c> or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown when one of the required arguments is <c>null</c>.</exception>
        public static void BootstrapEntityFrameworkReadModel([NotNull] Container container, [NotNull] string connectionString)
        {
            Guard.NotNull(container, nameof(container));
            Guard.NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            var thisAssembly = typeof(Bootstrapper).Assembly;

            container.Register<IEagleEyeRepository, EntityFrameworkEagleEyeRepository>();
            container.Collection.Register(typeof(IDbContextOptionsStrategy), thisAssembly);

            container.Register<DbContextOptionsFactory>(Lifestyle.Singleton);
            container.Register<DbContextOptions<EagleEyeDbContext>>(() => container.GetInstance<DbContextOptionsFactory>().Create(connectionString), Lifestyle.Singleton);
            container.Register<IEagleEyeDbContextFactory, EagleEyeDbContextFactory>(Lifestyle.Singleton);

            container.Register<IReadModelEntityFramework, ReadModelEntityFramework>();
            container.Register<MediaItemConsistency>();

            container.Collection.Append<IEagleEyeInitialize, ModuleInitializer>();
        }

        public static Type[] GetEventHandlerTypes()
        {
            return new Type[]
            {
                typeof(MediaItemConsistency),
            };
        }
    }
}