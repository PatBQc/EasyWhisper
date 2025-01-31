using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EasyWhisper
{
    public class AudioRecorder : IDisposable
    {
        private WaveInEvent? waveIn;
        private WaveFileWriter? writer;
        private string? tempFile;
        private Stopwatch? stopwatch;
        private TaskCompletionSource? recordingCompletionSource;
        private bool isDisposed;

        public event EventHandler<TimeSpan>? RecordingTimeUpdated;
        public event EventHandler<string>? StatusUpdated;
        public event EventHandler<Exception>? ErrorOccurred;

        public bool IsRecording { get; private set; }
        public string? TempFilePath => tempFile;

        public async Task StartRecording()
        {
            try
            {
                if (IsRecording)
                    return;

                tempFile = Path.GetTempFileName();
                waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 16, 1),
                    BufferMilliseconds = 20
                };

                writer = new WaveFileWriter(tempFile, waveIn.WaveFormat);
                recordingCompletionSource = new TaskCompletionSource();

                waveIn.DataAvailable += WaveIn_DataAvailable;
                waveIn.RecordingStopped += WaveIn_RecordingStopped;

                waveIn.StartRecording();
                IsRecording = true;
                stopwatch = Stopwatch.StartNew();
                StatusUpdated?.Invoke(this, "Recording started");

                // Start time update timer
                StartTimeUpdates();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                await StopRecording();
                throw;
            }
        }

        public async Task StopRecording()
        {
            if (!IsRecording)
                return;

            IsRecording = false;
            waveIn?.StopRecording();

            // Wait for recording to fully stop
            if (recordingCompletionSource != null)
            {
                await recordingCompletionSource.Task;
                recordingCompletionSource = null;
            }

            StatusUpdated?.Invoke(this, "Recording stopped");
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            writer?.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Dispose();
                writer = null;
            }

            waveIn?.Dispose();
            waveIn = null;

            stopwatch?.Stop();
            recordingCompletionSource?.TrySetResult();
        }

        private async void StartTimeUpdates()
        {
            while (IsRecording && !isDisposed)
            {
                if (stopwatch != null)
                {
                    RecordingTimeUpdated?.Invoke(this, stopwatch.Elapsed);
                }
                await Task.Delay(750); // Update every 750ms to match original timer
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                writer?.Dispose();
                waveIn?.Dispose();
                stopwatch?.Stop();

                if (tempFile != null && File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }
    }
}
