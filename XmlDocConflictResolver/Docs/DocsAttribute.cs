using System.Xml.Linq;

internal class DocsAttribute
{
    private readonly XElement XEAttribute;

    public string FrameworkAlternate
    {
        get
        {
            return XmlHelper.GetAttributeValue(XEAttribute, "FrameworkAlternate");
        }
    }
    public string AttributeName
    {
        get
        {
            return XmlHelper.GetChildElementValue(XEAttribute, "AttributeName");
        }
    }

    public DocsAttribute(XElement xeAttribute)
    {
        XEAttribute = xeAttribute;
    }
}