namespace CleanRepo.Extensions;

// Inspired/Borrowed from: https://stackoverflow.com/a/21649465/2410379
static class TimeSpanExtensions
{
    static readonly SortedList<long, string> s_cutOff = new()
    {
        [59] = "{3:S}",
        [60] = "{2:M}",
        [60 * 60 - 1] = "{2:M}, {3:S}",
        [60 * 60] = "{1:H}",
        [24 * 60 * 60 - 1] = "{1:H}, {2:M}",
        [24 * 60 * 60] = "{0:D}",
        [long.MaxValue] = "{0:D}, {1:H}"
    };

    internal static string ToHumanReadableString(this TimeSpan ts)
    {
        int find =
            s_cutOff.Keys
                  .ToList()
                  .BinarySearch((long)ts.TotalSeconds);

        int near = find < 0 ? Math.Abs(find) - 1 : find;

        return string.Format(
            TimeSpanFormatter.Instance,
            s_cutOff[s_cutOff.Keys[near]],
            ts.Days,
            ts.Hours,
            ts.Minutes,
            ts.Seconds);
    }

    class TimeSpanFormatter : ICustomFormatter, IFormatProvider
    {
        internal static IFormatProvider Instance { get; } = new TimeSpanFormatter();

        static readonly Dictionary<string, string> s_timeFormats = new()
        {
            ["S"] = "{0:P:Seconds:Second}",
            ["M"] = "{0:P:Minutes:Minute}",
            ["H"] = "{0:P:Hours:Hour}",
            ["D"] = "{0:P:Days:Day}"
        };

        private TimeSpanFormatter() { }

        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (format is null)
                return $"{arg}";

            return string.Format(PluralFormatter.Instance, s_timeFormats[format], arg);
        }

        public object? GetFormat(Type? formatType)
            => formatType == typeof(ICustomFormatter) ? this : null;
    }

    class PluralFormatter : ICustomFormatter, IFormatProvider
    {
        internal static PluralFormatter Instance { get; } = new PluralFormatter();

        private PluralFormatter() { }

        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (format is null)
                return $"{arg}";

            if (arg != null)
            {
                string[] parts = format.Split(':');
                if (parts[0] == "P")
                {
                    int partIndex = (arg.ToString() == "1") ? 2 : 1;
                    return $"{arg} {(parts.Length > partIndex ? parts[partIndex] : "")}";
                }
            }
            return string.Format(formatProvider, format, arg);
        }

        public object? GetFormat(Type? formatType)
            => formatType == typeof(ICustomFormatter) ? this : null;
    }
}
