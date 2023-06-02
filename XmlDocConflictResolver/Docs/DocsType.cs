using System.Text;
using System.Xml.Linq;

namespace XmlDocConflictResolver
{
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
        private List<DocsTypeSignature>? _typesSignatures;

        public DocsType(string filePath, XDocument xDoc, XElement xeRoot)
            : base(xeRoot)
        {
            FilePath = filePath;
            XDoc = xDoc;
            AssemblyInfos.AddRange(XERoot.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)));
        }

        public XDocument XDoc { get; set; }

        public override bool Changed { get; set; }

        public string TypeName
        {
            get
            {
                if (_typeName == null)
                {
                    // TODO - is this correct?
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

        public override string Summary => GetNodesInPlainText("summary");

        // TODO: param and typeparam are missing from DocsType (e.g. if it's a delegate)?

        /// <summary>
        /// Only available when the type is a delegate.
        /// </summary>
        public override string Returns => GetNodesInPlainText("returns");

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
}