using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace OptiKey.ET5.Plugin.Runtime.Discovery
{
    /// <summary>
    /// Discovers candidate paths by inspecting Windows NT service registrations
    /// (e.g. Tobii Service ImagePath).
    ///
    /// WARNING (ADR / User Guidance):
    /// Deriving a DLL path from a service executable directory is ONLY candidate evidence.
    /// It MUST NOT be immediately trusted or loaded without passing PE x64,
    /// expected filename, signer metadata, and capability discovery gates.
    /// </summary>
    public sealed class WindowsServiceDiscoverySource : IRuntimeDiscoverySource
    {
        public string SourceName => "WindowsServices";

        private const string ServicesRoot = @"SYSTEM\CurrentControlSet\Services";

        public IEnumerable<string> DiscoverCandidatePaths()
        {
            var candidates = new List<string>();

            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(ServicesRoot))
                {
                    if (key == null) return candidates;

                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        if (subKeyName.IndexOf("Tobii", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        try
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey == null) continue;

                                string imagePath = subKey.GetValue("ImagePath") as string;
                                if (string.IsNullOrWhiteSpace(imagePath)) continue;

                                string exePath = CleanImagePath(imagePath);
                                if (string.IsNullOrEmpty(exePath)) continue;

                                string dir = Path.GetDirectoryName(exePath);
                                if (!string.IsNullOrEmpty(dir))
                                {
                                    candidates.Add(Path.Combine(dir, "tobii_stream_engine.dll"));
                                    candidates.Add(Path.Combine(dir, @"x64\tobii_stream_engine.dll"));
                                }
                            }
                        }
                        catch
                        {
                            // Permission or parsing failure on individual service key; continue
                        }
                    }
                }
            }
            catch
            {
                // Registry root access failure; non-fatal
            }

            return candidates;
        }

        private static string CleanImagePath(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string s = raw.Trim();
            if (s.StartsWith("\""))
            {
                int endQuote = s.IndexOf('\"', 1);
                if (endQuote > 1)
                {
                    return s.Substring(1, endQuote - 1);
                }
            }

            int spaceIdx = s.IndexOf(' ');
            if (spaceIdx > 0 && !s.StartsWith("\""))
            {
                // Might have arguments appended (e.g. C:\Path\srv.exe /arg)
                string candidate = s.Substring(0, spaceIdx);
                if (candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return s;
        }
    }
}
