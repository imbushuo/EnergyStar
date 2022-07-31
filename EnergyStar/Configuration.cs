using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EnergyStar
{
    internal class Configuration
    {
        public string[] Exemptions { get; set; } = new string[] { };

        public static Configuration Load()
        {
            string json = File.ReadAllText("settings.json");
            // TODO: optimize logic
            return JsonSerializer.Deserialize(json, ConfigurationJsonContext.Default.Configuration)!;
        }
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        // We only need metadata mode because we only do deserialization.
        GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(Configuration))]
    internal partial class ConfigurationJsonContext : JsonSerializerContext
    {

    }
}
