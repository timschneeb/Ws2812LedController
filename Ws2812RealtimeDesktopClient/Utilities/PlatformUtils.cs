using System.Runtime.InteropServices;

namespace Ws2812RealtimeDesktopClient.Utilities
{
    public static class PlatformUtils
    {
        public enum Platforms
        {
            Windows,
            Linux,
            OSX,
            Other
        }
        
        public static bool IsPlatformSupported()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                   RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            //|| RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        
        public static Platforms Platform
        {
            get
            {
                if (IsWindows)
                {
                    return Platforms.Windows;
                }
                if (IsLinux)
                {
                    return Platforms.Linux;
                }
                if (IsOSX)
                {
                    return Platforms.OSX;
                }

                return Platforms.Other;
            }
        }

        public static string CombineDataPath(string postfix)
        {
            return Path.Combine(AppDataPath, postfix);
        }

        public static string AppDataPath =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/Ws2812RealtimeDesktopClient/";
    }
}