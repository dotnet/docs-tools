using System.Xml.Linq;

internal class IntelliSenseXmlMember
{
    private readonly XElement XEMember;

    //public bool Changed = false;

    private XElement? _xInheritDoc = null;
    private XElement? XInheritDoc => _xInheritDoc ??= XEMember.Elements("inheritdoc").FirstOrDefault();

    //public string Assembly { get; private set; }
    public IntelliSenseXmlFile XmlFile { get; private set; }

    private string? _inheritDocCref = null;
    public string InheritDocCref
    {
        get
        {
            if (_inheritDocCref == null)
            {
                _inheritDocCref = string.Empty;
                if (InheritDoc && XInheritDoc != null)
                {
                    XAttribute? xInheritDocCref = XInheritDoc.Attribute("cref");
                    if (xInheritDocCref != null)
                    {
                        _inheritDocCref = xInheritDocCref.Value;
                    }
                }
            }
            return _inheritDocCref;
        }
    }

    public bool InheritDoc
    {
        get => XInheritDoc != null;
    }

    private string _namespace = string.Empty;
    public string Namespace
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_namespace))
            {
                string[] splittedParenthesis = Name.Split('(', StringSplitOptions.RemoveEmptyEntries);
                string withoutParenthesisAndPrefix = splittedParenthesis[0][2..]; // Exclude the "X:" prefix
                string[] splittedDots = withoutParenthesisAndPrefix.Split('.', StringSplitOptions.RemoveEmptyEntries);

                // TODO: For nested classes, this needs to be:
                // _namespace = string.Join('.', splittedDots.Take(splittedDots.Length - 2));
                _namespace = string.Join('.', splittedDots.Take(splittedDots.Length - 1));
            }

            return _namespace;
        }
    }

    private string? _name;

    /// <summary>
    /// The API DocId.
    /// </summary>
    public string Name => _name ??= XmlHelper.GetAttributeValue(XEMember, "name");

    private List<IntelliSenseXmlParam>? _params;
    public List<IntelliSenseXmlParam> Params
    {
        get
        {
            if (_params == null)
            {
                _params = XEMember.Elements("param").Select(x => new IntelliSenseXmlParam(x)).ToList();
            }
            return _params;
        }
    }

    private List<IntelliSenseXmlTypeParam>? _typeParams;
    public List<IntelliSenseXmlTypeParam> TypeParams
    {
        get
        {
            if (_typeParams == null)
            {
                _typeParams = XEMember.Elements("typeparam").Select(x => new IntelliSenseXmlTypeParam(x)).ToList();
            }
            return _typeParams;
        }
    }

    private List<IntelliSenseXmlException>? _exceptions;
    public IEnumerable<IntelliSenseXmlException> Exceptions
    {
        get
        {
            if (_exceptions == null)
            {
                _exceptions = XEMember.Elements("exception").Select(x => new IntelliSenseXmlException(x)).ToList();
            }
            return _exceptions;
        }
    }

    private string? _summary;
    public string Summary
    {
        get
        {
            if (_summary == null)
            {
                XElement? xElement = XEMember.Element("summary");
                _summary = (xElement != null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
            }
            return _summary;
        }
        set
        {
            _summary = value;

            // Update the XElement.
            XElement? xElement = XEMember.Element("summary");
            if (xElement != null) { xElement.Value = value; }
        }
    }

    public string? _value;
    public string Value
    {
        get
        {
            if (_value == null)
            {
                XElement? xElement = XEMember.Element("value");
                _value = (xElement != null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
            }
            return _value;
        }
        set
        {
            _value = value;

            // Update the XElement.
            XElement? xElement = XEMember.Element("value");
            if (xElement != null) { xElement.Value = value; }
        }
    }

    private string? _returns;
    public string Returns
    {
        get
        {
            if (_returns == null)
            {
                XElement? xElement = XEMember.Element("returns");
                _returns = (xElement != null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
            }
            return _returns;
        }
        set
        {
            _returns = value;

            // Update the XElement.
            XElement? xElement = XEMember.Element("returns");
            if (xElement != null) { xElement.Value = value; }
        }
    }

    public IntelliSenseXmlMember(XElement xeMember, IntelliSenseXmlFile xmlFile)
    {
        XEMember = xeMember ?? throw new ArgumentNullException(nameof(xeMember));
        XmlFile = xmlFile ?? throw new ArgumentNullException(nameof(xmlFile));
    }

    public override string ToString()
    {
        return Name;
    }

    public bool IsType() => Name.StartsWith("T:");
}
