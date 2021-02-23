using System;
using System.IO;
using System.Text.Json;

using Evergreen.Lib.Session;

namespace Evergreen.Lib.Configuration
{
    public static class RestoreSession
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            AllowTrailingCommas = true, WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static void SaveSession(RepositorySession session)
        {
            if (session is null)
            {
                return;
            }

            var sessionCachePath = GetSessionCachePath();
            var sessionJson = JsonSerializer.Serialize(session, JsonSerializerOptions);

            EnsureDirectoryExists(sessionCachePath);

            try
            {
                File.WriteAllText(sessionCachePath, sessionJson);
            }
            catch
            {
                Console.WriteLine("[Config] Save session failed. ");
            }
        }

        public static RepositorySession LoadSession()
        {
            var sessionCachePath = GetSessionCachePath();

            if (!File.Exists(sessionCachePath))
            {
                return null;
            }

            try
            {
                var contents = File.ReadAllText(sessionCachePath);

                return JsonSerializer.Deserialize<RepositorySession>(contents, JsonSerializerOptions);
            }
            catch
            {
                Console.WriteLine("[Config] Save session failed. ");

                return null;
            }
        }

        private static string GetSessionCachePath()
        {
            var configFolder = LocalStorage.GetConfigurationFolder();

            return Path.Join(configFolder, "session.cache");
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            var file = new FileInfo(filePath);
            file.Directory?.Create();
        }
    }
}
