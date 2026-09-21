using System;
using System.Diagnostics;

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
    /// Minimal host-compatible logger with no plugin-specific logging dependency.
    /// </summary>
    public class PluginLogger : IPluginLogger
    {
        private readonly TraceSource traceSource;

        public PluginLogger(Type declaringType)
        {
            traceSource = new TraceSource((declaringType ?? typeof(PluginLogger)).FullName);
        }

        public PluginLogger(string loggerName)
        {
            traceSource = new TraceSource(loggerName ?? "OptiKey.ET5.Plugin");
        }

        public void Debug(string message)
        {
            traceSource.TraceEvent(TraceEventType.Verbose, 0, message ?? string.Empty);
        }

        public void Info(string message)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, message ?? string.Empty);
        }

        public void Warn(string message)
        {
            traceSource.TraceEvent(TraceEventType.Warning, 0, message ?? string.Empty);
        }

        public void Error(string message, Exception ex = null)
        {
            traceSource.TraceEvent(TraceEventType.Error, 0, ex == null ? message : message + " " + ex.Message);
        }
    }
}
