using WhatsNew.Infrastructure.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WhatsNew.Infrastructure.Services;

// Assumptions:
// 1. Toc node name is "Month YYYY" **valid**
// Later enhancement: The code assumes the parent node is a child of the root node.
// This may need to be updated to have the parent node be anywhere in the tree.
public class TocUpdateService
{
    private readonly WhatsNewConfiguration _configuration;

    public TocUpdateService(WhatsNewConfiguration config) => _configuration = config;

    public async Task UpdateWhatsNewToc()
    {
        if (_configuration.Repository.NavigationOptions?.RepoTocFolder is null)
        {
            Console.WriteLine("No TOC folder specified in the configuration. Skipping TOC update.");
            return;
        }

        // Update TOC.YML:
        // 1. Read Toc:
        var tocFile = Path.Combine(Path.Combine(_configuration.PathToRepoRoot, _configuration.Repository.NavigationOptions.RepoTocFolder), "toc.yml");
        using var reader = new StreamReader(tocFile);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var toc = deserializer.Deserialize<Toc>(reader);
        reader.Close();
        TocNode? SearchChildren(TocNode item, string target)
        {
            TocNode? result = null;
            if (item.Items is not null)
            {
                foreach (var child in item.Items)
                {
                    result ??= SearchChildren(child, target);
                }
            }
            result ??= item.Name == target ? item : null;
            return result;
        }
        TocNode? newArticles = default;
        foreach(var node in toc.Items)
            newArticles ??= SearchChildren(node, _configuration.Repository.NavigationOptions.TocParentNode);

        // 2. Add new article, if it doesn't already exist
        string articleName = _configuration.DateRange.StartDate.ToString("MMMM yyyy");
        if (!newArticles?.Items?.Any(item =>item.Name.Equals(articleName)) ?? false)
        {
            var newNode = new TocNode
            {
                Name = articleName,
                Href = _configuration.MarkdownFileName,
            };
            newArticles?.Items?.Insert(0, newNode);
        }

        // 3 remove outdated article.
        while (newArticles?.Items?.Count > _configuration.Repository.NavigationOptions.MaximumNumberOfArticles)
        {
            newArticles?.Items.RemoveAt(newArticles.Items.Count - 1);
        }

        var serializer = new SerializerBuilder().Build();
        var yaml = serializer.Serialize(toc);
        await File.WriteAllTextAsync(tocFile, yaml);
        Console.WriteLine($"Updated the table of contents \"{tocFile}\"");
    }
}
