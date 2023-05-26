using System.Xml.Linq;

internal class DocsMemberSignature
{
    private readonly XElement XEMemberSignature;

    public string Language
    {
        get
        {
            return XmlHelper.GetAttributeValue(XEMemberSignature, "Language");
        }
    }

    public string Value
    {
        get
        {
            return XmlHelper.GetAttributeValue(XEMemberSignature, "Value");
        }
    }

    public DocsMemberSignature(XElement xeMemberSignature)
    {
        XEMemberSignature = xeMemberSignature;
    }
}