using System.Xml.Linq;

internal class IntelliSenseXmlException
{
    public XElement XEException
    {
        get;
        private set;
    }

    private string _cref = string.Empty;
    public string Cref
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_cref))
            {
                _cref = XmlHelper.GetAttributeValue(XEException, "cref");
            }
            return _cref;
        }
    }

    private string _value = string.Empty;
    public string Value
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_value))
            {
                _value = XmlHelper.GetNodesInPlainText(XEException);
            }
            return _value;
        }
        set
        {
            _value = value;

            XEException.Value = value;
        }
    }

    public IntelliSenseXmlException(XElement xeException)
    {
        XEException = xeException;
    }

    public override string ToString()
    {
        return $"{Cref} - {Value}";
    }
}
