using System.Globalization;
using Peritus.Api.Accessors;
using Peritus.Api.Headers;
using Peritus.Types.Localization;

namespace Peritus.Api.Middlewares;

public class CultureMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, CultureAccessor cultureAccessor)
    {
        cultureAccessor.Culture = Culture.Default;

        if (context.Request.Headers.TryGetValue(CustomHeaders.Culture, out var cultureHeader))
        {
            var languages = ParseLanguages(cultureHeader.ToString())
                .OrderByDescending(x => x.Quality);

            foreach (var language in languages)
            {
                if (!TryGetCulture(language.Code, out var culture) || culture is null)
                {
                    continue;
                }

                cultureAccessor.Culture = culture.Value;
                break;
            }
        }

        await next(context);
    }

    /// <summary>
    ///     Parses the Accept-Language header value into a collection of valid language quality values.
    /// </summary>
    private static IEnumerable<LanguageQuality> ParseLanguages(string headerValue)
    {
        foreach (var part in headerValue.Split(','))
        {
            if (LanguageQuality.TryParse(part, out var language) && language is not null)
            {
                yield return language;
            }
        }
    }

    /// <summary>
    ///     Tries to get a culture by its code. Supports both base codes (e.g., "en")
    ///     and specific variants (e.g., "en-GB", "en-US").
    /// </summary>
    private static bool TryGetCulture(string code, out Culture? culture)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            culture = null;
            return false;
        }

        // Try exact match first (handles both "en" and "en-GB" style codes)
        foreach (var supportedCulture in Culture.Supported)
        {
            if (!code.Equals(supportedCulture.Code, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            culture = supportedCulture;
            return true;
        }

        // If no exact match and code contains a variant (e.g., "en-GB"),
        // try matching just the base language code (e.g., "en")
        var dashIndex = code.IndexOf('-');
        if (dashIndex > 0)
        {
            var baseCode = code[..dashIndex];
            foreach (var supportedCulture in Culture.Supported)
            {
                if (!baseCode.Equals(supportedCulture.Code, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                culture = supportedCulture;
                return true;
            }
        }

        culture = null;
        return false;
    }

    private record LanguageQuality(string Code, double Quality)
    {
        private const double BestQuality = 1.0;

        /// <summary>
        ///     Tries to parse a language quality value from the Accept-Language header format.
        ///     Examples: "en-US", "en-GB;q=0.9", "uk;q=0.8"
        /// </summary>
        public static bool TryParse(string input, out LanguageQuality? language)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                language = null;
                return false;
            }

            var parts = input.Split(';');
            var code = parts[0].Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                language = null;
                return false;
            }

            if (parts.Length <= 1)
            {
                language = new LanguageQuality(code, BestQuality);
                return true;
            }

            var qualityPart = parts[1].Trim();
            if (qualityPart.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(qualityPart[2..], NumberStyles.Any, CultureInfo.InvariantCulture, out var quality))
            {
                language = new LanguageQuality(code, quality);
                return true;
            }

            language = new LanguageQuality(code, BestQuality);
            return true;
        }
    }
}
