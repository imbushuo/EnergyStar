using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EnergyStar
{
    internal class Settings
    {
        public string[] Exemptions { get; set; } = new string[] { };

        public static Settings Load()
        {
            string json = File.ReadAllText("settings.json");
            // TODO: optimize logic
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
            return JsonSerializer.Deserialize(json,
                new SettingsJsonContext(options).Settings)!;
        }
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        // We only need metadata mode because we only do deserialization.
        GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(Settings))]
    internal partial class SettingsJsonContext : JsonSerializerContext
    {

    }
}
