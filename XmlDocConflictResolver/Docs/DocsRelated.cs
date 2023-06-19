using System.Xml.Linq;

internal class DocsRelated
{
    private readonly XElement _xERelatedArticle;

    public IDocsAPI ParentAPI
    {
        get; private set;
    }

    public string ArticleType => XmlHelper.GetAttributeValue(_xERelatedArticle, "type");
    public string Href => XmlHelper.GetAttributeValue(_xERelatedArticle, "href");
    public string Value => XmlHelper.GetNodesInPlainText(_xERelatedArticle);

    public DocsRelated(IDocsAPI parentAPI, XElement xeRelatedArticle)
    {
        ParentAPI = parentAPI;
        _xERelatedArticle = xeRelatedArticle;
    }

    public override string ToString() => Value;
}