using YamlDotNet.Serialization;

namespace WhatsNew.Infrastructure.Models;

public class Toc
{
    [YamlMember(Alias = "items", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public List<TocNode> Items { get; set; } = new List<TocNode>();
}

public class TocNode
{
    [YamlMember(Alias ="name",DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string Name { get; set; } = default!;
    [YamlMember(Alias = "href", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Href { get; set; }
    [YamlMember(Alias = "expanded", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public bool? Expanded { get; set; }
    [YamlMember(Alias = "displayName", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? DisplayName { get; set; }
    [YamlMember(Alias = "items", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public List<TocNode>? Items { get; set; }

    // generally not used:

    [YamlMember(Alias = "homepage", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Homepage { get; set; }
    [YamlMember(Alias = "preserveContext", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public bool? PreserveContext { get; set; }
    [YamlMember(Alias = "tocHref", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? TocHRef { get; set; }
    [YamlMember(Alias = "topicHref", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? TopicHref { get; set; }
    [YamlMember(Alias = "topicUid", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? TopicUid { get; set; }
    [YamlMember(Alias = "type", ApplyNamingConventions = false, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? NodeType { get; set; }

}
