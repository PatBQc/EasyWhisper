using Whisper.net.Ggml;
using System.Linq;

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
        public GgmlType WhisperModel { get; set; }
        public bool IncludeTimestamps { get; set; }
        public string? OpenAIApiKey { get; set; }
        public string? OpenAIBasePath { get; set; }

        // Ordered from most to least capable
        public static readonly GgmlType[] OrderedModels = new[]
        {
                GgmlType.LargeV3,
                GgmlType.LargeV2,
                GgmlType.LargeV1,
                GgmlType.LargeV3Turbo,  // Turbo models are faster but slightly less accurate
                GgmlType.Medium,
                GgmlType.MediumEn,
                GgmlType.Small,
                GgmlType.SmallEn,
                GgmlType.Base,
                GgmlType.BaseEn,
                GgmlType.Tiny,
                GgmlType.TinyEn
            };

        public ParameterOptions()
        {
            ProcessLocally = false;
            Language = WhisperLanguage.Auto;
            WhisperModel = GgmlType.LargeV3Turbo; // Default model
            IncludeTimestamps = false;
            OpenAIApiKey = null; // Will use OPENAI_API_KEY environment variable if not set
            OpenAIBasePath = null;
        }

        public ParameterOptions Clone()
        {
            return new ParameterOptions
            {
                ProcessLocally = this.ProcessLocally,
                Language = this.Language,
                WhisperModel = this.WhisperModel,
                IncludeTimestamps = this.IncludeTimestamps,
                OpenAIApiKey = this.OpenAIApiKey,
                OpenAIBasePath = this.OpenAIBasePath
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

        public static string GetModelDisplayName(GgmlType model)
        {
            string name = model.ToString().Replace("En", " English");
            // Convert "LargeV3" to "Large V3", etc.
            if (name.EndsWith("Turbo"))
            {
                name = name.Substring(0, name.Length - 5); // Remove "Turbo"
                int vIndex = name.IndexOf('V');
                return $"{name.Substring(0, vIndex)} V{name.Substring(vIndex + 1)} Turbo";
            }
            else
            {
                int vIndex = name.IndexOf('V');
                
                if(vIndex == -1)
                {
                    return name;
                }

                return $"{name.Substring(0, vIndex)} V{name.Substring(vIndex + 1)}";
            }
        }
    }
}
