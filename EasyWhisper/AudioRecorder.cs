using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EasyWhisper
{
    public class AudioRecorder : IDisposable
    {
        private WaveInEvent? waveIn;
        private WasapiLoopbackCapture? speakerCapture;
        private WaveFileWriter? micWriter;
        private WaveFileWriter? speakerWriter;
        private string? micFile;
        private string? speakerFile;
        private string? mixedFile;
        private Stopwatch? stopwatch;
        private TaskCompletionSource? recordingCompletionSource;
        private bool isDisposed;
        private readonly bool keepRecordingFiles;
        private readonly string recordingTimestamp;
        private readonly string recordingsDirectory;

        public event EventHandler<TimeSpan>? RecordingTimeUpdated;
        public event EventHandler<string>? StatusUpdated;
        public event EventHandler<Exception>? ErrorOccurred;

        public bool IsRecording { get; private set; }
        public string? TempFilePath => mixedFile;
        public bool IsMicrophoneMuted { get; private set; }
        public bool IsSpeakerMuted { get; private set; }

        public AudioRecorder(bool keepRecordingFiles, bool speakerMutedByDefault)
        {
            this.keepRecordingFiles = keepRecordingFiles;
            this.IsSpeakerMuted = speakerMutedByDefault;
            this.IsMicrophoneMuted = false;
            this.recordingTimestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            this.recordingsDirectory = Path.Combine(AppContext.BaseDirectory, "Recordings");
            
            // Create recordings directory if it doesn't exist
            if (!Directory.Exists(recordingsDirectory))
            {
                Directory.CreateDirectory(recordingsDirectory);
            }
        }

        public void SetMicrophoneMuted(bool muted)
        {
            IsMicrophoneMuted = muted;
        }

        public void SetSpeakerMuted(bool muted)
        {
            IsSpeakerMuted = muted;
        }

        public async Task StartRecording()
        {
            try
            {
                if (IsRecording)
                    return;

                // Create recording files with timestamp
                micFile = Path.Combine(recordingsDirectory, $"{recordingTimestamp}-microphone.wav");
                speakerFile = Path.Combine(recordingsDirectory, $"{recordingTimestamp}-system.wav");
                mixedFile = Path.Combine(recordingsDirectory, $"{recordingTimestamp}-mixed.wav");

                // Initialize microphone recording
                waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(44100, 16, 1),
                    BufferMilliseconds = 50
                };

                micWriter = new WaveFileWriter(micFile, waveIn.WaveFormat);
                waveIn.DataAvailable += WaveIn_DataAvailable;
                waveIn.RecordingStopped += WaveIn_RecordingStopped;

                // Initialize speaker recording (always)
                Debug.WriteLine("Initializing system audio capture");
                speakerCapture = new WasapiLoopbackCapture();
                speakerWriter = new WaveFileWriter(speakerFile, speakerCapture.WaveFormat);
                speakerCapture.DataAvailable += SpeakerCapture_DataAvailable;
                speakerCapture.RecordingStopped += SpeakerCapture_RecordingStopped;

                recordingCompletionSource = new TaskCompletionSource();

                // Start recording
                waveIn.StartRecording();
                speakerCapture.StartRecording();
                IsRecording = true;
                stopwatch = Stopwatch.StartNew();
                StatusUpdated?.Invoke(this, "Recording started");

                // Start time update timer
                StartTimeUpdates();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting recording: {ex}");
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
            speakerCapture?.StopRecording();

            // Wait for recording to fully stop
            if (recordingCompletionSource != null)
            {
                await recordingCompletionSource.Task;
                recordingCompletionSource = null;
            }

            // Mix the audio files (both are always captured)
            if (micFile != null && speakerFile != null && mixedFile != null)
            {
                try
                {
                    await Task.Run(() => MixAudioFiles(micFile, speakerFile, mixedFile));

                    // Clean up source files if not keeping them
                    if (!keepRecordingFiles)
                    {
                        try
                        {
                            File.Delete(micFile);
                            File.Delete(speakerFile);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error cleaning up source files: {ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error mixing audio: {ex}");
                    ErrorOccurred?.Invoke(this, ex);
                    throw;
                }
            }

            StatusUpdated?.Invoke(this, "Recording stopped");
        }

        private float AnalyzeVolume(ISampleProvider provider)
        {
            float maxVolume = 0;
            var buffer = new float[provider.WaveFormat.SampleRate * 2]; // 2 seconds of audio
            int samplesRead;

            while ((samplesRead = provider.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < samplesRead; i++)
                {
                    float abs = Math.Abs(buffer[i]);
                    if (abs > maxVolume) maxVolume = abs;
                }
            }

            return maxVolume;
        }

        private ISampleProvider NormalizeVolume(ISampleProvider provider, float targetVolume)
        {
            return new VolumeSampleProvider(provider) { Volume = targetVolume };
        }

        private void MixAudioFiles(string micFile, string speakerFile, string outputFile)
        {
            using var micReader = new AudioFileReader(micFile);
            using var speakerReader = new AudioFileReader(speakerFile);
            
            // Convert speaker audio to mono and match sample rate
            var speakerMono = speakerReader.ToMono();
            var speakerResampled = new WdlResamplingSampleProvider(speakerMono, 44100);

            // Analyze volumes
            Debug.WriteLine("Analyzing audio volumes...");
            float micVolume = AnalyzeVolume(micReader);
            micReader.Position = 0; // Reset position after analysis
            
            float speakerVolume = AnalyzeVolume(speakerResampled);
            speakerReader.Position = 0; // Reset position
            speakerMono = speakerReader.ToMono(); // Recreate after position reset
            speakerResampled = new WdlResamplingSampleProvider(speakerMono, 44100);

            Debug.WriteLine($"Original volumes - Mic: {micVolume:F2}, Speaker: {speakerVolume:F2}");

            // Determine which source is stronger and calculate boost factor for the weaker one
            float maxBoostFactor = 2.0f; // Maximum boost to avoid excessive amplification
            float micNormFactor = 1.0f;
            float speakerNormFactor = 1.0f;

            if (micVolume > speakerVolume && speakerVolume > 0)
            {
                // Mic is stronger, boost speaker
                speakerNormFactor = Math.Min(micVolume / speakerVolume, maxBoostFactor);
                Debug.WriteLine($"Boosting speaker audio to match mic level (factor: {speakerNormFactor:F2})");
            }
            else if (speakerVolume > micVolume && micVolume > 0)
            {
                // Speaker is stronger, boost mic
                micNormFactor = Math.Min(speakerVolume / micVolume, maxBoostFactor);
                Debug.WriteLine($"Boosting mic audio to match speaker level (factor: {micNormFactor:F2})");
            }

            // Apply volume adjustments
            var normalizedMic = NormalizeVolume(micReader, micNormFactor);
            var normalizedSpeaker = NormalizeVolume(speakerResampled, speakerNormFactor);

            Debug.WriteLine($"Applied volume factors - Mic: {micNormFactor:F2}, Speaker: {speakerNormFactor:F2}");

            // Create the mixer with normalized inputs
            var mixer = new MixingSampleProvider(new ISampleProvider[] { normalizedMic, normalizedSpeaker });
            
            // Write the mixed audio to the output file
            WaveFileWriter.CreateWaveFile16(outputFile, mixer);
            Debug.WriteLine("Audio mixing completed with volume normalization");
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (IsMicrophoneMuted)
                {
                    // Write silence
                    var silenceBuffer = new byte[e.BytesRecorded];
                    micWriter?.Write(silenceBuffer, 0, e.BytesRecorded);
                }
                else
                {
                    micWriter?.Write(e.Buffer, 0, e.BytesRecorded);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in WaveIn_DataAvailable: {ex}");
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        private void SpeakerCapture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (IsSpeakerMuted)
                {
                    // Write silence
                    var silenceBuffer = new byte[e.BytesRecorded];
                    speakerWriter?.Write(silenceBuffer, 0, e.BytesRecorded);
                }
                else
                {
                    speakerWriter?.Write(e.Buffer, 0, e.BytesRecorded);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SpeakerCapture_DataAvailable: {ex}");
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            CleanupRecording();
        }

        private void SpeakerCapture_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            CleanupRecording();
        }

        private void CleanupRecording()
        {
            micWriter?.Dispose();
            micWriter = null;

            speakerWriter?.Dispose();
            speakerWriter = null;

            waveIn?.Dispose();
            waveIn = null;

            speakerCapture?.Dispose();
            speakerCapture = null;

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
                micWriter?.Dispose();
                speakerWriter?.Dispose();
                waveIn?.Dispose();
                speakerCapture?.Dispose();
                stopwatch?.Stop();

                // Clean up files if not keeping them
                if (!keepRecordingFiles)
                {
                    try
                    {
                        if (micFile != null && File.Exists(micFile))
                            File.Delete(micFile);
                        if (speakerFile != null && File.Exists(speakerFile))
                            File.Delete(speakerFile);
                        if (mixedFile != null && File.Exists(mixedFile))
                            File.Delete(mixedFile);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }
    }
}
