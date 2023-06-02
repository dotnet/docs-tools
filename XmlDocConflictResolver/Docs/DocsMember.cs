using System.Xml.Linq;

namespace XmlDocConflictResolver
{
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

        // TODO - not sure if these need to be overridden.
        public override string Returns => GetNodesInPlainText("returns");

        public override string Summary => GetNodesInPlainText("summary");

        public string Value => GetNodesInPlainText("value");

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
}