using System.Diagnostics.CodeAnalysis;
using System.Text;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace DotNetDocs.RepoMan;

internal partial class InstanceData
{
    public SettingsConfig? Settings { get; set; }

    [MemberNotNull(nameof(Settings))]
    public void LoadSettings(YamlNode settingsNode)
    {
        Deserializer deserializer = new();
        YamlStream resultStream = new(new YamlDocument(settingsNode));
        StringBuilder builder = new();
        using StringWriter writer = new(builder);
        resultStream.Save(writer);
        Settings = deserializer.Deserialize<SettingsConfig>(builder.ToString());
    }

    public class SettingsConfig
    {
        public ConfigDocMetadata DocMetadata { get; set; } = new();

        public class ConfigDocMetadata
        {
            // DELETE ME
            public string? ParserRegex { get; set; }
            // DELETE ME
            public List<string[]>? Headers;


            public List<string>? ContentUrlRegex { get; set; }
        }
    }
}
