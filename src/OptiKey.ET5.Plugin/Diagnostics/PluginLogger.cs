using System;
using System.Reflection;
using log4net;

namespace OptiKey.ET5.Plugin.Diagnostics
{
    public interface IPluginLogger
    {
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception ex = null);
    }

    /// <summary>
    /// Logging implementation wrapping log4net with privacy enforcement (PRIVACY.md).
    /// </summary>
    public class PluginLogger : IPluginLogger
    {
        private readonly ILog log;

        public PluginLogger(Type declaringType)
        {
            log = LogManager.GetLogger(declaringType ?? typeof(PluginLogger));
        }

        public PluginLogger(string loggerName)
        {
            log = LogManager.GetLogger(Assembly.GetExecutingAssembly(), loggerName ?? "OptiKey.ET5.Plugin");
        }

        public void Debug(string message)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(message);
            }
        }

        public void Info(string message)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(message);
            }
        }

        public void Warn(string message)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn(message);
            }
        }

        public void Error(string message, Exception ex = null)
        {
            if (log.IsErrorEnabled)
            {
                if (ex != null)
                {
                    log.Error(message, ex);
                }
                else
                {
                    log.Error(message);
                }
            }
        }
    }
}
