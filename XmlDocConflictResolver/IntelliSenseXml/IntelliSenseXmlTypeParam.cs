using System.Xml.Linq;

internal class IntelliSenseXmlTypeParam
{
    public XElement XETypeParam;

    private string _name = string.Empty;
    public string Name
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_name))
            {
                _name = XmlHelper.GetAttributeValue(XETypeParam, "name");
            }
            return _name;
        }
    }

    private string _value = string.Empty;
    public string Value
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_value))
            {
                _value = XmlHelper.GetNodesInPlainText(XETypeParam);
            }
            return _value;
        }
        set
        { 
            _value = value;

            XETypeParam.Value = value;
        }
    }

    public IntelliSenseXmlTypeParam(XElement xeTypeParam)
    {
        XETypeParam = xeTypeParam;
    }
}