using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Diagnostics;
using OptiKey.ET5.Plugin.Runtime;
using OptiKey.ET5.Plugin.Runtime.Discovery;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class RuntimeDiscoveryTests
    {
        [Test]
        public void KnownPathDiscovery_ReturnsStandardExpectedPaths()
        {
            var source = new KnownPathDiscoverySource();
            var candidates = source.DiscoverCandidatePaths().ToList();

            Assert.That(source.SourceName, Is.EqualTo("KnownPaths"));
            Assert.That(candidates, Is.Not.Empty);
            Assert.That(candidates, Has.All.EndWith("tobii_stream_engine.dll"));
        }

        [Test]
        public void DeveloperExplicitPath_DiscoversExplicitAndEnvVarPaths()
        {
            string testPath1 = @"C:\CustomTobii\tobii_stream_engine.dll";
            string testPath2 = @"D:\DevTobii\tobii_stream_engine.dll";

            Environment.SetEnvironmentVariable(
                DeveloperExplicitPathDiscoverySource.RuntimePathVariable, testPath2);

            try
            {
                var source = new DeveloperExplicitPathDiscoverySource(new[] { testPath1 });
                var candidates = source.DiscoverCandidatePaths().ToList();

                Assert.That(candidates, Contains.Item(testPath1));
                Assert.That(candidates, Contains.Item(testPath2));
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    DeveloperExplicitPathDiscoverySource.RuntimePathVariable, null);
            }
        }

        [Test]
        public void InstalledProgramsDiscovery_ExecutesSafelyWithoutThrowing()
        {
            var source = new InstalledProgramsDiscoverySource();
            Assert.That(source.SourceName, Is.EqualTo("InstalledPrograms"));

            // Must execute gracefully even if registry access is unavailable
            List<string> candidates = null;
            Assert.DoesNotThrow(() =>
            {
                candidates = source.DiscoverCandidatePaths().ToList();
            });

            Assert.That(candidates, Is.Not.Null);
        }

        [Test]
        public void WindowsServiceDiscovery_ExecutesSafelyWithoutThrowing()
        {
            var source = new WindowsServiceDiscoverySource();
            Assert.That(source.SourceName, Is.EqualTo("WindowsServices"));

            List<string> candidates = null;
            Assert.DoesNotThrow(() =>
            {
                candidates = source.DiscoverCandidatePaths().ToList();
            });

            Assert.That(candidates, Is.Not.Null);
        }

        [Test]
        public void CompositeDiscovery_DeduplicatesAcrossMultipleSources()
        {
            var fakeSource1 = new FakeDiscoverySource("Source1", new[]
            {
                @"C:\Tobii\tobii_stream_engine.dll",
                @"C:\Another\tobii_stream_engine.dll"
            });

            var fakeSource2 = new FakeDiscoverySource("Source2", new[]
            {
                @"c:\tobii\tobii_stream_engine.dll", // duplicate with different casing
                @"D:\Third\tobii_stream_engine.dll"
            });

            var composite = new CompositeRuntimeDiscovery(new[] { fakeSource1, fakeSource2 });
            var candidates = composite.DiscoverAllCandidates().ToList();

            // 3 unique paths expected after case-insensitive deduplication
            Assert.That(candidates.Count, Is.EqualTo(3));
        }

        [Test]
        public void CompositeDiscovery_FaultTolerantToRogueSourceException()
        {
            var goodSource = new FakeDiscoverySource("GoodSource", new[] { @"C:\Good\tobii_stream_engine.dll" });
            var faultySource = new FaultyDiscoverySource();

            var composite = new CompositeRuntimeDiscovery(new IRuntimeDiscoverySource[] { faultySource, goodSource });

            List<string> candidates = null;
            Assert.DoesNotThrow(() =>
            {
                candidates = composite.DiscoverAllCandidates().ToList();
            });

            Assert.That(candidates, Is.Not.Null);
            Assert.That(candidates.Count, Is.EqualTo(1));
        }

        [Test]
        public void TobiiRuntimeLocator_UsesDiscoveryAndRejectsNonExistentCandidates()
        {
            var emptyDiscovery = new CompositeRuntimeDiscovery(new IRuntimeDiscoverySource[]
            {
                new FakeDiscoverySource("Empty", new[] { @"C:\NonExistent\tobii_stream_engine.dll" })
            });

            var locator = new TobiiRuntimeLocator(discovery: emptyDiscovery);
            var result = locator.LocateRuntime();

            Assert.That(result.IsFound, Is.False);
            Assert.That(result.FailureReason, Does.Contain("was not found"));
        }

        private class FakeDiscoverySource : IRuntimeDiscoverySource
        {
            private readonly IEnumerable<string> paths;
            public string SourceName { get; }

            public FakeDiscoverySource(string name, IEnumerable<string> paths)
            {
                SourceName = name;
                this.paths = paths;
            }

            public IEnumerable<string> DiscoverCandidatePaths() => paths;
        }

        private class FaultyDiscoverySource : IRuntimeDiscoverySource
        {
            public string SourceName => "FaultySource";
            public IEnumerable<string> DiscoverCandidatePaths()
            {
                throw new UnauthorizedAccessException("Simulated registry permission denial");
            }
        }
    }
}
