using System.Collections.Generic;

namespace OptiKey.ET5.Plugin.Runtime.Discovery
{
    /// <summary>
    /// Contract for discovering candidate filesystem paths for tobii_stream_engine.dll.
    /// All returned candidates are treated as unverified leads and must pass
    /// strict PE x64, expected filename, signer metadata, and export validation gates.
    /// </summary>
    public interface IRuntimeDiscoverySource
    {
        /// <summary>
        /// Source identifier for diagnostics and logging.
        /// </summary>
        string SourceName { get; }

        /// <summary>
        /// Discovers candidate file paths without performing deep verification or loading.
        /// </summary>
        IEnumerable<string> DiscoverCandidatePaths();
    }
}
