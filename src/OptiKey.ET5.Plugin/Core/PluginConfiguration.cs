using System;
using System.IO;
using OptiKey.ET5.Plugin.Diagnostics;

namespace OptiKey.ET5.Plugin.Core
{
    /// <summary>
    /// Callback pump strategy for native Tobii stream event processing.
    /// </summary>
    public enum CallbackStrategy
    {
        /// <summary>
        /// Periodic polling using tobii_device_process_callbacks with interruptible wait handle.
        /// Guaranteed bounded, deterministic shutdown without native thread blocking.
        /// </summary>
        Polling = 0,

        /// <summary>
        /// Classic wait-and-process using blocking tobii_wait_for_callbacks.
        /// Bounded via thread join timeout with stuck-worker isolation.
        /// </summary>
        WaitAndProcess = 1
    }

    /// <summary>
    /// Plugin configuration controlling device selection, callback pump strategy,
    /// and runtime behavior. Loaded lazily on first provider Start(), not during
    /// construction, to preserve the parameterless constructor contract.
    ///
    /// Configuration is read from (in priority order):
    /// 1. Environment variable overrides (developer use only)
    /// 2. Config file at %APPDATA%\OptiKey-ET5-Plugin\et5-plugin.config (primary)
    ///    or %APPDATA%\OptiKey\OptiKey\ET5Plugin\et5-plugin.config (legacy fallback)
    ///
    /// Production default:
    /// - AutomaticDeviceSelection = true (automatically connects if exactly 1 device is detected)
    /// - Multi-device safety = refuses silent auto-binding if multiple devices are detected
    /// - CallbackStrategy = Polling (guaranteed bounded shutdown)
    /// </summary>
    public class PluginConfiguration
    {
        public const string PrimaryConfigDirectory = "OptiKey-ET5-Plugin";
        public const string ConfigFileName = "et5-plugin.config";

        /// <summary>
        /// When true (default), automatically selects the target device if exactly
        /// one Tobii device is enumerated. When multiple devices exist, explicit
        /// selection is strictly required to prevent accidental connection.
        /// </summary>
        public bool AutomaticDeviceSelection { get; set; } = true;

        /// <summary>
        /// Callback pump strategy. Default is Polling (ProcessOnlyPollingPump)
        /// for guaranteed bounded, interruptible shutdown.
        /// </summary>
        public CallbackStrategy CallbackStrategy { get; set; } = CallbackStrategy.Polling;

        /// <summary>
        /// Polling interval in milliseconds when using CallbackStrategy.Polling.
        /// Default is 5ms.
        /// </summary>
        public int PollIntervalMs { get; set; } = 5;

        /// <summary>
        /// The zero-based local index of the device to use from the current enumeration.
        /// If set, overrides automatic selection.
        /// </summary>
        public int? PreferredDeviceIndex { get; set; }

        /// <summary>
        /// Optional explicit device URL. Takes precedence over PreferredDeviceIndex
        /// if both are set. This value is never written to logs to protect privacy.
        /// </summary>
        public string PreferredDeviceUrl { get; set; }

        /// <summary>
        /// Alias for backwards compatibility with developer test scripts.
        /// Maps directly to AutomaticDeviceSelection.
        /// </summary>
        public bool AllowUnverifiedTobiiDevice
        {
            get => AutomaticDeviceSelection;
            set => AutomaticDeviceSelection = value;
        }

        public PluginConfiguration()
        {
            AutomaticDeviceSelection = true;
            CallbackStrategy = CallbackStrategy.Polling;
            PollIntervalMs = 5;
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
            logger.Debug($"PluginConfiguration loaded: AutoSelect={config.AutomaticDeviceSelection}, " +
                $"CallbackStrategy={config.CallbackStrategy}, PollInterval={config.PollIntervalMs}ms");

            if (config.PreferredDeviceIndex.HasValue)
            {
                logger.Info($"Configured PreferredDeviceIndex = {config.PreferredDeviceIndex.Value}");
            }
            else if (!string.IsNullOrEmpty(config.PreferredDeviceUrl))
            {
                logger.Info("Configured explicit PreferredDeviceUrl (redacted for privacy).");
            }

            return config;
        }

