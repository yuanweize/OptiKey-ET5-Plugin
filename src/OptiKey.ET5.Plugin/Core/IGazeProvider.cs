using System;

namespace OptiKey.ET5.Plugin.Core
{
    /// <summary>
    /// Abstraction for a gaze data streaming source (real Tobii hardware or mock).
    /// </summary>
    public interface IGazeProvider : IDisposable
    {
        event EventHandler<GazePointEventArgs> GazePointAvailable;
        event EventHandler<Exception> ErrorOccurred;
        event EventHandler<bool> ConnectionStatusChanged;

        bool IsConnected { get; }

        void Start();
        void Stop();
    }
}
