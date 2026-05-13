using System.Text.Json.Serialization;

namespace RunCat365
{
    internal class CustomRunnerProfile
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("frameFileNames")]
        public List<string> FrameFileNames { get; set; } = [];
    }
}
