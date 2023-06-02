using System.Xml.Linq;

internal class DocsParam
{
    private readonly XElement XEDocsParam;
    public IDocsAPI ParentAPI
    {
        get; private set;
    }
    public string Name
    {
        get
        {
            return XmlHelper.GetAttributeValue(XEDocsParam, "name");
        }
    }
    public string Value
    {
        get
        {
            return XmlHelper.GetNodesInPlainText(XEDocsParam);
        }
    }
    public DocsParam(IDocsAPI parentAPI, XElement xeDocsParam)
    {
        ParentAPI = parentAPI;
        XEDocsParam = xeDocsParam;
    }
    public override string ToString()
    {
        return Name;
    }
}