        private static void LoadFromEnvironment(PluginConfiguration config, IPluginLogger logger)
        {
            try
            {
                string autoSelect = Environment.GetEnvironmentVariable("ET5_AUTOMATIC_DEVICE_SELECTION");
                if (string.IsNullOrEmpty(autoSelect))
                {
                    // Fallback to legacy developer mode env var
                    autoSelect = Environment.GetEnvironmentVariable("ET5_ALLOW_UNVERIFIED_DEVICE");
                }

                if (!string.IsNullOrEmpty(autoSelect))
                {
                    config.AutomaticDeviceSelection =
                        autoSelect.Equals("1", StringComparison.Ordinal) ||
                        autoSelect.Equals("true", StringComparison.OrdinalIgnoreCase);
                    logger.Debug($"ET5_AUTOMATIC_DEVICE_SELECTION set from environment: {config.AutomaticDeviceSelection}");
                }

                string strategyStr = Environment.GetEnvironmentVariable("ET5_CALLBACK_STRATEGY");
                if (!string.IsNullOrEmpty(strategyStr))
                {
                    if (Enum.TryParse(strategyStr, true, out CallbackStrategy strategy))
                    {
                        config.CallbackStrategy = strategy;
                        logger.Debug($"ET5_CALLBACK_STRATEGY set from environment: {strategy}");
                    }
                }

                string pollMsStr = Environment.GetEnvironmentVariable("ET5_POLL_INTERVAL_MS");
                if (!string.IsNullOrEmpty(pollMsStr) && int.TryParse(pollMsStr, out int pollMs) && pollMs > 0)
                {
                    config.PollIntervalMs = pollMs;
                }

                string indexStr = Environment.GetEnvironmentVariable("ET5_PREFERRED_DEVICE_INDEX");
                if (string.IsNullOrEmpty(indexStr))
                {
                    indexStr = Environment.GetEnvironmentVariable("ET5_SELECTED_DEVICE_INDEX");
                }
                if (!string.IsNullOrEmpty(indexStr))
                {
                    if (int.TryParse(indexStr, out int idx) && idx >= 0)
                    {
                        config.PreferredDeviceIndex = idx;
                    }
                    else
                    {
                        logger.Warn($"Invalid ET5_PREFERRED_DEVICE_INDEX value: {indexStr}");
                    }
                }

                string urlStr = Environment.GetEnvironmentVariable("ET5_PREFERRED_DEVICE_URL");
                if (string.IsNullOrEmpty(urlStr))
                {
                    urlStr = Environment.GetEnvironmentVariable("ET5_SELECTED_DEVICE_URL");
                }
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

                // Primary path: %APPDATA%\OptiKey-ET5-Plugin\et5-plugin.config
                string configPath = Path.Combine(appData, PrimaryConfigDirectory, ConfigFileName);
                if (!File.Exists(configPath))
                {
                    // Fallback to legacy path: %APPDATA%\OptiKey\OptiKey\ET5Plugin\et5-plugin.config
                    string legacyPath = Path.Combine(appData, "OptiKey", "OptiKey", "ET5Plugin", ConfigFileName);
                    if (File.Exists(legacyPath))
                    {
                        configPath = legacyPath;
                    }
                    else
                    {
                        logger.Debug($"No config file found at primary or legacy paths.");
                        return;
                    }
                }

                logger.Info($"Reading config from: {configPath}");

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
                        case "automaticdeviceselection":
                        case "allowunverifiedtobiidevice":
                            config.AutomaticDeviceSelection =
                                value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                value.Equals("1", StringComparison.Ordinal);
                            break;

                        case "callbackstrategy":
                            if (Enum.TryParse(value, true, out CallbackStrategy strat))
                            {
                                config.CallbackStrategy = strat;
                            }
                            break;

                        case "pollintervalms":
                            if (int.TryParse(value, out int pollMs) && pollMs > 0)
                            {
                                config.PollIntervalMs = pollMs;
                            }
                            break;

                        case "preferreddeviceindex":
                        case "selecteddeviceindex":
                            if (!config.PreferredDeviceIndex.HasValue)
                            {
                                if (int.TryParse(value, out int idx) && idx >= 0)
                                {
                                    config.PreferredDeviceIndex = idx;
                                }
                            }
                            break;

                        case "preferreddeviceurl":
                        case "selecteddeviceurl":
                            if (string.IsNullOrEmpty(config.PreferredDeviceUrl))
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
