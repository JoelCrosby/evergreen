using System;
using System.IO;
using System.Threading.Tasks;

namespace Evergreen.Lib.Helpers
{
    public static class FileUtils
    {
        public async static Task<string> ReadToString(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            await using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);

            return await sr.ReadToEndAsync();
        }
    }
}
