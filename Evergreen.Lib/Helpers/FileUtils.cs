using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Evergreen.Lib.Helpers
{
    public static class FileUtils
    {
        public static async Task<string> ReadToString(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }


            try
            {
                if (IsBinary(path))
                {
                    return null;
                }

                await using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs, Encoding.UTF8);

                return await sr.ReadToEndAsync();
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public static async Task<string> GetFileContent(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                if (IsBinary(stream, out var reader))
                {
                    return null;
                }

                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                var result = await reader.ReadToEndAsync();

                reader.Dispose();

                return result;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        private static bool IsBinary(string path)
        {
            var length = new FileInfo(path).Length;

            if (length == 0)
            {
                return false;
            }

            using var stream = new StreamReader(path);

            int ch;
            while ((ch = stream.Read()) != -1)
            {
                if (IsControlChar(ch))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBinary(Stream content, out StreamReader reader)
        {
            reader = new StreamReader(content);

            int ch;
            while ((ch = reader.Read()) != -1)
            {
                if (IsControlChar(ch))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsControlChar(int ch) => ch is > Chars.NUL and < Chars.BS or > Chars.CR and < Chars.SUB;

        private static class Chars
        {
            // Null char
            public const char NUL = (char)0;

            // Back Space
            public const char BS = (char)8;

            // Carriage Return
            public const char CR = (char)13;

            // Substitute
            public const char SUB = (char)26;
        }
    }
}
