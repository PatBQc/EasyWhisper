﻿﻿﻿using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Whisper.net;
using Whisper.net.Ggml;

namespace EasyWhisper
{
    public partial class MainWindow : Window
    {
        private WaveInEvent? waveIn;
        private WaveFileWriter? writer;
        private string? tempFile;
        private DispatcherTimer? timer;
        private Stopwatch? stopwatch;
        private WhisperFactory? whisperFactory;
        private const string ModelFileNameTemplate = "ggml-{0}.bin";
        private string? ModelFileName;
        private const GgmlType WhisperModel = GgmlType.LargeV3Turbo;
        private TaskCompletionSource? recordingCompletionSource;
        private ParameterOptions _options = new ParameterOptions();

        public MainWindow()
        {
            InitializeComponent();
            StatusText.Text = "Ready to record...";
        }

        private async Task InitializeWhisper()
        {
            try
            {
                if (whisperFactory != null)
                {
                    return;
                }

                ModelFileName = string.Format(ModelFileNameTemplate, WhisperModel.ToString().ToLowerInvariant());

                if (!File.Exists(ModelFileName))
                {
                    StatusText.Text = "Downloading Whisper model...";
                    await DownloadModel();
                }

                whisperFactory = WhisperFactory.FromPath(ModelFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Whisper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error initializing Whisper";
            }
        }

        private async Task DownloadModel()
        {
            var modelPath = Path.Combine(AppContext.BaseDirectory, ModelFileName);

            if (!File.Exists(modelPath))
            {
                Debug.WriteLine("Downloading Whisper model...");
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(WhisperModel);
                using var fileWriter = File.OpenWrite(modelPath);
                await modelStream.CopyToAsync(fileWriter);
            }
        }

        private async void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                StartRecording();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting recording: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                await StopRecording();
            }
        }

        private void StartRecording()
        {
            waveIn?.StartRecording();
            StartRecordingButton.IsEnabled = false;
            StopRecordingButton.IsEnabled = true;
            TranscriptionTextBox.Clear();
            CopyToClipboardButton.IsEnabled = false;

            stopwatch = Stopwatch.StartNew();
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.75)
            };
            timer.Tick += Timer_Tick;
            timer.Start();

            StatusText.Text = "Recording...";
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (stopwatch != null)
            {
                TimerDisplay.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            }
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

            timer?.Stop();
            stopwatch?.Stop();

            recordingCompletionSource?.TrySetResult();
        }

        private async void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            await StopRecording();

            if (_options.ProcessLocally)
            {
                await ProcessAudioLocally();
            }
            else
            {
                await ProcessAudioOnline();
            }
        }

        private async Task StopRecording()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                // Wait for recording to fully stop
                if (recordingCompletionSource != null)
                {
                    await recordingCompletionSource.Task;
                    recordingCompletionSource = null;
                }
            }

            StopRecordingButton.IsEnabled = false;
            StatusText.Text = "Processing audio...";
            ProcessingProgressBar.Visibility = Visibility.Visible;
            ProcessingProgressBar.IsIndeterminate = true;
        }

        private async Task ProcessAudioLocally()
        {
            await InitializeWhisper();

            if (tempFile == null || whisperFactory == null) return;

            try
            {
                using var processor = whisperFactory.CreateBuilder()
                    .WithLanguage(_options.GetLanguageCode())
                    .Build();

                using var fileStream = File.OpenRead(tempFile);

                var startTime = DateTime.Now;

                var result = new System.Text.StringBuilder();
                await foreach (var segment in processor.ProcessAsync(fileStream))
                {
                    result.AppendLine(segment.Text);
                }

                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                TranscriptionTextBox.Text = result.ToString().Trim();
                StartRecordingButton.IsEnabled = true;
                CopyToClipboardButton.IsEnabled = true;
                StatusText.Text = "Transcription complete, took " + duration.ToString("mm\\:ss") + " (minutes:secondes) for a recording of " + TimerDisplay.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing audio: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error processing audio";
            }
            finally
            {
                ProcessingProgressBar.Visibility = Visibility.Collapsed;
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }

        private async Task ProcessAudioOnline()
        {
            try
            {
                var startTime = DateTime.Now;

                var transcript = await WhisperHelper.GenerateTranscription(tempFile, languageCode: _options.GetLanguageCode());

                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                TranscriptionTextBox.Text = transcript;
                StartRecordingButton.IsEnabled = true;
                CopyToClipboardButton.IsEnabled = true;
                StatusText.Text = "Transcription complete, took " + duration.ToString("mm\\:ss") + " (minutes:secondes) for a recording of " + TimerDisplay.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing audio: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error processing audio";
            }
            finally
            {
                ProcessingProgressBar.Visibility = Visibility.Collapsed;
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }

        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TranscriptionTextBox.Text))
            {
                Clipboard.SetText(TranscriptionTextBox.Text);
                StatusText.Text = "Text copied to clipboard";
            }
        }

        private void ParametersButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ParametersWindow(_options) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _options = dialog.Options;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            StopRecording().Wait();
            whisperFactory?.Dispose();
            base.OnClosed(e);
        }

        //public async Task<string> SpeechToTextOnline(byte[] audioBytes)
        //{
        //    string whisperUrl = "https://api.openai.com/v1/audio/transcriptions";
        //    string apiKey = Environment.GetEnvironmentVariable("OPEN_AI_API_KEY");
        //    using HttpClient http = new HttpClient();
        //    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        //    // add the audio bytes to the form data
        //    using var content = new MultipartFormDataContent
        //       {
        //           { new ByteArrayContent(audioBytes), "file", "audio.wav" },
        //                   {new StringContent("whisper-1"), "model" }
        //       };

        //    using HttpResponseMessage response = await http.PostAsync(whisperUrl, content); 
        //    string responseText = await response.Content.ReadAsStringAsync();
        //    dynamic responseJson = JsonConvert.DeserializeObject<dynamic>(responseText);
        //    return responseJson.text;
        //}
    }
}
