using System;
using System.Collections.Generic;
using System.IO;
using OptiKey.ET5.Plugin.Diagnostics;

namespace OptiKey.ET5.Plugin.Runtime.Discovery
{
    /// <summary>
    /// Aggregates multiple IRuntimeDiscoverySource implementations, collecting and
    /// deduplicating candidate paths before they enter the validation pipeline.
    /// </summary>
    public sealed class CompositeRuntimeDiscovery
    {
        private readonly List<IRuntimeDiscoverySource> sources;
        private readonly IPluginLogger logger;

        public IReadOnlyList<IRuntimeDiscoverySource> Sources => sources.AsReadOnly();

        public CompositeRuntimeDiscovery(
            IEnumerable<IRuntimeDiscoverySource> sources = null,
            IPluginLogger logger = null)
        {
            this.logger = logger;
            this.sources = new List<IRuntimeDiscoverySource>();

            if (sources != null)
            {
                this.sources.AddRange(sources);
            }
        }

        public static CompositeRuntimeDiscovery CreateDefault(
            IEnumerable<string> customProbePaths = null,
            IPluginLogger logger = null)
        {
            var sources = new List<IRuntimeDiscoverySource>
            {
                new DeveloperExplicitPathDiscoverySource(customProbePaths),
                new KnownPathDiscoverySource(),
                new InstalledProgramsDiscoverySource(),
                new WindowsServiceDiscoverySource()
            };

            return new CompositeRuntimeDiscovery(sources, logger);
        }

        public IEnumerable<string> DiscoverAllCandidates()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<string>();

            foreach (var source in sources)
            {
                try
                {
                    logger?.Debug($"Querying discovery source: {source.SourceName}");
                    foreach (var rawPath in source.DiscoverCandidatePaths())
                    {
                        if (string.IsNullOrWhiteSpace(rawPath)) continue;

                        string normalized;
                        try
                        {
                            normalized = Path.GetFullPath(rawPath.Trim());
                        }
                        catch
                        {
                            normalized = rawPath.Trim();
                        }

                        if (seen.Add(normalized))
                        {
                            results.Add(normalized);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.Warn($"Discovery source '{source.SourceName}' encountered an exception: {ex.Message}");
                }
            }

            return results;
        }
    }
}
