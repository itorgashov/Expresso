using System.Globalization;

namespace Expresso.Parsing
{
    /// <summary>
    /// Controls how quoted date/time literals are parsed in filter and sort query strings.
    /// When properties are null or empty, built-in defaults apply (ISO dates, invariant time-of-day formats).
    /// </summary>
    public sealed class LiteralParseOptions
    {
        /// <summary>Shared default options matching the library's built-in literal parsing rules.</summary>
        public static LiteralParseOptions Default { get; } = new LiteralParseOptions();

        /// <summary>
        /// Culture for exact format parsing. When null, exact parsing uses invariant culture
        /// and culture fallback uses <see cref="CultureInfo.CurrentCulture"/>.
        /// </summary>
        public string? CultureName { get; set; }

        /// <summary>Exact formats for <see cref="DateTime"/> literals. Default: <c>yyyy-MM-dd</c>.</summary>
        public string[]? DateTimeFormats { get; set; }

        /// <summary>Exact formats for DateOnly literals (net6.0). Default: <c>yyyy-MM-dd</c>.</summary>
        public string[]? DateFormats { get; set; }

        /// <summary>Exact formats for TimeOnly literals (net6.0). Default: <c>HH:mm:ss</c>, <c>HH:mm</c>.</summary>
        public string[]? TimeFormats { get; set; }

        /// <summary>Exact formats for <see cref="TimeSpan"/> time-of-day literals. Default: <c>hh:mm</c>, <c>hh:mm:ss</c>.</summary>
        public string[]? TimeSpanFormats { get; set; }

        /// <summary>When true, try culture-based <c>TryParse</c> after exact formats fail. Default: true.</summary>
        public bool AllowCultureFallback { get; set; } = true;

        internal static readonly string[] DefaultDateTimeFormats = { "yyyy-MM-dd" };
        internal static readonly string[] DefaultDateFormats = { "yyyy-MM-dd" };
        internal static readonly string[] DefaultTimeFormats = { "HH:mm:ss", "HH:mm" };
        internal static readonly string[] DefaultTimeSpanFormats =
        {
            @"hh\:mm\:ss\.FFFFFFF",
            @"hh\:mm\:ss",
            @"hh\:mm",
        };

        internal LiteralParseSettings ToSettings()
        {
            CultureInfo exactCulture;
            CultureInfo fallbackCulture;
            if (string.IsNullOrWhiteSpace(CultureName))
            {
                exactCulture = CultureInfo.InvariantCulture;
                fallbackCulture = CultureInfo.CurrentCulture;
            }
            else
            {
                exactCulture = ResolveCulture(CultureName);
                fallbackCulture = exactCulture;
            }

            return new LiteralParseSettings(
                exactCulture,
                fallbackCulture,
                AllowCultureFallback,
                CopyOrDefault(DateTimeFormats, DefaultDateTimeFormats),
                CopyOrDefault(DateFormats, DefaultDateFormats),
                CopyOrDefault(TimeFormats, DefaultTimeFormats),
                CopyOrDefault(TimeSpanFormats, DefaultTimeSpanFormats));
        }

        private static string[] CopyOrDefault(string[]? formats, string[] defaults) =>
            formats is { Length: > 0 } ? (string[])formats.Clone() : defaults;

        private static CultureInfo ResolveCulture(string cultureName)
        {
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (string.Equals(culture.Name, cultureName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(culture.IetfLanguageTag, cultureName, StringComparison.OrdinalIgnoreCase))
                {
                    return CultureInfo.GetCultureInfo(culture.Name);
                }
            }

            throw new ArgumentException($"Unknown culture name: '{cultureName}'.", nameof(cultureName));
        }
    }

    internal sealed class LiteralParseSettings
    {
        public LiteralParseSettings(
            CultureInfo exactCulture,
            CultureInfo fallbackCulture,
            bool allowCultureFallback,
            string[] dateTimeFormats,
            string[] dateFormats,
            string[] timeFormats,
            string[] timeSpanFormats)
        {
            ExactCulture = exactCulture;
            FallbackCulture = fallbackCulture;
            AllowCultureFallback = allowCultureFallback;
            DateTimeFormats = dateTimeFormats;
            DateFormats = dateFormats;
            TimeFormats = timeFormats;
            TimeSpanFormats = timeSpanFormats;
        }

        public CultureInfo ExactCulture { get; }
        public CultureInfo FallbackCulture { get; }
        public bool AllowCultureFallback { get; }
        public string[] DateTimeFormats { get; }
        public string[] DateFormats { get; }
        public string[] TimeFormats { get; }
        public string[] TimeSpanFormats { get; }
    }
}
