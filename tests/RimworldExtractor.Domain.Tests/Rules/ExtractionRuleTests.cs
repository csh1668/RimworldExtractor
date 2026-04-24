using FluentAssertions;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Domain.Tests.Rules;

public class ExtractionRuleTests
{
    [Fact]
    public void TagOnly_CanExtract_AnyDefName()
    {
        var rule = new ExtractionRule("label");

        rule.CanExtract("Anything").Should().BeTrue();
    }

    [Fact]
    public void WithWhitelist_CanExtract_OnlyListedDefs()
    {
        var rule = new ExtractionRule("label", whitelist: ["ThingDef"]);

        rule.CanExtract("ThingDef").Should().BeTrue();
        rule.CanExtract("PawnKindDef").Should().BeFalse();
    }

    [Fact]
    public void WithBlacklist_CanExtract_AllExceptListed()
    {
        var rule = new ExtractionRule("label", blacklist: ["JunkDef"]);

        rule.CanExtract("ThingDef").Should().BeTrue();
        rule.CanExtract("JunkDef").Should().BeFalse();
    }

    [Fact]
    public void WithWhitelistAndBlacklist_BothConstraintsApply()
    {
        var rule = new ExtractionRule(
            "label",
            whitelist: ["ThingDef", "PawnKindDef"],
            blacklist: ["ThingDef"]);

        rule.CanExtract("ThingDef").Should().BeFalse("blacklist wins");
        rule.CanExtract("PawnKindDef").Should().BeTrue();
        rule.CanExtract("BuildingDef").Should().BeFalse("not in whitelist");
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = new ExtractionRule("label", whitelist: ["ThingDef"]);
        var b = new ExtractionRule("label", whitelist: ["ThingDef"]);

        a.Should().Be(b);
    }
}
