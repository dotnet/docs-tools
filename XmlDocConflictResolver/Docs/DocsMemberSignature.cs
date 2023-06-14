using System.Xml.Linq;

internal class DocsMemberSignature
{
    private readonly XElement XEMemberSignature;

    public string Language => XmlHelper.GetAttributeValue(XEMemberSignature, "Language");
    public string Value => XmlHelper.GetAttributeValue(XEMemberSignature, "Value");

    public DocsMemberSignature(XElement xeMemberSignature)
    {
        XEMemberSignature = xeMemberSignature;
    }
}