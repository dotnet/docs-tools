using WhatsNew.Infrastructure.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WhatsNew.Infrastructure.Services;

// Assumptions:
// 2. Node title is "Find .NET updates" **invalid**
// 1. Title is "Month YYYY" **valid**
public class IndexUpdateService
{
    private readonly WhatsNewConfiguration _configuration;

    public IndexUpdateService(WhatsNewConfiguration config) => _configuration = config;

    public async Task UpdateWhatsNewLandingPage()
    {
        if (_configuration.Repository.NavigationOptions?.RepoIndexFolder is null)
        {
            Console.WriteLine("No index folder specified in the configuration. Skipping index update.");
            return;
        }

        // Update Index.YML:
        var indexFile = Path.Combine(Path.Combine(_configuration.PathToRepoRoot, _configuration.Repository.NavigationOptions.RepoIndexFolder), "index.yml");
        using var indexReader = new StreamReader(indexFile);
        var indexDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var index = indexDeserializer.Deserialize<Landing>(indexReader);
        indexReader.Close();

        var section = index.LandingContent.Single(s => s.Title == _configuration.Repository.NavigationOptions.IndexParentNode);

        var tile = section.LinkLists.First();
        var articleText = _configuration.DateRange.StartDate.ToString("MMMM yyyy");
        if (!tile.Links.Any(item => item.Text.Equals(articleText)))
        {
            var newArticle = new Article
            {
                Text = articleText,
                Url = _configuration.MarkdownFileName,
            };
            tile.Links.Insert(0, newArticle);
            index.Metadata.MsDate = DateTime.Now.Date.ToString("MM/dd/yyyy");
        }

        while (tile.Links.Count > _configuration.Repository.NavigationOptions.MaximumNumberOfArticles)
        {
            tile.Links.RemoveAt(tile.Links.Count-1);
            index.Metadata.MsDate = DateTime.Now.Date.ToString("MM/dd/yyyy");
        }

        // 3. Write index file
        var indexSerializer = new SerializerBuilder().Build();
        var indexYaml = indexSerializer.Serialize(index);
        using var writer = new StreamWriter(indexFile);
        await writer.WriteLineAsync("### YamlMime:Landing");
        await writer.WriteLineAsync();
        await writer.WriteAsync(indexYaml);
        Console.WriteLine($"Update the index landing page \"{indexFile}\"");

    }
}
