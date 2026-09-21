using System;
using System.IO;
using OptiKey.ET5.Plugin.Diagnostics;

namespace OptiKey.ET5.Plugin.Core
{
    /// <summary>
    /// Plugin configuration controlling developer test mode, device selection,
    /// and runtime behavior. Loaded lazily on first provider Start(), not during
    /// construction, to preserve the parameterless constructor contract.
    ///
    /// Configuration is read from (in priority order):
    /// 1. Environment variable overrides (developer use only)
    /// 2. Config file at %APPDATA%\OptiKey\OptiKey\ET5Plugin\et5-plugin.config
    ///
    /// Production default: AllowUnverifiedTobiiDevice = false (strict mode).
    /// </summary>
    public class PluginConfiguration
    {
        /// <summary>
        /// When false (default/STRICT MODE), the provider refuses to bind to any
        /// device whose identity cannot be verified as ET5.
        /// When true (DEVELOPER TEST MODE), the provider permits controlled testing
        /// with an explicitly selected unverified device.
        /// </summary>
        public bool AllowUnverifiedTobiiDevice { get; set; }

        /// <summary>
        /// The zero-based local index of the device to use from the current enumeration.
        /// Only meaningful when AllowUnverifiedTobiiDevice is true.
        /// If null (default), no device is automatically selected even in developer mode.
        /// </summary>
        public int? PreferredDeviceIndex { get; set; }

        /// <summary>
        /// Optional explicit device URL. Takes precedence over PreferredDeviceIndex
        /// if both are set. Only meaningful in developer test mode.
        /// This value is never written to logs to protect privacy.
        /// </summary>
        public string PreferredDeviceUrl { get; set; }

        public PluginConfiguration()
        {
            AllowUnverifiedTobiiDevice = false;
            PreferredDeviceIndex = null;
            PreferredDeviceUrl = null;
        }

        /// <summary>
        /// Load configuration from environment variables and config file.
        /// This is intentionally lightweight and does not throw on missing/malformed config.
        /// </summary>
        public static PluginConfiguration Load(IPluginLogger logger = null)
        {
            var config = new PluginConfiguration();
            logger = logger ?? new PluginLogger(typeof(PluginConfiguration));

            // Priority 1: Environment variables (developer override)
            LoadFromEnvironment(config, logger);

            // Priority 2: Config file (if env vars did not set values)
            LoadFromConfigFile(config, logger);

            // Log active configuration state (without sensitive values)
            if (config.AllowUnverifiedTobiiDevice)
            {
                logger.Warn("DEVELOPER TEST MODE is ENABLED. " +
                    "This is NOT for production use. Device identity is NOT verified.");

                if (config.PreferredDeviceIndex.HasValue)
                {
                    logger.Info($"Developer mode: PreferredDeviceIndex = {config.PreferredDeviceIndex.Value}");
                }
                else if (!string.IsNullOrEmpty(config.PreferredDeviceUrl))
                {
                    // Log that a URL is configured but NOT the URL itself
                    logger.Info("Developer mode: explicit device URL is configured.");
                }
                else
                {
                    logger.Info("Developer mode: no device index or URL configured. " +
                        "Device selection will fail until one is specified.");
                }
            }
            else
            {
                logger.Debug("Strict mode active (default). Unverified devices will be refused.");
            }

            return config;
        }

        private static void LoadFromEnvironment(PluginConfiguration config, IPluginLogger logger)
        {
            try
            {
                string devMode = Environment.GetEnvironmentVariable("ET5_ALLOW_UNVERIFIED_DEVICE");
                if (!string.IsNullOrEmpty(devMode))
                {
                    if (devMode.Equals("1", StringComparison.Ordinal) ||
                        devMode.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        config.AllowUnverifiedTobiiDevice = true;
                        logger.Warn("ET5_ALLOW_UNVERIFIED_DEVICE environment variable is set.");
                    }
                }

                string indexStr = Environment.GetEnvironmentVariable("ET5_PREFERRED_DEVICE_INDEX");
                if (!string.IsNullOrEmpty(indexStr))
                {
                    int idx;
                    if (int.TryParse(indexStr, out idx) && idx >= 0)
                    {
                        config.PreferredDeviceIndex = idx;
                    }
                    else
                    {
                        logger.Warn($"Invalid ET5_PREFERRED_DEVICE_INDEX value: {indexStr}");
                    }
                }

                string urlStr = Environment.GetEnvironmentVariable("ET5_PREFERRED_DEVICE_URL");
                if (!string.IsNullOrEmpty(urlStr))
                {
                    config.PreferredDeviceUrl = urlStr;
                }
            }
            catch (Exception ex)
            {
                logger.Debug($"Error reading environment variables: {ex.Message}");
            }
        }

        private static void LoadFromConfigFile(PluginConfiguration config, IPluginLogger logger)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrEmpty(appData)) return;

                string configPath = Path.Combine(appData, "OptiKey", "OptiKey", "ET5Plugin", "et5-plugin.config");
                if (!File.Exists(configPath))
                {
                    logger.Debug($"No config file at: {configPath}");
                    return;
                }

                logger.Info($"Reading config from: {configPath}");

                // Simple key=value format, one per line. No complex parsing needed.
                foreach (string rawLine in File.ReadAllLines(configPath))
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                        continue;

                    int eqIdx = line.IndexOf('=');
                    if (eqIdx <= 0) continue;

                    string key = line.Substring(0, eqIdx).Trim();
                    string value = line.Substring(eqIdx + 1).Trim();

                    switch (key.ToLowerInvariant())
                    {
                        case "allowunverifiedtobiidevice":
                            if (!config.AllowUnverifiedTobiiDevice) // env var takes precedence
                            {
                                config.AllowUnverifiedTobiiDevice =
                                    value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                    value.Equals("1", StringComparison.Ordinal);
                            }
                            break;

                        case "preferreddeviceindex":
                            if (!config.PreferredDeviceIndex.HasValue) // env var takes precedence
                            {
                                int idx;
                                if (int.TryParse(value, out idx) && idx >= 0)
                                {
                                    config.PreferredDeviceIndex = idx;
                                }
                            }
                            break;

                        case "preferreddeviceurl":
                            if (string.IsNullOrEmpty(config.PreferredDeviceUrl)) // env var takes precedence
                            {
                                config.PreferredDeviceUrl = value;
                            }
                            break;

                        default:
                            logger.Debug($"Unknown config key: {key}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Debug($"Error reading config file: {ex.Message}");
            }
        }
    }
}
