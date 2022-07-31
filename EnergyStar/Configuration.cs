using EnergyStar.Interop;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnergyStar
{
    internal class Settings
    {
        public string[] Exemptions { get; set; } = new string[] { };

        public static Settings Load()
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
            try
            {
                string json = File.ReadAllText("settings.json");
                // TODO: optimize logic

                return JsonSerializer.Deserialize(json,
                    new SettingsJsonContext(options).Settings)!;
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is SecurityException
                || ex is UnauthorizedAccessException
            )
            {
                // Show message box to the user since the console is hidden in InvisibleRelease mode.
                Win32Api.MessageBox(IntPtr.Zero,
                    "IO Error occurred when reading settings.json!",
                    "EnergyStar Error", Win32Api.MB_ICONERROR | Win32Api.MB_OK);
                Environment.Exit(1);
            }
            catch (JsonException)
            {
                Win32Api.MessageBox(IntPtr.Zero,
                   "Failed to parse settings.json! Please check your settings.",
                   "EnergyStar Error", Win32Api.MB_ICONERROR | Win32Api.MB_OK);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Win32Api.MessageBox(IntPtr.Zero,
                   $@"Unknown Error:
{ex.Message}
{ex.StackTrace}",
                   "EnergyStar Error", Win32Api.MB_ICONERROR | Win32Api.MB_OK);
                Environment.Exit(1);
            }
            throw new InvalidOperationException("Unreachable code");
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
