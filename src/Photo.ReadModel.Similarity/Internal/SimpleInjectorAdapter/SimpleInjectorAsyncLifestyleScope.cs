﻿namespace EagleEye.Photo.ReadModel.Similarity.Internal.SimpleInjectorAdapter
{
    using System;

    using Dawn;
    using Hangfire;
    using JetBrains.Annotations;
    using SimpleInjector;
    using SimpleInjector.Lifestyles;

    internal class SimpleInjectorAsyncLifestyleScope : JobActivatorScope
    {
        private readonly Container container;
        private readonly Scope scope;

        public SimpleInjectorAsyncLifestyleScope([NotNull] Container container)
        {
            Guard.Argument(container, nameof(container)).NotNull();

            this.container = container;
            scope = AsyncScopedLifestyle.BeginScope(container);
        }

        public override object Resolve(Type type)
        {
            return container.GetInstance(type);
        }

        public override void DisposeScope()
        {
            scope.Dispose();
        }
    }
}
