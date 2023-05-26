using System.Text;
using System.Xml.Linq;

/// <summary>
/// Represents the root xml element (unique) of a Docs xml file, called Type.
/// </summary>
internal class DocsType : DocsAPI
{
    private string? _typeName;
    private string? _name;
    private string? _fullName;
    private string? _namespace;
    private string? _baseTypeName;
    private List<string>? _interfaceNames;
    private List<DocsAttribute>? _attributes;
    private List<DocsTypeSignature>? _typesSignatures;

    public DocsType(string filePath, XDocument xDoc, XElement xeRoot, Encoding encoding)
        : base(xeRoot)
    {
        FilePath = filePath;
        XDoc = xDoc;
        FileEncoding = encoding;
        AssemblyInfos.AddRange(XERoot.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)));
    }

    public XDocument XDoc { get; set; }

    public override bool Changed { get; set; }

    public Encoding FileEncoding { get; internal set; }

    public string TypeName
    {
        get
        {
            if (_typeName == null)
            {
                // DocId uses ` notation for generic types, but it uses . for nested types
                // Name uses + for nested types, but it uses &lt;T&gt; for generic types
                // We need ` notation for generic types and + notation for nested types
                // Only filename gives us that format, but we have to prepend the namespace
                if (DocId.Contains('`') || Name.Contains('+'))
                {
                    _typeName = Namespace + "." + System.IO.Path.GetFileNameWithoutExtension(FilePath);
                }
                else
                {
                    _typeName = FullName;
                }
            }
            return _typeName;
        }
    }

    public string Name
    {
        get
        {
            if (_name == null)
            {
                _name = XmlHelper.GetAttributeValue(XERoot, "Name");
            }
            return _name;
        }
    }

    public string FullName
    {
        get
        {
            if (_fullName == null)
            {
                _fullName = XmlHelper.GetAttributeValue(XERoot, "FullName");
            }
            return _fullName;
        }
    }

    public string Namespace
    {
        get
        {
            if (_namespace == null)
            {
                int lastDotPosition = FullName.LastIndexOf('.');
                _namespace = lastDotPosition < 0 ? FullName : FullName.Substring(0, lastDotPosition);
            }
            return _namespace;
        }
    }

    public List<DocsTypeSignature> TypeSignatures
    {
        get
        {
            if (_typesSignatures == null)
            {
                _typesSignatures = XERoot.Elements("TypeSignature").Select(x => new DocsTypeSignature(x)).ToList();
            }
            return _typesSignatures;
        }
    }

    public XElement? Base
    {
        get
        {
            return XERoot.Element("Base");
        }
    }

    public string BaseTypeName
    {
        get
        {
            if (Base == null)
            {
                _baseTypeName = string.Empty;
            }
            else if (_baseTypeName == null)
            {
                _baseTypeName = XmlHelper.GetChildElementValue(Base, "BaseTypeName");
            }
            return _baseTypeName;
        }
    }

    public XElement? Interfaces
    {
        get
        {
            return XERoot.Element("Interfaces");
        }
    }

    public List<string> InterfaceNames
    {
        get
        {
            if (Interfaces == null)
            {
                _interfaceNames = new List<string>();
            }
            else if (_interfaceNames == null)
            {
                _interfaceNames = Interfaces.Elements("Interface").Select(x => XmlHelper.GetChildElementValue(x, "InterfaceName")).ToList();
            }
            return _interfaceNames;
        }
    }

    public List<DocsAttribute> Attributes
    {
        get
        {
            if (_attributes == null)
            {
                XElement? e = XERoot.Element("Attributes");
                if (e == null)
                {
                    _attributes = new();
                }
                else
                {
                    _attributes = (e != null) ? e.Elements("Attribute").Select(x => new DocsAttribute(x)).ToList() : new List<DocsAttribute>();
                }
            }
            return _attributes;
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

    /// <summary>
    /// Only available when the type is a delegate.
    /// </summary>
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

    /// <summary>
    /// Only available when the type is a delegate.
    /// </summary>
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

    public override string ToString()
    {
        return FullName;
    }

    protected override string GetApiSignatureDocId()
    {
        DocsTypeSignature? dts = TypeSignatures.FirstOrDefault(x => x.Language == "DocId");
        if (dts == null)
        {
            throw new FormatException($"DocId TypeSignature not found for {FullName}");
        }
        return dts.Value;
    }
}