using System.Xml.Linq;

internal class DocsMember : DocsAPI
{
    private string? _memberName;
    private List<DocsMemberSignature>? _memberSignatures;
    private List<DocsException>? _exceptions;

    public DocsMember(string filePath, DocsType parentType, XElement xeMember)
        : base(xeMember)
    {
        FilePath = filePath;
        ParentType = parentType;
        AssemblyInfos.AddRange(XERoot.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)));
    }

    public DocsType ParentType { get; private set; }

    public override bool Changed
    {
        get => ParentType.Changed;
        set => ParentType.Changed |= value;
    }
    public bool IsProperty => MemberType == "Property";

    public bool IsMethod => MemberType == "Method";

    public string MemberName
    {
        get
        {
            if (_memberName == null)
            {
                _memberName = XmlHelper.GetAttributeValue(XERoot, "MemberName");
            }
            return _memberName;
        }
    }

    public List<DocsMemberSignature> MemberSignatures
    {
        get
        {
            if (_memberSignatures == null)
            {
                _memberSignatures = XERoot.Elements("MemberSignature").Select(x => new DocsMemberSignature(x)).ToList();
            }
            return _memberSignatures;
        }
    }

    public string MemberType
    {
        get
        {
            return XmlHelper.GetChildElementValue(XERoot, "MemberType");
        }
    }

    public string ImplementsInterfaceMember
    {
        get
        {
            XElement? xeImplements = XERoot.Element("Implements");
            return (xeImplements != null) ? XmlHelper.GetChildElementValue(xeImplements, "InterfaceMember") : string.Empty;
        }
    }

    public override string ReturnType
    {
        get
        {
            XElement? xeReturnValue = XERoot.Element("ReturnValue");
            if (xeReturnValue != null)
            {
                return XmlHelper.GetChildElementValue(xeReturnValue, "ReturnType");
            }
            return string.Empty;
        }
    }

    public override string Returns
    {
        get
        {
            return (ReturnType != "System.Void") ? GetNodesInPlainText("returns") : string.Empty;
        }
        set
        {
            if (ReturnType != "System.Void")
            {
                SaveFormattedAsXml("returns", value, addIfMissing: false);
            }
            else
            {
                Log.Warning($"Attempted to save a returns item for a method that returns System.Void: {DocId}");
            }
        }
    }

    public override string Summary
    {
        get
        {
            return GetNodesInPlainText("summary");
        }
        set
        {
            SaveFormattedAsXml("summary", value, addIfMissing: true);
        }
    }

    public override string Remarks
    {
        get
        {
            return GetNodesInPlainText("remarks");
        }
        set
        {
            SaveAsIs("remarks", value, addIfMissing: !value.IsDocsEmpty());
        }
    }

    public string Value
    {
        get
        {
            return (IsProperty) ? GetNodesInPlainText("value") : string.Empty;
        }
        set
        {
            if (IsProperty)
            {
                SaveFormattedAsXml("value", value, addIfMissing: true);
            }
            else
            {
                Log.Warning($"Attempted to save a value element for an API that is not a property: {DocId}");
            }
        }
    }

    public List<DocsException> Exceptions
    {
        get
        {
            if (_exceptions == null)
            {
                if (Docs != null)
                {
                    _exceptions = Docs.Elements("exception").Select(x => new DocsException(this, x)).ToList();
                }
                else
                {
                    _exceptions = new List<DocsException>();
                }
            }
            return _exceptions;
        }
    }

    public override string ToString()
    {
        return DocId;
    }

    public DocsException AddException(string cref, string value)
    {
        XElement exception = new XElement("exception");
        exception.SetAttributeValue("cref", cref);
        XmlHelper.SaveFormattedAsXml(exception, value, removeUndesiredEndlines: false);
        Docs.Add(exception);
        Changed = true;
        return new DocsException(this, exception);
    }

    protected override string GetApiSignatureDocId()
    {
        DocsMemberSignature? dts = MemberSignatures.FirstOrDefault(x => x.Language == "DocId");
        if (dts == null)
        {
            throw new FormatException($"DocId TypeSignature not found for {MemberName}");
        }
        return dts.Value;
    }
}