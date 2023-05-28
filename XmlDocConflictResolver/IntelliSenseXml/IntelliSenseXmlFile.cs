using System.Text;
using System.Xml.Linq;

internal class IntelliSenseXmlFile
{
    public IntelliSenseXmlFile(XDocument xDoc, string filePath, Encoding encoding)
    {
        Xdoc = xDoc;
        FilePath = filePath;
        FileEncoding = encoding;
    }

    public XDocument Xdoc { get; private set; }
    public string FilePath { get; private set; }
    public Encoding FileEncoding { get; private set; }
    public bool Changed { get; set; } = false;
}
