using System.Xml.Linq;

internal class DocsAssemblyInfo
{
    private readonly XElement _xEAssemblyInfo;
    public string AssemblyName => XmlHelper.GetChildElementValue(_xEAssemblyInfo, "AssemblyName");

    private List<string>? _assemblyVersions;
    public List<string> AssemblyVersions
    {
        get
        {
            if (_assemblyVersions == null)
            {
                _assemblyVersions = _xEAssemblyInfo.Elements("AssemblyVersion").Select(x => XmlHelper.GetNodesInPlainText(x)).ToList();
            }
            return _assemblyVersions;
        }
    }

    public DocsAssemblyInfo(XElement xeAssemblyInfo)
    {
        _xEAssemblyInfo = xeAssemblyInfo;
    }

    public override string ToString() => AssemblyName;
}