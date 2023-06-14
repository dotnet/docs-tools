using System.Xml.Linq;

/// <summary>
/// Each one of these typeparam objects live inside the Docs section inside the Member object.
/// </summary>
internal class DocsTypeParam
{
    private readonly XElement _xEDocsTypeParam;
    public IDocsAPI ParentAPI
    {
        get; private set;
    }

    public string Name => XmlHelper.GetAttributeValue(_xEDocsTypeParam, "name");

    public string Value => XmlHelper.GetNodesInPlainText(_xEDocsTypeParam);

    public DocsTypeParam(IDocsAPI parentAPI, XElement xeDocsTypeParam)
    {
        ParentAPI = parentAPI;
        _xEDocsTypeParam = xeDocsTypeParam;
    }
}