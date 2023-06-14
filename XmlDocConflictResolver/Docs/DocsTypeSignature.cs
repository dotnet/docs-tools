using System.Xml.Linq;

internal class DocsTypeSignature
{
    private readonly XElement _xETypeSignature;

    public string Language => XmlHelper.GetAttributeValue(_xETypeSignature, "Language");
    public string Value => XmlHelper.GetAttributeValue(_xETypeSignature, "Value");

    public DocsTypeSignature(XElement xeTypeSignature) => _xETypeSignature = xeTypeSignature;
}