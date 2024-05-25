using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ShareBook.Helper.Extensions
{
    public static class StringExtension
    {
        private const string CopySuffix = "_copy";

        public static string GenerateSlug(this string phrase)
        {
            var str = phrase.RemoveAccent().ToLower();
            var regexTimeout = TimeSpan.FromSeconds(5);

            try
            {
                // invalid chars
                str = Regex.Replace(str, @"[^a-z0-9\s-]", "", RegexOptions.None, regexTimeout);

                // convert multiple spaces into one space
                str = Regex.Replace(str, @"\s+", " ", RegexOptions.None, regexTimeout).Trim();

                // Corta e remove espaços
                str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();

                // cut and trim
                str = Regex.Replace(str, @"\s", "-", RegexOptions.None, regexTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                throw new Exception("A operação de correspondência de regex excedeu o tempo limite.");
            }

            return str;
        }

        public static string RemoveAccent(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string AddIncremental(this string text)
        {
            var number = text.Split(CopySuffix).Length == 2 ? Convert.ToInt32(text.Split("_copy")[1]) + 1 : 1;

            var onlyText = text.Split(CopySuffix).Length == 2 ? text.Split("_copy")[0] : text;

            return $"{onlyText}{CopySuffix}{number}";
        }
    }
}