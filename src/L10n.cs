using System;
using System.Globalization;

namespace MouseTrail
{
    /// <summary>极简双语字符串表：zh 为默认，en 为英文。</summary>
    internal static class L10n
    {
        private static string _language = "zh";

        public static string Language
        {
            get { return _language; }
        }

        public static bool IsChinese
        {
            get { return _language != "en"; }
        }

        public static void Set(string language)
        {
            _language = language == "en" ? "en" : "zh";
        }

        public static string T(string zh, string en)
        {
            return _language == "en" ? en : zh;
        }

        public static string SystemDefault()
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh" : "en";
        }
    }
}
