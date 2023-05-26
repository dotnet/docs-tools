using System.Xml.Linq;

internal class IntelliSenseXmlMember
{
    private readonly XElement XEMember;

    private XElement? _xInheritDoc = null;
    private XElement? XInheritDoc => _xInheritDoc ??= XEMember.Elements("inheritdoc").FirstOrDefault();

    public string Assembly { get; private set; }

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

    private List<List<IntelliSenseXmlParam>> _params;
    public List<List<IntelliSenseXmlParam>> Params
    {
        get
        {
            if (_params == null)
            {
                // Max capacity is 2: the original text and the incoming text.
                _params = new List<List<IntelliSenseXmlParam>>(2);

                List<IntelliSenseXmlParam>? existingParams = 
                    XEMember.Elements("param").Select(x => new IntelliSenseXmlParam(x)).ToList();

                _params.Add(existingParams);
            }
            return _params;
        }
    }

    private List<List<IntelliSenseXmlTypeParam>> _typeParams;
    public List<List<IntelliSenseXmlTypeParam>> TypeParams
    {
        get
        {
            if (_typeParams == null)
            {
                // Max capacity is 2: the original text and the incoming text.
                _typeParams = new List<List<IntelliSenseXmlTypeParam>>(2);

                List<IntelliSenseXmlTypeParam>? existingTypeParams =
                    XEMember.Elements("typeparam").Select(x => new IntelliSenseXmlTypeParam(x)).ToList();

                _typeParams.Add(existingTypeParams);
            }
            return _typeParams;
        }
    }

    private List<List<IntelliSenseXmlException>> _exceptions;
    public List<List<IntelliSenseXmlException>> Exceptions
    {
        get
        {
            if (_exceptions == null)
            {
                // Max capacity is 2: the original text and the incoming text.
                _exceptions = new List<List<IntelliSenseXmlException>>(2);

                List<IntelliSenseXmlException>? existingExceptions =
                    XEMember.Elements("exception").Select(x => new IntelliSenseXmlException(x)).ToList();

                _exceptions.Add(existingExceptions);
            }
            return _exceptions;
        }
    }

    private List<string?> _summary;
    public List<string> Summary
    {
        get
        {
            if (_summary == null)
            {
                // Max capacity is 2: the original text and the incoming text.
                _summary = new List<string?>(2);

                XElement? xElement = XEMember.Element("summary");
                if (xElement != null)
                {
                    _summary.Add(XmlHelper.GetNodesInPlainText(xElement));
                }
            }
            return _summary;
        }
    }

    public List<string?> _value;
    public List<string?> Value
    {
        get
        {
            if (_value == null)
            {
                // Max capacity is 2: the original text and the incoming text.
                _value = new List<string?>(2);

                XElement? xElement = XEMember.Element("value");
                if (xElement != null)
                {
                    _value.Add(XmlHelper.GetNodesInPlainText(xElement));
                }
            }
            return _value;
        }
    }

    private List<string?> _returns;
    public List<string?> Returns
    {
        get
        {
            if (_returns == null)
            {
                // Max capacity is 2: the original text and the incoming text.
                _returns = new List<string?>(2);

                XElement? xElement = XEMember.Element("returns");
                if (xElement != null)
                {
                    _returns.Add(XmlHelper.GetNodesInPlainText(xElement));
                }
            }
            return _returns;
        }
    }

    public IntelliSenseXmlMember(XElement xeMember, string assembly)
    {
        if (string.IsNullOrEmpty(assembly))
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        XEMember = xeMember ?? throw new ArgumentNullException(nameof(xeMember));
        Assembly = assembly.Trim();
    }

    public override string ToString()
    {
        return Name;
    }
}
