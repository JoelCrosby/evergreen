using System;
using System.IO;

namespace Evergreen.Lib.Helpers
{
    public static class FileUtils
    {
        public static string ReadToString(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);

            return sr.ReadToEnd();
        }
    }
}
