using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace RepoMan
{
    public static class YamlExtensions
    {
        public static int ToInt(this YamlNode node) =>
            Convert.ToInt32(((YamlScalarNode)node).Value);

        public static bool Exists(this YamlMappingNode node, string name) =>
            node.Children.ContainsKey(name);

        public static bool Exists(this YamlMappingNode node, string name, [NotNullWhen(true)] out YamlMappingNode? mappingNode)
        {
            if (node.Children.ContainsKey(name))
            {
                mappingNode = (YamlMappingNode)node.Children[name];
                return true;
            }

            mappingNode = null;
            return false;
        }

        public static bool Exists(this YamlMappingNode node, string name, [NotNullWhen(true)] out YamlSequenceNode? sequenceNode)
        {
            if (node.Children.ContainsKey(name))
            {
                sequenceNode = (YamlSequenceNode)node.Children[name];
                return true;
            }

            sequenceNode = null;
            return false;
        }

        public static YamlMappingNode AsMappingNode(this YamlNode node) =>
            node.NodeType == YamlNodeType.Mapping ? (YamlMappingNode)node : throw new InvalidCastException("Node type isn't a mapping node");

        public static YamlSequenceNode AsSequenceNode(this YamlNode node) =>
            node.NodeType == YamlNodeType.Sequence ? (YamlSequenceNode)node : throw new InvalidCastException("Node type isn't a sequence node");

        public static bool IsFirstProperty(this YamlMappingNode node, string name) =>
            node.Children.Keys.First().ToString().Equals(name, StringComparison.OrdinalIgnoreCase);

        public static (string Name, YamlNode Node) FirstProperty(this YamlMappingNode node) =>
            (node.Children.Keys.First().ToString(), node.Children.Values.First());
    }
}
