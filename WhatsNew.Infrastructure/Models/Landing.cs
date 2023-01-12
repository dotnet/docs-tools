using YamlDotNet.Serialization;

namespace WhatsNew.Infrastructure.Models;

public class Landing
{
    [YamlMember(Alias = "title", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string Title { get; set; } = default!;
    [YamlMember(Alias = "summary", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string Summary { get; set; } = default!;
    [YamlMember(Alias = "metadata", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public MetaDataBlock Metadata { get; set; } = default!;

    [YamlMember(Alias = "landingContent", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public List<Tile> LandingContent { get; set; } = default!;
}

public class MetaDataBlock
{
    [YamlMember(Alias = "title", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string Title { get; set; } = default!;
    [YamlMember(Alias = "description", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string Description { get; set; } = default!;
    [YamlMember(Alias = "ms.date", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string MsDate { get; set; } = default!;
    [YamlMember(Alias = "ms.topic", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string MsTopic { get; set; } = default!;
}

public class Tile
{
    [YamlMember(Alias = "title", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string Title { get; set; } = default!;
    [YamlMember(Alias = "linkLists", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public List<LinkSection> LinkLists { get; set; } = default!;
}

public class LinkSection
{
    [YamlMember(Alias = "linkListType", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string LinkListType { get; set; } = default!;
    [YamlMember(Alias = "links", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public List<Article> Links { get; set; } = default!;
}

public class Article
{
    [YamlMember(Alias = "text", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string Text { get; set; } = default!;
    [YamlMember(Alias = "url", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string Url { get; set; } = default!;
}
