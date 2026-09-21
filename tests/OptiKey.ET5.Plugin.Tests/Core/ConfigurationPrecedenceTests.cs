using System;
using System.IO;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Core;

namespace OptiKey.ET5.Plugin.Tests.Core
{
    [TestFixture]
    public class ConfigurationPrecedenceTests
    {
        private string tempDir;
        private string tempConfigFile;

        private static readonly string[] MonitoredEnvVars = new[]
        {
            "ET5_AUTOMATIC_DEVICE_SELECTION",
            "ET5_ALLOW_UNVERIFIED_DEVICE",
            "ET5_CALLBACK_STRATEGY",
            "ET5_POLL_INTERVAL_MS",
            "ET5_PREFERRED_DEVICE_INDEX",
            "ET5_SELECTED_DEVICE_INDEX",
            "ET5_PREFERRED_DEVICE_URL",
            "ET5_SELECTED_DEVICE_URL"
        };

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "OptiKey_ConfigTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            tempConfigFile = Path.Combine(tempDir, "et5-plugin.config");

            ClearEnvironmentVariables();
        }

        [TearDown]
        public void TearDown()
        {
            ClearEnvironmentVariables();

            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore temp directory deletion errors
                }
            }
        }

        private static void ClearEnvironmentVariables()
        {
            foreach (var envVar in MonitoredEnvVars)
            {
                Environment.SetEnvironmentVariable(envVar, null);
            }
        }

        [Test]
        public void Precedence_Step1_DefaultsAreCorrectlyInitialized()
        {
            var config = new PluginConfiguration();

            Assert.That(config.AutomaticDeviceSelection, Is.True, "Default AutomaticDeviceSelection must be true");
            Assert.That(config.AllowUnverifiedTobiiDevice, Is.False, "Default AllowUnverifiedTobiiDevice must be false (Strict Mode)");
            Assert.That(config.CallbackStrategy, Is.EqualTo(CallbackStrategy.Polling), "Default CallbackStrategy must be Polling");
            Assert.That(config.PollIntervalMs, Is.EqualTo(5), "Default PollIntervalMs must be 5ms");
            Assert.That(config.PreferredDeviceIndex, Is.Null, "Default PreferredDeviceIndex must be null");
            Assert.That(config.PreferredDeviceUrl, Is.Null, "Default PreferredDeviceUrl must be null");
        }

        [Test]
        public void Precedence_Step2_ConfigFileOverridesDefaults_ForAllFields()
        {
            // Write config file specifying non-default values for all 6 fields
            string configContent = @"
# OptiKey ET5 Test Configuration File
AutomaticDeviceSelection = false
AllowUnverifiedTobiiDevice = true
CallbackStrategy = WaitAndProcess
PollIntervalMs = 12
PreferredDeviceIndex = 2
PreferredDeviceUrl = tobii-prx://custom-config-url
";
            File.WriteAllText(tempConfigFile, configContent);

            var config = new PluginConfiguration();
            PluginConfiguration.LoadFromConfigFile(config, null, tempConfigFile);

            Assert.That(config.AutomaticDeviceSelection, Is.False);
            Assert.That(config.AllowUnverifiedTobiiDevice, Is.True);
            Assert.That(config.CallbackStrategy, Is.EqualTo(CallbackStrategy.WaitAndProcess));
            Assert.That(config.PollIntervalMs, Is.EqualTo(12));
            Assert.That(config.PreferredDeviceIndex, Is.EqualTo(2));
            Assert.That(config.PreferredDeviceUrl, Is.EqualTo("tobii-prx://custom-config-url"));
        }

        [Test]
        public void Precedence_Step3_EnvironmentVariablesOverrideConfigFile_ForAllFields()
        {
            // 1. Config file has non-default values
            string configContent = @"
AutomaticDeviceSelection = false
AllowUnverifiedTobiiDevice = false
CallbackStrategy = WaitAndProcess
PollIntervalMs = 10
PreferredDeviceIndex = 1
PreferredDeviceUrl = tobii-prx://file-url
";
            File.WriteAllText(tempConfigFile, configContent);

            var config = new PluginConfiguration();
            PluginConfiguration.LoadFromConfigFile(config, null, tempConfigFile);

            // 2. Set environment variables that contradict the config file
            Environment.SetEnvironmentVariable("ET5_AUTOMATIC_DEVICE_SELECTION", "true");
            Environment.SetEnvironmentVariable("ET5_ALLOW_UNVERIFIED_DEVICE", "true");
            Environment.SetEnvironmentVariable("ET5_CALLBACK_STRATEGY", "Polling");
            Environment.SetEnvironmentVariable("ET5_POLL_INTERVAL_MS", "25");
            Environment.SetEnvironmentVariable("ET5_PREFERRED_DEVICE_INDEX", "4");
            Environment.SetEnvironmentVariable("ET5_PREFERRED_DEVICE_URL", "tobii-prx://env-url");

            // 3. Load from environment (highest precedence)
            PluginConfiguration.LoadFromEnvironment(config, null);

            // 4. Assert environment values won over config file values for all 6 fields
            Assert.That(config.AutomaticDeviceSelection, Is.True, "Env must override config file for AutomaticDeviceSelection");
            Assert.That(config.AllowUnverifiedTobiiDevice, Is.True, "Env must override config file for AllowUnverifiedTobiiDevice");
            Assert.That(config.CallbackStrategy, Is.EqualTo(CallbackStrategy.Polling), "Env must override config file for CallbackStrategy");
            Assert.That(config.PollIntervalMs, Is.EqualTo(25), "Env must override config file for PollIntervalMs");
            Assert.That(config.PreferredDeviceIndex, Is.EqualTo(4), "Env must override config file for PreferredDeviceIndex");
            Assert.That(config.PreferredDeviceUrl, Is.EqualTo("tobii-prx://env-url"), "Env must override config file for PreferredDeviceUrl");
        }

        [Test]
        public void Precedence_EnvironmentVariables_SupportAlternateAliasesAndNumericBooleans()
        {
            Environment.SetEnvironmentVariable("ET5_AUTOMATIC_DEVICE_SELECTION", "0"); // numeric false
            Environment.SetEnvironmentVariable("ET5_ALLOW_UNVERIFIED_DEVICE", "1");    // numeric true
            Environment.SetEnvironmentVariable("ET5_SELECTED_DEVICE_INDEX", "3");      // legacy alias
            Environment.SetEnvironmentVariable("ET5_SELECTED_DEVICE_URL", "tobii-prx://alias-url"); // legacy alias

            var config = new PluginConfiguration();
            PluginConfiguration.LoadFromEnvironment(config, null);

            Assert.That(config.AutomaticDeviceSelection, Is.False);
            Assert.That(config.AllowUnverifiedTobiiDevice, Is.True);
            Assert.That(config.PreferredDeviceIndex, Is.EqualTo(3));
            Assert.That(config.PreferredDeviceUrl, Is.EqualTo("tobii-prx://alias-url"));
        }

        [Test]
        public void Precedence_MalformedValues_DoNotCorruptExistingConfiguration()
        {
            var config = new PluginConfiguration
            {
                PollIntervalMs = 8,
                PreferredDeviceIndex = 2
            };

            Environment.SetEnvironmentVariable("ET5_POLL_INTERVAL_MS", "not-a-number");
            Environment.SetEnvironmentVariable("ET5_PREFERRED_DEVICE_INDEX", "-5");
            Environment.SetEnvironmentVariable("ET5_CALLBACK_STRATEGY", "InvalidStrategyEnum");

            PluginConfiguration.LoadFromEnvironment(config, null);

            Assert.That(config.PollIntervalMs, Is.EqualTo(8), "Malformed poll interval must be ignored");
            Assert.That(config.PreferredDeviceIndex, Is.EqualTo(2), "Negative device index must be ignored");
            Assert.That(config.CallbackStrategy, Is.EqualTo(CallbackStrategy.Polling), "Invalid enum value must be ignored");
        }
    }
}
