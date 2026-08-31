using BuildVerifier.IO.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;

namespace RedirectionVerifier;

public class DocfxConfigurationReader
    : BaseMappedConfigurationReader<DocfxConfiguration, IEnumerable<Matcher>>
{
    private static readonly Matcher s_matchAllMatcher = new Matcher().AddInclude("**");

    public DocfxConfigurationReader()
    {
        ConfigurationFileName = "docfx.json";
    }

    public override async ValueTask<IEnumerable<Matcher>> MapConfigurationAsync()
    {
        DocfxConfiguration? configuration = await ReadConfigurationAsync();
        return AdjustMatchers(configuration?.GetMatchers(ConfigurationDirectory));
    }

    private static IEnumerable<Matcher> AdjustMatchers(IEnumerable<Matcher>? matchers)
        => (matchers is null || !matchers.Any())
            ? [s_matchAllMatcher]
            : matchers;
}
