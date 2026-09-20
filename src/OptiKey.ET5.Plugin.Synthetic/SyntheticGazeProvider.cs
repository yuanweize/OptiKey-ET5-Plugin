using System;
using System.Threading;
using OptiKey.ET5.Plugin.Core;

namespace OptiKey.ET5.Plugin.Synthetic
{
    public enum SyntheticTrajectoryPattern
    {
        StaticCenter,
        CircularOrbit,
        FourCorners,
        LinearSweep,
        IntermittentLossOfTrack
    }

    /// <summary>
    /// Test-only gaze provider generating deterministic synthetic trajectories (ADR-006).
    /// Housed exclusively in OptiKey.ET5.Plugin.Synthetic.dll and never bundled in production releases.
    /// </summary>
    public class SyntheticGazeProvider : IGazeProvider
    {
        private readonly int intervalMs;
        private readonly SyntheticTrajectoryPattern pattern;
        private Thread workerThread;
        private CancellationTokenSource cts;
        private readonly object syncLock = new object();
        private long sampleCounter = 0;

        public event EventHandler<GazePointEventArgs> GazePointAvailable;
        public event EventHandler<Exception> ErrorOccurred;
        public event EventHandler<bool> ConnectionStatusChanged;

        public bool IsConnected { get; private set; }

        public SyntheticGazeProvider(
            SyntheticTrajectoryPattern pattern = SyntheticTrajectoryPattern.CircularOrbit,
            int intervalMs = 16) // Default ~60 Hz
        {
            this.pattern = pattern;
            this.intervalMs = intervalMs;
        }

        public void Start()
        {
            lock (syncLock)
            {
                if (workerThread != null && workerThread.IsAlive) return;

                cts = new CancellationTokenSource();
                IsConnected = true;
                ConnectionStatusChanged?.Invoke(this, true);

                workerThread = new Thread(WorkerLoop)
                {
                    Name = "SyntheticGazeWorker",
                    IsBackground = true
                };
                workerThread.Start();
            }
        }

        public void Stop()
        {
            lock (syncLock)
            {
                if (cts != null)
                {
                    cts.Cancel();
                }

                if (workerThread != null && workerThread.IsAlive)
                {
                    workerThread.Join(500);
                    workerThread = null;
                }

                IsConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
            }
        }

        private void WorkerLoop()
        {
            double angle = 0.0;

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    sampleCounter++;
                    float x = 0.5f;
                    float y = 0.5f;
                    bool isValid = true;

                    switch (pattern)
                    {
                        case SyntheticTrajectoryPattern.StaticCenter:
                            x = 0.5f;
                            y = 0.5f;
                            break;

                        case SyntheticTrajectoryPattern.CircularOrbit:
                            angle += 0.05;
                            x = (float)(0.5 + 0.3 * Math.Cos(angle));
                            y = (float)(0.5 + 0.3 * Math.Sin(angle));
                            break;

                        case SyntheticTrajectoryPattern.FourCorners:
                            long phase = (sampleCounter / 30) % 4;
                            switch (phase)
                            {
                                case 0: x = 0.1f; y = 0.1f; break;
                                case 1: x = 0.9f; y = 0.1f; break;
                                case 2: x = 0.9f; y = 0.9f; break;
                                case 3: x = 0.1f; y = 0.9f; break;
                            }
                            break;

                        case SyntheticTrajectoryPattern.LinearSweep:
                            float sweep = (float)((sampleCounter % 100) / 100.0);
                            x = sweep;
                            y = sweep;
                            break;

                        case SyntheticTrajectoryPattern.IntermittentLossOfTrack:
                            isValid = (sampleCounter % 20) < 15; // 75% valid, 25% track loss
                            x = 0.5f;
                            y = 0.5f;
                            break;
                    }

                    long timestampUs = DateTime.UtcNow.Ticks / 10;
                    var args = new GazePointEventArgs(x, y, timestampUs, isValid);
                    GazePointAvailable?.Invoke(this, args);

                    if (cts.Token.WaitHandle.WaitOne(intervalMs))
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    break;
                }
            }
        }

        public void InjectError(Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }

        public void Dispose()
        {
            Stop();
            if (cts != null)
            {
                cts.Dispose();
                cts = null;
            }
        }
    }
}
