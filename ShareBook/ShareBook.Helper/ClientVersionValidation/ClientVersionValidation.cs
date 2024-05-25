using ShareBook.Helper.Exceptions;
using System;
using System.Text.RegularExpressions;

namespace ShareBook.Helper
{
    public static class ClientVersionValidation
    {
        public static bool IsValidVersion(string version, string minVersion)
        {
            try
            {
                (int majorMin, int minorMin, int patchMin) = VersionDeconstructor(minVersion);
                (int major, int minor, int patch) = VersionDeconstructor(version);

                if (major < majorMin) return false;
                if (major > majorMin) return true;

                if (minor < minorMin) return false;
                if (minor > minorMin) return true;

                if (patch < patchMin) return false;
                else return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Tuple<int, int, int> VersionDeconstructor(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                throw new FormatVersionInvalidException("Formato inválido");
            }

            string pattern = @"^v([0-9]{1,2})\.([0-9]{1,2})\.([0-9]{1,2})$";
            Regex regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(5));

            try
            {
                Match match = regex.Match(version);

                if (!match.Success)
                {
                    throw new FormatVersionInvalidException("Formato inválido");
                }

                var major = int.Parse(match.Groups[1].Value);
                var minor = int.Parse(match.Groups[2].Value);
                var patch = int.Parse(match.Groups[3].Value);

                return Tuple.Create(major, minor, patch);
            }
            catch (RegexMatchTimeoutException)
            {
                throw new FormatVersionInvalidException("A operação de correspondência de regex excedeu o tempo limite.");
            }
            catch (FormatException)
            {
                throw new FormatVersionInvalidException("Formato inválido");
            }
        }
    }
}