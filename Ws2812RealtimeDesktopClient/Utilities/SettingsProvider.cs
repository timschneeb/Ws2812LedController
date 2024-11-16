using Newtonsoft.Json;
using Ws2812RealtimeDesktopClient.Json;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.Utilities
{
    public static class SettingsProvider
    {
        public static Settings Instance { private set; get; } = new();

        private static Task? _saveTask;
        private static readonly JsonSerializerSettings Options = new()
        {
            Formatting = Formatting.Indented,
            Converters = {new PropertyRowConverter()},
            NullValueHandling = NullValueHandling.Ignore
        };

        static SettingsProvider()
        {
            Console.WriteLine($"SettingsProvider: Using settings file at: {SettingsPath}");
            Directory.CreateDirectory(PlatformUtils.AppDataPath);
            if (!File.Exists(SettingsPath))
            {
                File.WriteAllText(SettingsPath, "");
            }

            Load();
        }

        public static void Load()
        {
            var json = string.Empty;
            try
            { 
                json = File.ReadAllText(SettingsPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Settings? settings = null;
            try
            {
                settings = JsonConvert.DeserializeObject<Settings>(json, Options);
            }
            catch (JsonException e)
            {
                Console.WriteLine(e);
            }
            
            if (settings == null)
            {
                Console.WriteLine("SettingsProvider.Load: Deserialized result is null");
                Instance = new Settings();
            }
            else
            {
                Instance = settings;
            }
        }

        public static void Save()
        {
            if (_saveTask is { Status: TaskStatus.Running })
            {
                Console.WriteLine("SettingsProvider.Save: Already saving");
                return;
            }
            
            _saveTask = Task.Run(() =>
            {
                using var file = File.CreateText(SettingsPath);
                var json = JsonConvert.SerializeObject(Instance, Options);
                if (json.Trim().Length < 1)
                {
                    Console.WriteLine("SettingsProvider.Save: Serialized JSON is empty; refusing to write to disk");
                    return;
                }
                file.Write(json);
            });
        }

        public static string SettingsPath => PlatformUtils.IsPlatformSupported() ? PlatformUtils.CombineDataPath("config.json") : string.Empty;
    }
}