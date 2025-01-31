using Whisper.net.Ggml;
using System.Linq;
using System.Xml.Serialization;
using System.IO;

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
        private const string SettingsFileName = "EasyWhisper.settings.xml";

        public bool ProcessLocally { get; set; }
        public WhisperLanguage Language { get; set; }
        public GgmlType WhisperModel { get; set; }
        public bool IncludeTimestamps { get; set; }
        public bool CaptureSystemAudio { get; set; }
        public bool KeepRecordingFiles { get; set; }
        public bool SaveTranscript { get; set; }
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

        public static string GetSettingsFilePath()
        {
            return Path.Combine(AppContext.BaseDirectory, SettingsFileName);
        }

        public static ParameterOptions LoadSettings()
        {
            string path = GetSettingsFilePath();
            if (!File.Exists(path))
            {
                return new ParameterOptions();
            }

            try
            {
                var serializer = new XmlSerializer(typeof(ParameterOptions));
                using var reader = new StreamReader(path);
                return (ParameterOptions)serializer.Deserialize(reader);
            }
            catch
            {
                return new ParameterOptions();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ParameterOptions));
                using var writer = new StreamWriter(GetSettingsFilePath());
                serializer.Serialize(writer, this);
            }
            catch
            {
                // Ignore save errors
            }
        }

        public ParameterOptions()
        {
            ProcessLocally = false;
            Language = WhisperLanguage.Auto;
            WhisperModel = GgmlType.LargeV3Turbo; // Default model
            IncludeTimestamps = false;
            CaptureSystemAudio = false;
            KeepRecordingFiles = false;
            SaveTranscript = false;
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
                OpenAIBasePath = this.OpenAIBasePath,
                CaptureSystemAudio = this.CaptureSystemAudio,
                KeepRecordingFiles = this.KeepRecordingFiles,
                SaveTranscript = this.SaveTranscript
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
