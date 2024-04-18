using System.Collections.Immutable;

namespace PackageIndexer;

public sealed class PackageFilterExpression
{
    private PackageFilterExpression(string text, bool isPrefix, bool isSuffix)
    {
        Text = text;
        IsPrefix = isPrefix;
        IsSuffix = isSuffix;
    }

    public string Text { get; }
    public bool IsExact => !IsPrefix && !IsSuffix;
    public bool IsPrefix { get; }
    public bool IsSuffix { get; }

    public static PackageFilterExpression Parse(string text)
    {
        int firstAsterisk = text.IndexOf('*');
        int lastAsterisk = text.LastIndexOf('*');
        if (firstAsterisk != lastAsterisk)
            throw new FormatException();

        int asterisk = firstAsterisk;
        if (asterisk > 0 && asterisk < text.Length - 1)
            throw new FormatException();

        bool isPrefix = asterisk == text.Length - 1;
        bool isSuffix = asterisk == 0;

        if (isPrefix)
        {
            text = text.Substring(0, text.Length - 1);
            if (text.EndsWith('.'))
                text = text.Substring(0, text.Length - 1);
        }
        else if (isSuffix)
        {
            text = text.Substring(1);
        }

        return new PackageFilterExpression(text, isPrefix, isSuffix);
    }

    public bool IsMatch(string packageId)
    {
        if (IsPrefix)
        {
            return packageId.Equals(Text, StringComparison.OrdinalIgnoreCase) ||
                   packageId.StartsWith(Text + ".", StringComparison.OrdinalIgnoreCase);
        }
        else if (IsSuffix)
        {
            return packageId.EndsWith(Text, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            return packageId.Equals(Text, StringComparison.OrdinalIgnoreCase);
        }
    }
}

public sealed class PackageFilter(IEnumerable<PackageFilterExpression> includes, IEnumerable<PackageFilterExpression> excludes)
{
    public ImmutableArray<PackageFilterExpression> Includes { get; } = includes.ToImmutableArray();
    public ImmutableArray<PackageFilterExpression> Excludes { get; } = excludes.ToImmutableArray();

    public bool IsMatch(string packageId)
    {
        return Includes.Any(e => e.IsMatch(packageId)) &&
               !Excludes.Any(e => e.IsMatch(packageId));
    }

    public static PackageFilter Default { get; } = new(
        includes:
        [
            PackageFilterExpression.Parse("Microsoft.Bcl.*"),
            PackageFilterExpression.Parse("Microsoft.Extensions.*"),
            PackageFilterExpression.Parse("Microsoft.Win32.*"),
            PackageFilterExpression.Parse("System.*"),
        ],
        excludes:
        [
            PackageFilterExpression.Parse("*.cs"),
            PackageFilterExpression.Parse("*.de"),
            PackageFilterExpression.Parse("*.es"),
            PackageFilterExpression.Parse("*.fr"),
            PackageFilterExpression.Parse("*.it"),
            PackageFilterExpression.Parse("*.ja"),
            PackageFilterExpression.Parse("*.ko"),
            PackageFilterExpression.Parse("*.pl"),
            PackageFilterExpression.Parse("*.pt-br"),
            PackageFilterExpression.Parse("*.ru"),
            PackageFilterExpression.Parse("*.tr"),
            PackageFilterExpression.Parse("*.zh-Hans"),
            PackageFilterExpression.Parse("*.zh-Hant"),
        ]
    );
}
