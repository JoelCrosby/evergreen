using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Evergreen.Lib.Helpers
{
    public static class PathUtils
    {
        public static string GetConfigurationFolder()
        {
            var home = GetHomeFolder();
            const string configFolder = ".config";

            return Path.Join(home, configFolder, "evergreen");
        }

        public static string GetCacheFolder()
        {
            var home = GetHomeFolder();
            const string configFolder = ".cache";

            return Path.Join(home, configFolder, "evergreen");
        }

        public static string GetHomeFolder()
        {
            return GetPlatform() switch
            {
                Platform.Linux => Environment.GetEnvironmentVariable("HOME"),
                Platform.OSX => Environment.GetEnvironmentVariable("HOME"),
                Platform.FreeBSD => Environment.GetEnvironmentVariable("HOME"),
                Platform.Windows => GetHomePathWindows(),
                _ => throw new Exception("Unsupported Platform"),
            };
        }

        public static Platform GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Platform.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Platform.OSX;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                return Platform.FreeBSD;
            }

            return Platform.Linux;
        }

        private static string GetHomePathWindows()
        {
            var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
            var homePath = Environment.GetEnvironmentVariable("HOMEPATH");

            return Path.Join(homeDrive, homePath);
        }
    }
}
