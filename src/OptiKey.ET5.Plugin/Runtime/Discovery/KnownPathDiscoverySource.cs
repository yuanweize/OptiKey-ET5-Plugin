using System;
using System.Collections.Generic;
using System.IO;

namespace OptiKey.ET5.Plugin.Runtime.Discovery
{
    /// <summary>
    /// Probes established, high-integrity filesystem locations where Tobii Experience
    /// components typically place tobii_stream_engine.dll.
    /// </summary>
    public sealed class KnownPathDiscoverySource : IRuntimeDiscoverySource
    {
        public string SourceName => "KnownPaths";

        public IEnumerable<string> DiscoverCandidatePaths()
        {
            var candidates = new List<string>();

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (!string.IsNullOrEmpty(programFiles))
            {
                candidates.Add(Path.Combine(programFiles, @"Tobii\Tobii Service\tobii_stream_engine.dll"));
                candidates.Add(Path.Combine(programFiles, @"Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll"));
                candidates.Add(Path.Combine(programFiles, @"Tobii\Tobii EyeX Config\tobii_stream_engine.dll"));
            }

            if (!string.IsNullOrEmpty(programFilesX86))
            {
                candidates.Add(Path.Combine(programFilesX86, @"Tobii\Tobii Eye Tracker 5\x64\tobii_stream_engine.dll"));
                candidates.Add(Path.Combine(programFilesX86, @"Tobii\Tobii Service\tobii_stream_engine.dll"));
            }

            if (!string.IsNullOrEmpty(localAppData))
            {
                candidates.Add(Path.Combine(localAppData, @"Programs\Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll"));
            }

            return candidates;
        }
    }
}
