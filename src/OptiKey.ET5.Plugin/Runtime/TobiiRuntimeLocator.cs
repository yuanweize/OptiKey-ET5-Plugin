using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using OptiKey.ET5.Plugin.Diagnostics;

namespace OptiKey.ET5.Plugin.Runtime
{
    public class RuntimeLocatorResult
    {
        public bool IsFound { get; }
        public string LibraryPath { get; }
        public bool IsArchitectureValid { get; }
        public bool IsSignerMetadataAccepted { get; }
        public string Publisher { get; }
        public string FailureReason { get; }

        public RuntimeLocatorResult(
            bool isFound,
            string libraryPath,
            bool isArchValid,
            bool isSigVerified,
            string publisher,
            string failureReason = null)
        {
            IsFound = isFound;
            LibraryPath = libraryPath;
            IsArchitectureValid = isArchValid;
            IsSignerMetadataAccepted = isSigVerified;
            Publisher = publisher;
            FailureReason = failureReason;
        }

        public static RuntimeLocatorResult Failed(string reason)
        {
            return new RuntimeLocatorResult(false, null, false, false, null, reason);
        }
    }

    public interface ITobiiRuntimeLocator
    {
        RuntimeLocatorResult LocateRuntime();
    }

    /// <summary>
    /// Strict, secure runtime locator for tobii_stream_engine.dll (ADR-004).
    /// Prevents DLL preloading attacks and ensures 64-bit AMD64 binary compatibility.
    /// </summary>
    public class TobiiRuntimeLocator : ITobiiRuntimeLocator
    {
        private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
        private readonly IPluginLogger logger;
        private readonly IEnumerable<string> customProbePaths;

        public TobiiRuntimeLocator(IPluginLogger logger = null, IEnumerable<string> customProbePaths = null)
        {
            this.logger = logger ?? new PluginLogger(typeof(TobiiRuntimeLocator));
            this.customProbePaths = customProbePaths;
        }

        public RuntimeLocatorResult LocateRuntime()
        {
            var candidatePaths = GetCandidateProbePaths();

            foreach (var path in candidatePaths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                logger.Debug($"Probing candidate Tobii runtime path: {path}");

                // 1. Verify PE Architecture (Must be x64 AMD64)
                if (!VerifyPe64Architecture(path))
                {
                    logger.Warn($"Candidate rejected: Not a valid 64-bit (AMD64) PE binary: {path}");
                    continue;
                }

                // 2. Inspect signer metadata; this is not Authenticode trust validation.
                bool signerMetadataAccepted = InspectSignerMetadata(path, out string publisher);
                if (!signerMetadataAccepted)
                {
                    logger.Warn($"Candidate rejected: Tobii signer metadata was not accepted for: {path}");
                    continue;
                }

                logger.Info($"Located Tobii runtime candidate after PE and signer metadata checks: {path} (Publisher: {publisher ?? "Unknown"})");
                return new RuntimeLocatorResult(
                    isFound: true,
                    libraryPath: path,
                    isArchValid: true,
                    isSigVerified: signerMetadataAccepted,
                    publisher: publisher);
            }

            string failure = "Tobii Experience runtime (tobii_stream_engine.dll) was not found in standard system locations.";
            logger.Warn(failure);
            return RuntimeLocatorResult.Failed(failure);
        }

        public IEnumerable<string> GetCandidateProbePaths()
        {
            var paths = new List<string>();

            if (customProbePaths != null)
            {
                paths.AddRange(customProbePaths);
            }

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Whitelist of legitimate Tobii installation directories
            paths.Add(Path.Combine(programFiles, @"Tobii\Tobii Service\tobii_stream_engine.dll"));
            paths.Add(Path.Combine(programFiles, @"Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll"));
            paths.Add(Path.Combine(programFiles, @"Tobii\Tobii EyeX Config\tobii_stream_engine.dll"));
            paths.Add(Path.Combine(programFilesX86, @"Tobii\Tobii Eye Tracker 5\x64\tobii_stream_engine.dll"));
            paths.Add(Path.Combine(localAppData, @"Programs\Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll"));

            return paths;
        }

        public static bool VerifyPe64Architecture(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(fs))
                {
                    if (fs.Length < 64) return false;

                    // DOS Header: "MZ"
                    ushort dosMagic = reader.ReadUInt16();
                    if (dosMagic != 0x5A4D) return false;

                    // e_lfanew offset to PE Header
                    fs.Seek(0x3C, SeekOrigin.Begin);
                    int peOffset = reader.ReadInt32();
                    if (peOffset < 0 || peOffset > fs.Length - 4) return false;

                    fs.Seek(peOffset, SeekOrigin.Begin);

                    // PE Signature: "PE\0\0"
                    uint peSignature = reader.ReadUInt32();
                    if (peSignature != 0x00004550) return false;

                    // Machine Architecture
                    ushort machine = reader.ReadUInt16();
                    return machine == IMAGE_FILE_MACHINE_AMD64;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool InspectSignerMetadata(string filePath, out string publisher)
        {
            publisher = null;
            try
            {
                var cert = X509Certificate.CreateFromSignedFile(filePath);
                if (cert != null)
                {
                    publisher = cert.Subject;
                    // Confirm publisher contains "Tobii"
                    return publisher.IndexOf("Tobii", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch
            {
                // Unsigned or certificate read failure
            }

            return false;
        }
    }
}
