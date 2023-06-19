using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace XmlDocConflictResolver;

internal abstract class DocsAPI : IDocsAPI
{
    private string? _docId;
    private string? _docIdUnprefixed;
    private List<DocsParam>? _params;
    private List<DocsTypeParam>? _typeParams;
    private List<DocsAssemblyInfo>? _assemblyInfos;
    private List<string>? _seeAlsoCrefs;
    private List<string>? _altMemberCrefs;
    private List<DocsRelated>? _relateds;
    private XElement? _xInheritDoc = null;
    private string? _inheritDocCref = null;

    protected readonly XElement XERoot;

    protected DocsAPI(XElement xeRoot) => XERoot = xeRoot;

    public bool IsUndocumented =>
        Summary.IsDocsEmpty() ||
        Returns.IsDocsEmpty() ||
        Params.Any(p => p.Value.IsDocsEmpty()) ||
        TypeParams.Any(tp => tp.Value.IsDocsEmpty());

    public abstract bool Changed { get; set; }
    public string FilePath { get; set; } = string.Empty;

    public string DocId => _docId ??= GetApiSignatureDocId();

    public string DocIdUnprefixed => _docIdUnprefixed ??= DocId[2..];

    public XElement Docs
    {
        get
        {
            return XERoot.Element("Docs") ?? throw new NullReferenceException($"Docs section was null in {FilePath}");
        }
    }

    /// <summary>
    ///  The param elements found inside the Docs section.
    /// </summary>
    public List<DocsParam> Params
    {
        get
        {
            if (_params == null)
            {
                if (Docs != null)
                {
                    _params = Docs.Elements("param").Select(x => new DocsParam(this, x)).ToList();
                }
                else
                {
                    _params = new List<DocsParam>();
                }
            }
            return _params;
        }
    }

    /// <summary>
    /// The typeparam elements found inside the Docs section.
    /// </summary>
    public List<DocsTypeParam> TypeParams
    {
        get
        {
            if (_typeParams == null)
            {
                if (Docs != null)
                {
                    _typeParams = Docs.Elements("typeparam").Select(x => new DocsTypeParam(this, x)).ToList();
                }
                else
                {
                    _typeParams = new();
                }
            }
            return _typeParams;
        }
    }

    public List<string> SeeAlsoCrefs
    {
        get
        {
            if (_seeAlsoCrefs == null)
            {
                if (Docs != null)
                {
                    _seeAlsoCrefs = Docs.Elements("seealso").Select(x => XmlHelper.GetAttributeValue(x, "cref")).ToList();
                }
                else
                {
                    _seeAlsoCrefs = new();
                }
            }
            return _seeAlsoCrefs;
        }
    }

    public List<string> AltMembers
    {
        get
        {
            if (_altMemberCrefs == null)
            {
                if (Docs != null)
                {
                    _altMemberCrefs = Docs.Elements("altmember").Select(x => XmlHelper.GetAttributeValue(x, "cref")).ToList();
                }
                else
                {
                    _altMemberCrefs = new();
                }
            }
            return _altMemberCrefs;
        }
    }

    public List<DocsRelated> Relateds
    {
        get
        {
            if (_relateds == null)
            {
                if (Docs != null)
                {
                    _relateds = Docs.Elements("related").Select(x => new DocsRelated(this, x)).ToList();
                }
                else
                {
                    _relateds = new();
                }
            }
            return _relateds;
        }
    }

    private XElement? XInheritDoc
    {
        get
        {
            return _xInheritDoc ??= Docs.Elements("inheritdoc").FirstOrDefault();
        }
        set
        {
            _xInheritDoc = value;
        }
    }

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
        set
        {
            // Null to remove
            if (value == null)
            {
                XInheritDoc = null;
                _inheritDocCref = null;
            }
            // Non-null to add
            else
            {
                _inheritDocCref = value; // Can be empty string too
                if (XInheritDoc == null) // Not found in Docs
                {
                    XInheritDoc = new XElement("inheritdoc");
                    Docs.Add(XInheritDoc);
                }
                // Only set cref if non-empty
                if (_inheritDocCref.Length == 0)
                {
                    XInheritDoc.RemoveAttributes();
                }
                else
                {
                    XInheritDoc.SetAttributeValue("cref", value);
                }
            }
            Changed = true;
        }
    }

    public bool InheritDoc => XInheritDoc != null;

    public abstract string Summary { get; }
    public abstract string Returns { get; }

    public List<DocsAssemblyInfo> AssemblyInfos
    {
        get
        {
            if (_assemblyInfos == null)
            {
                _assemblyInfos = new List<DocsAssemblyInfo>();
            }
            return _assemblyInfos;
        }
    }

    public APIKind Kind
    {
        get
        {
            return this switch
            {
                DocsMember _ => APIKind.Member,
                DocsType _ => APIKind.Type,
                _ => throw new ArgumentException("Unrecognized IDocsAPI object")
            };
        }
    }

    // For Types, these elements are called TypeSignature.
    // For Members, these elements are called MemberSignature.
    protected abstract string GetApiSignatureDocId();

    protected string GetNodesInPlainText(string name)
    {
        if (TryGetElement(name, out XElement? element))
        {
            if (name == "remarks")
            {
                XElement? formatElement = element.Element("format");
                if (formatElement != null)
                {
                    element = formatElement;
                }
            }

            return XmlHelper.GetNodesInPlainText(element);
        }
        return string.Empty;
    }

    // Returns true if the element existed or had to be created
    // with "To be added." as value. Returns false the element was not found and a new one was not created.
    private bool TryGetElement(string name, [NotNullWhen(returnValue: true)] out XElement? element)
    {
        element = null;

        if (Docs == null)
        {
            return false;
        }

        element = Docs.Element(name);

        return element != null;
    }
}