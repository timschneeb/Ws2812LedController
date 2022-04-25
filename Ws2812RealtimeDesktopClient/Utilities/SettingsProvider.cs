using Config.Net;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.Utilities
{
    public static class SettingsProvider
    {
        public static ISettings Instance { get; }
        static SettingsProvider()
        {
            Console.WriteLine($"SettingsProvider: Using settings file at: {SettingsPath}");
            Instance = new ConfigurationBuilder<ISettings>()
                .UseJsonFile(SettingsPath)
                .UseTypeParser(new ClassArrayParser<SegmentEntry>())
                .UseTypeParser(new ClassArrayParser<PaletteEntry>())
                .UseTypeParser(new ClassArrayParser<EffectAssignment>())
                .UseTypeParser(new ColorParser())
                .Build();
        }

        public static string SettingsPath => PlatformUtils.IsPlatformSupported() ? PlatformUtils.CombineDataPath("config.json") : string.Empty;
    }
}