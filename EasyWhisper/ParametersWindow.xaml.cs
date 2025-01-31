using System;
using System.Windows;
using System.Windows.Controls;

namespace EasyWhisper
{
    public partial class ParametersWindow : Window
    {
        private ParameterOptions _options;
        public ParameterOptions Options => _options;

        private void UpdateApiControlsState()
        {
            bool isOnline = !ProcessLocallyCheckBox.IsChecked ?? false;
            ApiKeyTextBox.IsEnabled = isOnline;
            BasePathTextBox.IsEnabled = isOnline;
        }

        public ParametersWindow(ParameterOptions currentOptions)
        {
            InitializeComponent();
            _options = currentOptions.Clone();

            // Populate model ComboBox
            foreach (var model in ParameterOptions.OrderedModels)
            {
                ModelComboBox.Items.Add(ParameterOptions.GetModelDisplayName(model));
            }
            ModelComboBox.SelectedIndex = Array.IndexOf(ParameterOptions.OrderedModels, _options.WhisperModel);

            // Set other controls
            ProcessLocallyCheckBox.IsChecked = _options.ProcessLocally;
            LanguageComboBox.SelectedIndex = (int)_options.Language;
            IncludeTimestampsCheckBox.IsChecked = _options.IncludeTimestamps;
            CaptureSystemAudioCheckBox.IsChecked = _options.CaptureSystemAudio;
            KeepRecordingFilesCheckBox.IsChecked = _options.KeepRecordingFiles;
            SaveTranscriptCheckBox.IsChecked = _options.SaveTranscript;
            ApiKeyTextBox.Text = _options.OpenAIApiKey ?? "";
            BasePathTextBox.Text = _options.OpenAIBasePath ?? "";

            // Enable/disable model selection based on ProcessLocally
            ModelComboBox.IsEnabled = _options.ProcessLocally;
            ProcessLocallyCheckBox.Checked += (s, e) => ModelComboBox.IsEnabled = true;
            ProcessLocallyCheckBox.Unchecked += (s, e) => ModelComboBox.IsEnabled = false;

            // Enable/disable API configuration based on ProcessLocally
            UpdateApiControlsState();
            ProcessLocallyCheckBox.Checked += (s, e) => UpdateApiControlsState();
            ProcessLocallyCheckBox.Unchecked += (s, e) => UpdateApiControlsState();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _options.ProcessLocally = ProcessLocallyCheckBox.IsChecked ?? false;
            _options.Language = (WhisperLanguage)LanguageComboBox.SelectedIndex;
            _options.WhisperModel = ParameterOptions.OrderedModels[ModelComboBox.SelectedIndex];
            _options.IncludeTimestamps = IncludeTimestampsCheckBox.IsChecked ?? false;
            _options.OpenAIApiKey = string.IsNullOrWhiteSpace(ApiKeyTextBox.Text) ? null : ApiKeyTextBox.Text.Trim();
            _options.OpenAIBasePath = string.IsNullOrWhiteSpace(BasePathTextBox.Text) ? null : BasePathTextBox.Text.Trim();
            _options.CaptureSystemAudio = CaptureSystemAudioCheckBox.IsChecked ?? false;
            _options.KeepRecordingFiles = KeepRecordingFilesCheckBox.IsChecked ?? false;
            _options.SaveTranscript = SaveTranscriptCheckBox.IsChecked ?? false;
            
            _options.SaveSettings();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
