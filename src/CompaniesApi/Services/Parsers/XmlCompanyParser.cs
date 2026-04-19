using System.Xml.Linq;
using CompaniesApi.Models;

namespace CompaniesApi.Services.Parsers;

public sealed class XmlCompanyParser : ICompanyParser
{
    public Company Parse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root ?? throw new InvalidOperationException("Missing root element.");
        if (!string.Equals(root.Name.LocalName, "Data", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unexpected root element '{root.Name.LocalName}'.");

        var parsedId = int.Parse(root.Element("id")?.Value ?? throw new InvalidOperationException("Missing id."));
        var name = root.Element("name")?.Value ?? throw new InvalidOperationException("Missing name.");
        var description = root.Element("description")?.Value ??
                          throw new InvalidOperationException("Missing description.");

        return new Company(parsedId, name, description);
    }
}