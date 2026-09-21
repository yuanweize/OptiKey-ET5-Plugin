using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace OptiKey.ET5.Plugin.Runtime.Discovery
{
    /// <summary>
    /// Discovers candidate paths by inspecting standard Windows installed program entries
    /// (Uninstall registry keys) without guessing proprietary Tobii keys.
    ///
    /// Any candidate found is treated as an unverified lead and must pass PE x64,
    /// signer metadata, and export validation gates before loading.
    /// </summary>
    public sealed class InstalledProgramsDiscoverySource : IRuntimeDiscoverySource
    {
        public string SourceName => "InstalledPrograms";

        private static readonly string[] UninstallRoots = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        public IEnumerable<string> DiscoverCandidatePaths()
        {
            var candidates = new List<string>();

            foreach (var rootPath in UninstallRoots)
            {
                try
                {
                    using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    using (var key = hklm.OpenSubKey(rootPath))
                    {
                        if (key == null) continue;

                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            try
                            {
                                using (var subKey = key.OpenSubKey(subKeyName))
                                {
                                    if (subKey == null) continue;

                                    string publisher = subKey.GetValue("Publisher") as string;
                                    string displayName = subKey.GetValue("DisplayName") as string;

                                    bool isTobii = (publisher != null && publisher.IndexOf("Tobii", StringComparison.OrdinalIgnoreCase) >= 0)
                                        || (displayName != null && displayName.IndexOf("Tobii", StringComparison.OrdinalIgnoreCase) >= 0);

                                    if (!isTobii) continue;

                                    string installLocation = subKey.GetValue("InstallLocation") as string;
                                    if (string.IsNullOrWhiteSpace(installLocation)) continue;

                                    installLocation = installLocation.Trim().Trim('"', '\'');

                                    candidates.Add(Path.Combine(installLocation, "tobii_stream_engine.dll"));
                                    candidates.Add(Path.Combine(installLocation, @"x64\tobii_stream_engine.dll"));
                                    candidates.Add(Path.Combine(installLocation, @"Tobii Service\tobii_stream_engine.dll"));
                                }
                            }
                            catch
                            {
                                // Permission or access failure on individual subkey; continue enumeration
                            }
                        }
                    }
                }
                catch
                {
                    // Registry root access failure (e.g. non-Windows or restricted); non-fatal
                }
            }

            return candidates;
        }
    }
}
