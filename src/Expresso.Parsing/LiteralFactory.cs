using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Globalization;

namespace Expresso.Parsing;

internal sealed class LiteralFactory
{
    private readonly LiteralParseSettings _settings;
    internal LiteralFactory(LiteralParseOptions? options = null) =>
        _settings = (options ?? LiteralParseOptions.Default).ToSettings();

    internal static Type GetLiteralType(string s)
    {
        if (!string.IsNullOrEmpty(s) && s.Length > 0 && s[0] >= '0' && s[0] <= '9')
        {
            return s.IndexOf('.') >= 0 || s.IndexOf('e') >= 0 || s.IndexOf('E') >= 0
                ? typeof(double)
                : typeof(int);
        }
        else
        {
            return typeof(string);
        }
    }

    internal AbstractExpression CreateLiteral(string value, Type targetType)
    {
        if (targetType == typeof(byte))
        {
            if (byte.TryParse(value, out var byteValue))
            {
                return new Literal(byteValue);
            }
            throw new ArgumentException($"Cannot parse '{value}' as {targetType}.");
        }
        if (targetType == typeof(int))
        {
            if (int.TryParse(value, out var intValue))
            {
                return new Literal(intValue);
            }
            throw new ArgumentException($"Cannot parse '{value}' as {targetType}.");
        }
        else if (targetType == typeof(double))
        {
            if (double.TryParse(value, out var doubleValue))
            {
                return new Literal(doubleValue);
            }
            throw new ArgumentException($"Cannot parse '{value}' as {targetType}.");
        }
        else if (targetType == typeof(DateTime))
        {
            string strippedValue = StripQuotedToken(value, targetType);
            if (TryParseDateTime(strippedValue, out var dateValue))
            {
                return new Literal(dateValue);
            }
            throw new ArgumentException($"Cannot parse '{strippedValue}' as {targetType}.");
        }
        else if (targetType == typeof(Guid))
        {
            string strippedValue = StripQuotedToken(value, targetType);
            if (Guid.TryParse(strippedValue, out var guidValue))
            {
                return new Literal(guidValue);
            }
            throw new ArgumentException($"Cannot parse '{strippedValue}' as {targetType}.");
        }
        else if (targetType == typeof(TimeSpan))
        {
            string strippedValue = StripQuotedToken(value, targetType);
            if (TryParseTimeOfDay(strippedValue, out var timeOfDay))
            {
                return new Literal(timeOfDay);
            }
            throw new ArgumentException($"Cannot parse '{strippedValue}' as {targetType}.");
        }
#if NET6_0_OR_GREATER
        else if (targetType == typeof(DateOnly))
        {
            string strippedValue = StripQuotedToken(value, targetType);
            if (TryParseDateOnly(strippedValue, out var dateOnlyValue))
            {
                return new Literal(dateOnlyValue);
            }
            throw new ArgumentException($"Cannot parse '{strippedValue}' as {targetType}.");
        }
        else if (targetType == typeof(TimeOnly))
        {
            string strippedValue = StripQuotedToken(value, targetType);
            if (TryParseTimeOnly(strippedValue, out var timeOnlyValue))
            {
                return new Literal(timeOnlyValue);
            }
            throw new ArgumentException($"Cannot parse '{strippedValue}' as {targetType}.");
        }
#endif
        else if (targetType == typeof(string))
        {
            return new Literal(StripQuotedToken(value, targetType));
        }
        else
        {
            throw new ArgumentException($"Unsupported target type: {targetType}.");
        }
    }

    private bool TryParseDateTime(string strippedValue, out DateTime dateValue)
    {
        if (DateTime.TryParseExact(
                strippedValue,
                _settings.DateTimeFormats,
                _settings.ExactCulture,
                DateTimeStyles.None,
                out dateValue))
        {
            return true;
        }

        if (_settings.AllowCultureFallback
            && DateTime.TryParse(strippedValue, _settings.FallbackCulture, DateTimeStyles.None, out dateValue))
        {
            return true;
        }

        dateValue = default;
        return false;
    }

#if NET6_0_OR_GREATER
    private bool TryParseDateOnly(string strippedValue, out DateOnly dateOnlyValue)
    {
        if (DateOnly.TryParseExact(
                strippedValue,
                _settings.DateFormats,
                _settings.ExactCulture,
                DateTimeStyles.None,
                out dateOnlyValue))
        {
            return true;
        }

        if (_settings.AllowCultureFallback
            && DateOnly.TryParse(strippedValue, _settings.FallbackCulture, DateTimeStyles.None, out dateOnlyValue))
        {
            return true;
        }

        dateOnlyValue = default;
        return false;
    }

    private bool TryParseTimeOnly(string strippedValue, out TimeOnly timeOnlyValue)
    {
        if (TimeOnly.TryParseExact(
                strippedValue,
                _settings.TimeFormats,
                _settings.ExactCulture,
                DateTimeStyles.None,
                out timeOnlyValue))
        {
            return true;
        }

        if (_settings.AllowCultureFallback
            && TimeOnly.TryParse(strippedValue, _settings.FallbackCulture, DateTimeStyles.None, out timeOnlyValue))
        {
            return true;
        }

        timeOnlyValue = default;
        return false;
    }
#endif

    private static string StripQuotedToken(string value, Type targetType)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"')
        {
            throw new ArgumentException($"Cannot parse token '{value}' as {targetType}.");
        }

        return value.Substring(1, value.Length - 2);
    }

    private bool TryParseTimeOfDay(string strippedValue, out TimeSpan timeOfDay)
    {
        timeOfDay = default;
        if (!TimeSpan.TryParseExact(
                strippedValue,
                _settings.TimeSpanFormats,
                _settings.ExactCulture,
                TimeSpanStyles.None,
                out timeOfDay))
        {
            return false;
        }

        return timeOfDay >= TimeSpan.Zero && timeOfDay < TimeSpan.FromDays(1);
    }

}
