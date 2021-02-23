using System;
using System.IO;

namespace Evergreen.Lib.Configuration
{
    public static class LocalStorage
    {
        public static string GetConfigurationFolder()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            const string configFolder = ".config";

            return Path.Join(home, configFolder, "evergreen");
        }
    }
}
