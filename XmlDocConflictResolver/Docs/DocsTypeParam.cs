using System.Xml.Linq;

/// <summary>
/// Each one of these typeparam objects live inside the Docs section inside the Member object.
/// </summary>
internal class DocsTypeParam
{
    private readonly XElement XEDocsTypeParam;
    public IDocsAPI ParentAPI
    {
        get; private set;
    }

    public string Name
    {
        get
        {
            return XmlHelper.GetAttributeValue(XEDocsTypeParam, "name");
        }
    }

    public string Value
    {
        get
        {
            return XmlHelper.GetNodesInPlainText(XEDocsTypeParam);
        }
    }

    public DocsTypeParam(IDocsAPI parentAPI, XElement xeDocsTypeParam)
    {
        ParentAPI = parentAPI;
        XEDocsTypeParam = xeDocsTypeParam;
    }
}