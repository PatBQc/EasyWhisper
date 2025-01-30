using System.Windows;

namespace EasyWhisper
{
    public partial class ParametersWindow : Window
    {
        private ParameterOptions _options;
        public ParameterOptions Options => _options;

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

            // Enable/disable model selection based on ProcessLocally
            ModelComboBox.IsEnabled = _options.ProcessLocally;
            ProcessLocallyCheckBox.Checked += (s, e) => ModelComboBox.IsEnabled = true;
            ProcessLocallyCheckBox.Unchecked += (s, e) => ModelComboBox.IsEnabled = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _options.ProcessLocally = ProcessLocallyCheckBox.IsChecked ?? false;
            _options.Language = (WhisperLanguage)LanguageComboBox.SelectedIndex;
            _options.WhisperModel = ParameterOptions.OrderedModels[ModelComboBox.SelectedIndex];
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
