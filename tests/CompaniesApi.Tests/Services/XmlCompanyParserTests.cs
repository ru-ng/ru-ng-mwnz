using CompaniesApi.Services.Parsers;
using FluentAssertions;
using Xunit;

namespace CompaniesApi.Tests.Services;

public sealed class XmlCompanyParserTests
{
    private readonly XmlCompanyParser _sut = new();

    private const string ValidXml = """
        <Data>
          <id>1</id>
          <name>MWNZ</name>
          <description>..is awesome</description>
        </Data>
        """;

    [Fact]
    public void Parse_ValidXml_ReturnsCompanyWithCorrectId()
    {
        _sut.Parse(ValidXml).Id.Should().Be(1);
    }

    [Fact]
    public void Parse_ValidXml_ReturnsCompanyWithCorrectName()
    {
        _sut.Parse(ValidXml).Name.Should().Be("MWNZ");
    }

    [Fact]
    public void Parse_ValidXml_ReturnsCompanyWithCorrectDescription()
    {
        _sut.Parse(ValidXml).Description.Should().Be("..is awesome");
    }

    [Fact]
    public void Parse_MalformedXml_Throws()
    {
        FluentActions.Invoking(() => _sut.Parse("not xml"))
            .Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_WrongRootElement_Throws()
    {
        FluentActions.Invoking(() => _sut.Parse("<Wrong><id>1</id></Wrong>"))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_MissingId_Throws()
    {
        FluentActions.Invoking(() => _sut.Parse("<Data><name>MWNZ</name><description>x</description></Data>"))
            .Should().Throw<InvalidOperationException>();
    }
}