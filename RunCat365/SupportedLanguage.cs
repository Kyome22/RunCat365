// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Globalization;

namespace RunCat365
{
    enum SupportedLanguage
    {
        English,
        Japanese,
        Spanish,
    }

    internal static class SupportedLanguageExtension
    {
        internal static SupportedLanguage GetCurrentLanguage()
        {
            var culture = CultureInfo.CurrentUICulture;
            return culture.TwoLetterISOLanguageName switch
            {
                "ja" => SupportedLanguage.Japanese,
                "es" => SupportedLanguage.Spanish,
                _ => SupportedLanguage.English,
            };
        }

        internal static CultureInfo GetDefaultCultureInfo(this SupportedLanguage language)
        {
            return language switch
            {
                SupportedLanguage.Japanese => new CultureInfo("ja-JP"),
                SupportedLanguage.Spanish => new CultureInfo("es-ES"),
                _ => new CultureInfo("en-US"),
            };
        }

        internal static string GetFontName(this SupportedLanguage language)
        {
            return language switch
            {
                SupportedLanguage.Japanese => "Noto Sans JP",
                SupportedLanguage.Spanish => "Consolas",
                _ => "Consolas",
            };
        }

        internal static bool IsFullWidth(this SupportedLanguage language)
        {
            return language switch
            {
                SupportedLanguage.Japanese => true,
                SupportedLanguage.Spanish => false,
                _ => false,
            };
        }
    }
}
