using FluentAssertions;
using RimworldExtractor.Domain.Mods;

namespace RimworldExtractor.Domain.Tests.Mods;

public class RequiredModsTests
{
    [Fact]
    public void Empty_HasNoAllowedOrDisallowed()
    {
        var empty = RequiredMods.Empty;

        empty.Allowed.Should().BeEmpty();
        empty.Disallowed.Should().BeEmpty();
        empty.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void WithOneAllowedGroup_StoresIt()
    {
        var mods = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("a.b") } },
            disallowed: Array.Empty<ModReference[]>());

        mods.Allowed.Should().HaveCount(1);
        mods.Allowed[0].Should().ContainSingle();
        mods.Allowed[0][0].Value.Should().Be("a.b");
        mods.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Combine_WithNull_ReturnsSelf()
    {
        var self = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("a.b") } },
            disallowed: Array.Empty<ModReference[]>());

        self.Combine(null).Should().BeSameAs(self);
    }

    [Fact]
    public void Combine_UnionsAllowedAndDisallowed()
    {
        var a = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("a.a") } },
            disallowed: Array.Empty<ModReference[]>());
        var b = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("b.b") } },
            disallowed: new[] { new[] { ModReference.ByPackageId("c.c") } });

        var merged = a.Combine(b);

        merged.Allowed.Should().HaveCount(2);
        merged.Disallowed.Should().HaveCount(1);
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("a.b") } },
            disallowed: Array.Empty<ModReference[]>());
        var b = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("a.b") } },
            disallowed: Array.Empty<ModReference[]>());

        a.Should().Be(b);
    }
}
