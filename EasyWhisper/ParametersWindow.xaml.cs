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
            ProcessLocallyCheckBox.IsChecked = _options.ProcessLocally;
            LanguageComboBox.SelectedIndex = (int)_options.Language;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _options.ProcessLocally = ProcessLocallyCheckBox.IsChecked ?? false;
            _options.Language = (WhisperLanguage)LanguageComboBox.SelectedIndex;
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
