using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Evergreen.Core.Helpers
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

        public static string? GetHomeFolder() => GetPlatform() switch
        {
            Platform.Linux => Environment.GetEnvironmentVariable("HOME"),
            Platform.Osx => Environment.GetEnvironmentVariable("HOME"),
            Platform.FreeBsd => Environment.GetEnvironmentVariable("HOME"),
            Platform.Windows => GetHomePathWindows(),
            _ => throw new PlatformNotSupportedException(),
        };

        public static Platform GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Platform.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Platform.Osx;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                return Platform.FreeBsd;
            }

            return Platform.Linux;
        }

        private static string GetHomePathWindows()
        {
            var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
            var homePath = Environment.GetEnvironmentVariable("HOMEPATH");

            return Path.Join(homeDrive, homePath);
        }

        public static string SubHomePath(this string path)
        {
            var home = GetHomeFolder();

            if (home is null)
            {
                throw new Exception("unable to get user home directory path");
            }

            if (path.StartsWith(home, false, CultureInfo.InvariantCulture))
            {
                return $"~{path[home.Length..]}";
            }

            return path;
        }
    }
}
