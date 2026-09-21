using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Runtime;

namespace OptiKey.ET5.Diagnostics
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================");
            Console.WriteLine("       OptiKey-ET5-Plugin Hardware Diagnostics Console          ");
            Console.WriteLine("================================================================");
            Console.ResetColor();
            Console.WriteLine();

            bool allChecksPassed = true;

            // 1. Check Process Bitness
            Console.Write("[CHECK 1/6] Validating Process Architecture: ");
            if (Environment.Is64BitProcess)
            {
                PrintSuccess("x64 (AMD64) confirmed.");
            }
            else
            {
                PrintFailure("Process is 32-bit (x86). Must run in 64-bit mode!");
                return 1;
            }

            // 2. Locate Tobii Stream Engine Runtime
            Console.Write("[CHECK 2/6] Locating Tobii Stream Engine Runtime: ");
            var locator = new TobiiRuntimeLocator();
            var locateResult = locator.LocateRuntime();

            if (locateResult.IsFound)
            {
                PrintSuccess("Found.");
                Console.WriteLine($"    Path: {SanitizePath(locateResult.LibraryPath)}");
                Console.WriteLine($"    PE Architecture: {(locateResult.IsArchitectureValid ? "Valid x64 AMD64" : "INVALID")}");
                Console.WriteLine($"    Publisher: {locateResult.Publisher ?? "Unsigned/Unknown"}");
            }
            else
            {
                PrintFailure(locateResult.FailureReason ?? "Runtime DLL not found.");
                Console.WriteLine("    Ensure Tobii Experience is installed from official installer or Microsoft Store.");
                return 2;
            }

            // 3. Initialize Runtime API Binding
            Console.Write("[CHECK 3/6] Initializing Tobii Native API: ");
            using (var runtime = new TobiiNativeRuntime(locator))
            {
                if (runtime.Initialize())
                {
                    PrintSuccess("API Context Created.");
                }
                else
                {
                    PrintFailure($"Failed to initialize API context. Details: {runtime.GetLastErrorDescription()}");
                    return 3;
                }

                // 4. Enumerate Connected Devices
                Console.Write("[CHECK 4/6] Enumerating Connected Tobii Trackers: ");
                List<string> deviceUrls;
                if (runtime.EnumerateDevices(out deviceUrls) && deviceUrls.Count > 0)
                {
                    PrintSuccess($"Found {deviceUrls.Count} device(s).");
                }
                else
                {
                    PrintFailure("No Tobii devices detected. Check USB connection and Tobii Service.");
                    return 4;
                }

                PrintFailure("Device identity is not verified; refusing to connect to an arbitrary enumerated device.");
                return 5;

                // 5. Gaze Subscription and Sample Throughput Check (PRIVACY: Aggregate count only)
                Console.WriteLine("[CHECK 6/6] Subscribing to Gaze Stream (5-second throughput test)...");
                Console.WriteLine("    [PRIVACY NOTICE] Raw coordinates are NOT displayed or recorded.");

                long sampleCount = 0;
                long validCount = 0;

                tobii_gaze_point_callback_t callback = (ref tobii_gaze_point_t gazePoint, IntPtr userData) =>
                {
                    Interlocked.Increment(ref sampleCount);
                    if (gazePoint.validity == tobii_validity_t.TOBII_VALIDITY_VALID)
                    {
                        Interlocked.Increment(ref validCount);
                    }
                };

                if (!runtime.SubscribeGaze(callback))
                {
                    PrintFailure($"Gaze subscription failed: {runtime.GetLastErrorDescription()}");
                    return 6;
                }

                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < 5000)
                {
                    runtime.WaitForCallbacks();
                    runtime.ProcessCallbacks();
                }
                stopwatch.Stop();

                runtime.UnsubscribeGaze();

                double durationSec = stopwatch.ElapsedMilliseconds / 1000.0;
                double fps = sampleCount / durationSec;

                Console.WriteLine();
                Console.WriteLine("----------------------------------------------------------------");
                Console.WriteLine($"  Test Duration       : {durationSec:F1} seconds");
                Console.WriteLine($"  Total Samples Recv  : {sampleCount}");
                Console.WriteLine($"  Valid Gaze Samples  : {validCount}");
                Console.WriteLine($"  Average Frequency   : {fps:F1} Hz");
                Console.WriteLine("----------------------------------------------------------------");

                if (sampleCount > 0)
                {
                    PrintSuccess("Hardware data pipeline is fully operational!");
                }
                else
                {
                    PrintWarning("No gaze samples received during 5 seconds. Ensure the tracker is facing you and calibrated in Tobii Experience.");
                    allChecksPassed = false;
                }
            }

            Console.WriteLine();
            if (allChecksPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("DIAGNOSTICS PASSED: Your system is ready for OptiKey-ET5-Plugin.");
                Console.ResetColor();
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("DIAGNOSTICS COMPLETED WITH WARNINGS: Review items above.");
                Console.ResetColor();
                return 1;
            }
        }

        private static string SanitizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "N/A";
            // Obfuscate username in user-scoped paths if present
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile) && path.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
            {
                return "%USERPROFILE%" + path.Substring(userProfile.Length);
            }
            return path;
        }

        private static string RedactSerial(string serial)
        {
            if (string.IsNullOrEmpty(serial)) return "REDACTED";
            if (serial.Length <= 4) return "****";
            return serial.Substring(0, 3) + "-****-" + serial.Substring(serial.Length - 2);
        }

        private static void PrintSuccess(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[PASS] {msg}");
            Console.ResetColor();
        }

        private static void PrintWarning(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {msg}");
            Console.ResetColor();
        }

        private static void PrintFailure(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {msg}");
            Console.ResetColor();
        }
    }
}
