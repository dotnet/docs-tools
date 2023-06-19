using System.Xml.Linq;

internal class DocsParam
{
    private readonly XElement _xEDocsParam;

    public IDocsAPI ParentAPI
    {
        get; private set;
    }

    public string Name => XmlHelper.GetAttributeValue(_xEDocsParam, "name");
    public string Value => XmlHelper.GetNodesInPlainText(_xEDocsParam);

    public DocsParam(IDocsAPI parentAPI, XElement xeDocsParam)
    {
        ParentAPI = parentAPI;
        _xEDocsParam = xeDocsParam;
    }

    public override string ToString() => Name;
}
