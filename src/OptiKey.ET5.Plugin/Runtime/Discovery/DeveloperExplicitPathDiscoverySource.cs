using System;
using System.Collections.Generic;

namespace OptiKey.ET5.Plugin.Runtime.Discovery
{
    /// <summary>
    /// Discovers candidate paths specified explicitly by developer configuration
    /// or environment variable (e.g. OPTIKEY_ET5_RUNTIME_PATH).
    /// </summary>
    public sealed class DeveloperExplicitPathDiscoverySource : IRuntimeDiscoverySource
    {
        public const string RuntimePathVariable = "OPTIKEY_ET5_RUNTIME_PATH";
        private readonly IEnumerable<string> explicitPaths;

        public string SourceName => "DeveloperExplicitPath";

        public DeveloperExplicitPathDiscoverySource(IEnumerable<string> explicitPaths = null)
        {
            this.explicitPaths = explicitPaths;
        }

        public IEnumerable<string> DiscoverCandidatePaths()
        {
            var candidates = new List<string>();

            if (explicitPaths != null)
            {
                candidates.AddRange(explicitPaths);
            }

            try
            {
                string envPath = Environment.GetEnvironmentVariable(RuntimePathVariable);
                if (!string.IsNullOrWhiteSpace(envPath))
                {
                    candidates.Add(envPath.Trim());
                }
            }
            catch
            {
                // Environment access failure; non-fatal
            }

            return candidates;
        }
    }
}
