﻿namespace EagleEye.ExifTool.Test.ExifTool
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using EagleEye.ExifTool.ExifTool;
    using EagleEye.TestHelper.XUnit;
    using FluentAssertions;
    using Xunit;
    using Xunit.Abstractions;

    [Xunit.Categories.IntegrationTest]
    public class MedallionShellAdapterTest : IDisposable
    {
        private const int FallbackTestTimeout = 5000;
        private readonly ITestOutputHelper output;
        private readonly ExifToolStayOpenStream stream;
        private readonly ManualResetEventSlim mreSutExited;
        private readonly MedallionShellAdapter sut;

        public MedallionShellAdapterTest(ITestOutputHelper output)
        {
            this.output = output;
            mreSutExited = new ManualResetEventSlim(false);
            var defaultArgs = new List<string>
                                    {
                                        ExifToolArguments.StayOpen,
                                        ExifToolArguments.BoolTrue,
                                        "-@",
                                        "-",
                                    };

            stream = new ExifToolStayOpenStream(Encoding.UTF8);

            sut = new MedallionShellAdapter(ExifToolSystemConfiguration.ExifToolExecutable, defaultArgs, stream);
            sut.ProcessExited += SutOnProcessExited;
        }

        public void Dispose()
        {
            sut.ProcessExited -= SutOnProcessExited;
            stream.Dispose();
        }

        [Fact]
        [ExifTool]
        public async Task KillingSutShouldInvokeProcessExitedEventTest()
        {
            // arrange

            // assume
            sut.Finished.Should().BeFalse();

            // act
            sut.Kill();
            await sut.Task.ConfigureAwait(false);

            // assert
            AssertSutFinished(FallbackTestTimeout);
        }

        [Fact]
        [ExifTool]
        public async Task SettingStayOpenToFalseShouldCloseSutTest()
        {
            // arrange

            // assume
            sut.Finished.Should().BeFalse();

            // act
            await sut.WriteLineAsync(ExifToolArguments.StayOpen).ConfigureAwait(false);
            await sut.WriteLineAsync(ExifToolArguments.BoolFalse).ConfigureAwait(false);

            output.WriteLine("Awaiting task to finish");
            await sut.Task.ConfigureAwait(false);
            output.WriteLine("Task finished");

            // assert
            AssertSutFinished(FallbackTestTimeout);
        }

        private void AssertSutFinished(int timeout = 0)
        {
            mreSutExited.Wait(timeout);
            mreSutExited.IsSet.Should().BeTrue($"{nameof(sut.ProcessExited)} event should have been fired.");
            sut.Task.IsCompleted.Should().BeTrue("Task should have been completed.");
            sut.Finished.Should().BeTrue($"{nameof(sut.Finished)} property should be true.");
        }

        private void SutOnProcessExited(object sender, EventArgs eventArgs)
        {
            mreSutExited.Set();
        }
    }
}