namespace PatientApi.Services;

/// <summary>
/// Parses FHIR date search parameters per https://www.hl7.org/fhir/search.html#date
/// Supported prefixes: eq, ne, lt, gt, le, ge, sa, eb, ap
/// Supported granularities: YYYY, YYYY-MM, YYYY-MM-DD, YYYY-MM-DDThh:mm:ss
/// </summary>
public static class FhirDateSearchParser
{
    private static readonly string[] KnownPrefixes =
        { "eq", "ne", "lt", "gt", "le", "ge", "sa", "eb", "ap" };

    public record DateSearchParam(string Prefix, DateTime Start, DateTime End);

    /// <summary>
    /// Parse a raw FHIR date param string like "ge2026-01-01" or "2026-01-13T18:25:43"
    /// Returns null when the string is null/empty or unparseable.
    /// </summary>
    public static DateSearchParam? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        string prefix = "eq";
        string datePart = raw.Trim();

        // Detect and strip prefix
        foreach (var p in KnownPrefixes)
        {
            if (datePart.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            {
                prefix = p.ToLower();
                datePart = datePart[p.Length..];
                break;
            }
        }

        // Determine precision and derive [start, end) interval
        var (start, end) = ParseInterval(datePart);
        if (start == null)
        {
            return null;
        }

        return new DateSearchParam(prefix, start.Value, end!.Value);
    }

    private static (DateTime? start, DateTime? end) ParseInterval(string datePart)
    {
        // YYYY
        if (System.Text.RegularExpressions.Regex.IsMatch(datePart, @"^\d{4}$"))
        {
            if (!int.TryParse(datePart, out int year))
            {
                return (null, null);
            }

            var s = new DateTime(year, 1, 1);
            return (s, s.AddYears(1));
        }

        // YYYY-MM
        if (System.Text.RegularExpressions.Regex.IsMatch(datePart, @"^\d{4}-\d{2}$"))
        {
            if (!DateTime.TryParseExact(datePart, "yyyy-MM",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var s))
            {
                return (null, null);
            }

            return (s, s.AddMonths(1));
        }

        // YYYY-MM-DD
        if (System.Text.RegularExpressions.Regex.IsMatch(datePart, @"^\d{4}-\d{2}-\d{2}$"))
        {
            if (!DateTime.TryParseExact(datePart, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var s))
            {
                return (null, null);
            }

            return (s, s.AddDays(1));
        }

        // YYYY-MM-DDThh:mm:ss  (also with ms or Z suffix – we parse loosely)
        if (datePart.Length >= 16)
        {
            var formats = new[]
            {
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:ss.fff",
                "yyyy-MM-ddTHH:mm:ss.fffZ",
                "yyyy-MM-ddTHH:mm",
            };
            if (!DateTime.TryParseExact(datePart, formats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeLocal, out var s))
            {
                return (null, null);
            }

            return (s, s.AddSeconds(1));
        }

        return (null, null);
    }

    /// <summary>
    /// Apply the parsed search param as a LINQ predicate.
    /// </summary>
    public static IQueryable<Models.Patient> ApplyFilter(
        IQueryable<Models.Patient> query,
        DateSearchParam param)
    {
        var (prefix, start, end) = (param.Prefix, param.Start, param.End);

        return prefix switch
        {
            // eq: the date falls within the implied interval
            "eq" => query.Where(p => p.BirthDate >= start && p.BirthDate < end),

            // ne: the date does NOT fall within the implied interval
            "ne" => query.Where(p => p.BirthDate < start || p.BirthDate >= end),

            // lt: before the start of the interval
            "lt" => query.Where(p => p.BirthDate < start),

            // gt: after the end of the interval
            "gt" => query.Where(p => p.BirthDate >= end),

            // le: before or within the interval (i.e. before end)
            "le" => query.Where(p => p.BirthDate < end),

            // ge: within or after the interval (i.e. >= start)
            "ge" => query.Where(p => p.BirthDate >= start),

            // sa (starts-after): the interval starts after the reference end
            "sa" => query.Where(p => p.BirthDate >= end),

            // eb (ends-before): the interval ends before the reference start
            "eb" => query.Where(p => p.BirthDate < start),

            // ap (approximately): ±10% of the search interval width (FHIR guidance)
            "ap" => ApplyApproximate(query, start, end),

            _ => query.Where(p => p.BirthDate >= start && p.BirthDate < end)
        };
    }

    private static IQueryable<Models.Patient> ApplyApproximate(
        IQueryable<Models.Patient> query, DateTime start, DateTime end)
    {
        var width = (end - start).TotalSeconds;
        var margin = TimeSpan.FromSeconds(width * 0.1);
        var apStart = start - margin;
        var apEnd = end + margin;
        return query.Where(p => p.BirthDate >= apStart && p.BirthDate < apEnd);
    }
}
