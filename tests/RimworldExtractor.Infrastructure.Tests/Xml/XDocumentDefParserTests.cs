using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Xml;

public class XDocumentDefParserTests
{
    [Fact]
    public async Task ParseAsync_ReturnsXDocumentWithDefsRoot()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/mods/Foo/Defs/T.xml", """
            <?xml version="1.0" encoding="utf-8"?>
            <Defs>
              <ThingDef>
                <defName>Spear</defName>
                <label>spear</label>
              </ThingDef>
            </Defs>
            """);
        var parser = new XDocumentDefParser(fs);

        var doc = await parser.ParseAsync("/mods/Foo/Defs/T.xml", TestContext.Current.CancellationToken);

        doc.Root!.Name.LocalName.Should().Be("Defs");
        doc.Root.Elements("ThingDef").Should().HaveCount(1);
    }

    [Fact]
    public async Task ParseAsync_IgnoresComments()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/a.xml", """
            <Defs>
              <!-- this is a comment -->
              <ThingDef><defName>X</defName></ThingDef>
            </Defs>
            """);
        var parser = new XDocumentDefParser(fs);

        var doc = await parser.ParseAsync("/a.xml", TestContext.Current.CancellationToken);

        doc.Root!.Nodes().OfType<XComment>().Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_OnInvalidXml_Throws()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/bad.xml", "<<<not xml");
        var parser = new XDocumentDefParser(fs);

        var act = () => parser.ParseAsync("/bad.xml", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<System.Xml.XmlException>();
    }
}
