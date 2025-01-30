namespace EasyWhisper
{
    public enum WhisperLanguage
    {
        Auto,
        English,
        French
    }

    public class ParameterOptions
    {
        public bool ProcessLocally { get; set; }
        public WhisperLanguage Language { get; set; }

        public ParameterOptions()
        {
            ProcessLocally = false;
            Language = WhisperLanguage.Auto;
        }

        public ParameterOptions Clone()
        {
            return new ParameterOptions
            {
                ProcessLocally = this.ProcessLocally,
                Language = this.Language
            };
        }

        public string GetLanguageCode()
        {
            return Language switch
            {
                WhisperLanguage.English => "en",
                WhisperLanguage.French => "fr",
                _ => ""
            };
        }
    }
}
