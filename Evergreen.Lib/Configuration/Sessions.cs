using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

using Evergreen.Lib.Helpers;
using Evergreen.Lib.Session;

namespace Evergreen.Lib.Configuration
{
    public static class Sessions
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            AllowTrailingCommas = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static void SaveSession(RepositorySession session)
        {
            var sessionCachePath = GetSessionCachePath();
            var sessionJson = JsonSerializer.Serialize(session, JsonSerializerOptions);

            try
            {
                EnsureDirectoryExists(sessionCachePath);

                File.WriteAllText(sessionCachePath, sessionJson);
            }
            catch
            {
                Debug.WriteLine("[Config] Save session failed. ");
            }
        }

        public static RepositorySession LoadSession()
        {
            var sessionCachePath = GetSessionCachePath();

            if (!File.Exists(sessionCachePath))
            {
                return new RepositorySession();
            }

            try
            {
                var contents = File.ReadAllText(sessionCachePath);
                var session = JsonSerializer.Deserialize<RepositorySession>(contents, JsonSerializerOptions);

                return session ?? throw new Exception("unable to read session state config");
            }
            catch
            {
                Debug.WriteLine("[Config] Save session failed. ");

                return new RepositorySession();
            }
        }

        private static string GetSessionCachePath()
        {
            var configFolder = PathUtils.GetConfigurationFolder();

            return Path.Join(configFolder, "session.cache");
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            var file = new FileInfo(filePath);
            file.Directory?.Create();
        }
    }
}
