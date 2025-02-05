﻿﻿using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Whisper.net;
using Whisper.net.Ggml;

namespace EasyWhisper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private AudioRecorder? audioRecorder;
        private WhisperFactory? whisperFactory;
        private const string ModelFileNameTemplate = "ggml-{0}.bin";
        private string? ModelFileName;
        private ParameterOptions _options;
        private const string MicIcon = "🎤";
        private const string SpeakerIcon = "🔊";
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool isMicrophoneMuted;
        private bool isSpeakerMuted;

        public bool IsMicrophoneMuted
        {
            get => isMicrophoneMuted;
            set
            {
                if (isMicrophoneMuted != value)
                {
                    isMicrophoneMuted = value;
                    if (audioRecorder != null)
                    {
                        audioRecorder.SetMicrophoneMuted(value);
                    }
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSpeakerMuted
        {
            get => isSpeakerMuted;
            set
            {
                if (isSpeakerMuted != value)
                {
                    isSpeakerMuted = value;
                    if (audioRecorder != null)
                    {
                        audioRecorder.SetSpeakerMuted(value);
                    }
                    OnPropertyChanged();
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _options = ParameterOptions.LoadSettings();
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

                ModelFileName = string.Format(ModelFileNameTemplate, _options.WhisperModel.ToString().ToLowerInvariant());

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
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(_options.WhisperModel);
                using var fileWriter = File.OpenWrite(modelPath);
                await modelStream.CopyToAsync(fileWriter);
            }
        }

        private async void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize recorder with both streams, speaker muted based on options
                audioRecorder = new AudioRecorder(_options.KeepRecordingFiles, !_options.CaptureSystemAudio);
                audioRecorder.StatusUpdated += (s, status) => StatusText.Text = status;
                audioRecorder.RecordingTimeUpdated += (s, time) => TimerDisplay.Text = time.ToString(@"hh\:mm\:ss");
                audioRecorder.ErrorOccurred += (s, ex) => MessageBox.Show($"Error during recording: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                StartRecordingButton.IsEnabled = false;
                StopRecordingButton.IsEnabled = true;
                MicrophoneButton.IsEnabled = true;
                SpeakerButton.IsEnabled = true;
                TranscriptionTextBox.Clear();
                CopyToClipboardButton.IsEnabled = false;
                
                // Set initial states
                IsMicrophoneMuted = false;
                IsSpeakerMuted = !_options.CaptureSystemAudio;

                await audioRecorder.StartRecording();
                StatusText.Text = "Recording...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting recording: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (audioRecorder != null)
                {
                    await audioRecorder.StopRecording();
                    // Reset mute states before disposing
                    IsMicrophoneMuted = false;
                    IsSpeakerMuted = false;
                    audioRecorder.Dispose();
                    audioRecorder = null;
                }
            }
        }

        private async void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            if (audioRecorder != null)
            {
                await audioRecorder.StopRecording();
                StopRecordingButton.IsEnabled = false;
                StatusText.Text = "Processing audio...";
                ProcessingProgressBar.Visibility = Visibility.Visible;
                ProcessingProgressBar.IsIndeterminate = true;

                if (_options.ProcessLocally)
                {
                    await ProcessAudioLocally();
                }
                else
                {
                    await ProcessAudioOnline();
                }

                // Reset mute states before disposing
                IsMicrophoneMuted = false;
                IsSpeakerMuted = false;
                audioRecorder.Dispose();
                audioRecorder = null;
            }
        }

        private async Task ProcessAudioLocally()
        {
            await InitializeWhisper();

            if (audioRecorder?.TempFilePath == null || whisperFactory == null) return;

            try
            {
                using var processor = whisperFactory.CreateBuilder()
                    .WithLanguage(_options.GetLanguageCode())
                    .Build();

                using var fileStream = File.OpenRead(audioRecorder.TempFilePath);

                var startTime = DateTime.Now;

                var result = new StringBuilder();
                await foreach (var segment in processor.ProcessAsync(fileStream))
                {
                    result.AppendLine(segment.Text);
                }

                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                var transcriptText = result.ToString().Trim();

                TranscriptionTextBox.Text = transcriptText;
                StartRecordingButton.IsEnabled = true;
                MicrophoneButton.IsEnabled = false;
                SpeakerButton.IsEnabled = false;
                CopyToClipboardButton.IsEnabled = true;

                if (_options.SaveTranscript && audioRecorder?.TempFilePath != null)
                {
                    var transcriptPath = Path.Combine(
                        Path.GetDirectoryName(audioRecorder.TempFilePath)!,
                        Path.GetFileNameWithoutExtension(audioRecorder.TempFilePath) + "-Transcript.txt"
                    );
                    File.WriteAllText(transcriptPath, transcriptText);
                }

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
            }
        }

        private async Task ProcessAudioOnline()
        {
            try
            {
                var startTime = DateTime.Now;

                var transcript = await WhisperHelper.GenerateTranscription(
                    audioRecorder.TempFilePath, 
                    openAIApiKey: _options.OpenAIApiKey,
                    openAIBasePath: _options.OpenAIBasePath,
                    includeTimestamps: _options.IncludeTimestamps,
                    languageCode: _options.GetLanguageCode());

                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                TranscriptionTextBox.Text = transcript;
                StartRecordingButton.IsEnabled = true;
                MicrophoneButton.IsEnabled = false;
                SpeakerButton.IsEnabled = false;
                CopyToClipboardButton.IsEnabled = true;

                if (_options.SaveTranscript && audioRecorder?.TempFilePath != null)
                {
                    var transcriptPath = Path.Combine(
                        Path.GetDirectoryName(audioRecorder.TempFilePath)!,
                        Path.GetFileNameWithoutExtension(audioRecorder.TempFilePath) + "-Transcript.txt"
                    );
                    File.WriteAllText(transcriptPath, transcript);
                }

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

        private void MicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            IsMicrophoneMuted = !IsMicrophoneMuted;
        }

        private void SpeakerButton_Click(object sender, RoutedEventArgs e)
        {
            IsSpeakerMuted = !IsSpeakerMuted;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (audioRecorder != null)
            {
                audioRecorder.StopRecording().Wait();
                // Reset mute states before disposing
                IsMicrophoneMuted = false;
                IsSpeakerMuted = false;
                audioRecorder.Dispose();
                audioRecorder = null;
            }
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
