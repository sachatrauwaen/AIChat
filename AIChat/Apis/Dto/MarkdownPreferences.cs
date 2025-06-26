using Newtonsoft.Json;


namespace Satrabel.PersonaBar.AIChat.Apis
{
    public class MarkdownPreferences
    {
        [JsonProperty("useHeaders")]
        public bool UseHeaders { get; set; } = true;

        [JsonProperty("useBulletPoints")]
        public bool UseBulletPoints { get; set; } = true;

        [JsonProperty("useCodeBlocks")]
        public bool UseCodeBlocks { get; set; } = true;

        [JsonProperty("useTables")]
        public bool UseTables { get; set; } = true;

        [JsonProperty("useEmphasis")]
        public bool UseEmphasis { get; set; } = true;
    }
}