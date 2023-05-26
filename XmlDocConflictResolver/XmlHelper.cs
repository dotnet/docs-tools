using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

internal class XmlHelper
{
    public static string GetAttributeValue(XElement parent, string name)
    {
        if (parent == null)
        {
            throw new Exception($"A null parent was passed when attempting to get attribute '{name}'");
        }
        else
        {
            XAttribute? attr = parent.Attribute(name);
            if (attr != null)
            {
                return attr.Value.Trim();
            }
        }
        return string.Empty;
    }

    public static bool TryGetChildElement(XElement parent, string name, out XElement? child)
    {
        child = null;

        if (parent == null || string.IsNullOrWhiteSpace(name))
            return false;

        child = parent.Element(name);

        return child != null;
    }

    public static string GetChildElementValue(XElement parent, string childName)
    {
        XElement? child = parent.Element(childName);

        if (child != null)
        {
            return GetNodesInPlainText(child);
        }

        return string.Empty;
    }

    public static string GetNodesInPlainText(XElement element)
    {
        if (element == null)
        {
            throw new Exception("A null element was passed when attempting to retrieve the nodes in plain text.");
        }

        return string.Join("", element.Nodes()).Trim();
    }

    public static void SaveAsIs(XElement element, string newValue)
    {
        if (element == null)
        {
            throw new Exception("A null element was passed when attempting to save text into it.");
        }

        element.Value = string.Empty;

        var attributes = element.Attributes();

        // Workaround: <x> will ensure XElement does not complain about having an invalid xml object inside. Those tags will be removed by replacing the nodes.
        XElement parsedElement;
        try
        {
            parsedElement = XElement.Parse("<x>" + newValue + "</x>");
        }
        catch (XmlException)
        {
            parsedElement = XElement.Parse("<x>" + newValue.Replace("<", "&lt;").Replace(">", "&gt;") + "</x>");
        }

        element.ReplaceNodes(parsedElement.Nodes());

        // Ensure attributes are preserved after replacing nodes
        element.ReplaceAttributes(attributes);
    }

    public static void SaveFormattedAsXml(XElement element, string newValue, bool removeUndesiredEndlines = true)
    {
        if (element == null)
        {
            throw new Exception("A null element was passed when attempting to save formatted as xml");
        }

        element.Value = string.Empty;

        var attributes = element.Attributes();

        string updatedValue = GetFormattedAsXml(newValue, removeUndesiredEndlines);

        // Workaround: <x> will ensure XElement does not complain about having an invalid xml object inside. Those tags will be removed by replacing the nodes.
        XElement parsedElement;
        try
        {
            parsedElement = XElement.Parse("<x>" + updatedValue + "</x>");
        }
        catch (XmlException)
        {
            parsedElement = XElement.Parse("<x>" + updatedValue.Replace("<", "&lt;").Replace(">", "&gt;") + "</x>");
        }

        element.ReplaceNodes(parsedElement.Nodes());

        // Ensure attributes are preserved after replacing nodes
        element.ReplaceAttributes(attributes);
    }

    public static void AppendFormattedAsXml(XElement element, string valueToAppend, bool removeUndesiredEndlines)
    {
        if (element == null)
        {
            throw new Exception("A null element was passed when attempting to append formatted as xml");
        }

        SaveFormattedAsXml(element, GetNodesInPlainText(element) + valueToAppend, removeUndesiredEndlines);
    }

    public static void AddChildFormattedAsXml(XElement parent, XElement child, string childValue)
    {
        if (parent == null)
        {
            throw new Exception("A null parent was passed when attempting to add child formatted as xml");
        }

        if (child == null)
        {
            throw new Exception("A null child was passed when attempting to add child formatted as xml");
        }

        SaveFormattedAsXml(child, childValue);
        parent.Add(child);
    }

    private static string RemoveUndesiredEndlines(string value)
    {
        value = Regex.Replace(value, @"((?'undesiredEndlinePrefix'[^\.\:])(\r\n)+[ \t]*)", @"${undesiredEndlinePrefix} ");

        return value.Trim();
    }
}