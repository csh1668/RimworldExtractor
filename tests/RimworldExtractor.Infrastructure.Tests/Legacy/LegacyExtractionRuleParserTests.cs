using FluentAssertions;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Infrastructure.Legacy;

namespace RimworldExtractor.Infrastructure.Tests.Legacy;

public class LegacyExtractionRuleParserTests
{
    [Theory]
    [InlineData("label")]
    [InlineData("description")]
    public void Parse_TagOnly_HasEmptyLists(string raw)
    {
        var rule = LegacyExtractionRuleParser.Parse(raw);

        rule.Tag.Should().Be(raw);
        rule.Whitelist.Should().BeEmpty();
        rule.Blacklist.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithWhitelist_ExtractsItems()
    {
        var rule = LegacyExtractionRuleParser.Parse("label+ThingDef,PawnKindDef");

        rule.Tag.Should().Be("label");
        rule.Whitelist.Should().BeEquivalentTo(["ThingDef", "PawnKindDef"]);
        rule.Blacklist.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithBlacklist_ExtractsItems()
    {
        var rule = LegacyExtractionRuleParser.Parse("label-TrashDef,JunkDef");

        rule.Tag.Should().Be("label");
        rule.Whitelist.Should().BeEmpty();
        rule.Blacklist.Should().BeEquivalentTo(["TrashDef", "JunkDef"]);
    }

    [Fact]
    public void Parse_WithBoth_ExtractsBoth()
    {
        var rule = LegacyExtractionRuleParser.Parse("label+ThingDef,PawnKindDef-TrashDef");

        rule.Whitelist.Should().BeEquivalentTo(["ThingDef", "PawnKindDef"]);
        rule.Blacklist.Should().BeEquivalentTo(["TrashDef"]);
    }

    [Fact]
    public void Parse_WithBlacklistThenWhitelist_ExtractsBoth()
    {
        var rule = LegacyExtractionRuleParser.Parse("label-TrashDef+ThingDef");

        rule.Whitelist.Should().BeEquivalentTo(["ThingDef"]);
        rule.Blacklist.Should().BeEquivalentTo(["TrashDef"]);
    }

    [Fact]
    public void Parse_EmptyString_Throws()
    {
        var act = () => LegacyExtractionRuleParser.Parse("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RoundTrip_Format_Then_Parse_IsIdempotent()
    {
        var rule = new ExtractionRule(
            "label",
            whitelist: ["PawnKindDef", "ThingDef"],
            blacklist: ["TrashDef"]);

        var formatted = LegacyExtractionRuleParser.Format(rule);
        var roundTripped = LegacyExtractionRuleParser.Parse(formatted);

        roundTripped.Tag.Should().Be(rule.Tag);
        roundTripped.Whitelist.Should().BeEquivalentTo(rule.Whitelist);
        roundTripped.Blacklist.Should().BeEquivalentTo(rule.Blacklist);
    }
}
