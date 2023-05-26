using System.Xml.Linq;

internal class DocsException
{
    private readonly XElement XEException;

    public IDocsAPI ParentAPI
    {
        get; private set;
    }

    public string Cref => XmlHelper.GetAttributeValue(XEException, "cref");

    public string Value
    {
        get => XmlHelper.GetNodesInPlainText(XEException);
        private set => XmlHelper.SaveFormattedAsXml(XEException, value);
    }

    public string OriginalValue { get; private set; }

    public DocsException(IDocsAPI parentAPI, XElement xException)
    {
        ParentAPI = parentAPI;
        XEException = xException;
        OriginalValue = Value;
    }

    public void AppendException(string toAppend)
    {
        XmlHelper.AppendFormattedAsXml(XEException, $"\r\n\r\n-or-\r\n\r\n{toAppend}", removeUndesiredEndlines: false);
        ParentAPI.Changed = true;
    }

    public bool WordCountCollidesAboveThreshold(string intelliSenseXmlValue, int threshold)
    {
        Dictionary<string, int> hashIntelliSenseXml = GetHash(intelliSenseXmlValue);
        Dictionary<string, int> hashDocs = GetHash(Value);

        int collisions = 0;
        // Iterate all the words of the IntelliSense xml exception string
        foreach (KeyValuePair<string, int> word in hashIntelliSenseXml)
        {
            // Check if the existing Docs string contained that word
            if (hashDocs.ContainsKey(word.Key))
            {
                // If the total found in Docs is >= than the total found in IntelliSense xml
                // then consider it a collision
                if (hashDocs[word.Key] >= word.Value)
                {
                    collisions++;
                }
            }
        }

        // If the number of word collisions is above the threshold, it probably means
        // that part of the original TS string was included in the Docs string
        double collisionPercentage = (collisions * 100 / (double)hashIntelliSenseXml.Count);
        return collisionPercentage >= threshold;
    }

    public override string ToString()
    {
        return $"{Cref} - {Value}";
    }

    // Gets a dictionary with the count of each character found in the string.
    private Dictionary<string, int> GetHash(string value)
    {
        Dictionary<string, int> hash = new Dictionary<string, int>();
        string[] words = value.Split(new char[] { ' ', '\'', '"', '\r', '\n', '.', ',', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
        {
            if (hash.ContainsKey(word))
            {
                hash[word]++;
            }
            else
            {
                hash.Add(word, 1);
            }
        }
        return hash;
    }
}