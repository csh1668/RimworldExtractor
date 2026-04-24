using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RimworldExtractor.Application.ModDiscovery;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Application.Tests.ModDiscovery;

public class ModDiscoveryServiceTests
{
    private static ModMetadata MakeMod(string name) =>
        new("/mods/" + name, "id-" + name, name, "author." + name, false);

    [Fact]
    public void DiscoverAll_SecondCallWithoutRefresh_ReturnsSameInstance()
    {
        var modLister = Substitute.For<IModLister>();
        modLister.DiscoverAll().Returns(
            new[] { MakeMod("Alpha"), MakeMod("Beta") },
            new[] { MakeMod("Gamma") });   // second call would return different list

        var service = new ModDiscoveryService(modLister, NullLogger<ModDiscoveryService>.Instance);

        var first = service.DiscoverAll();
        var second = service.DiscoverAll();

        second.Should().BeSameAs(first, "cache should be returned on the second call");
        modLister.Received(1).DiscoverAll();
    }

    [Fact]
    public void DiscoverAll_WithRefresh_InvalidatesCache()
    {
        var modLister = Substitute.For<IModLister>();
        var firstList = new[] { MakeMod("Alpha") };
        var secondList = new[] { MakeMod("Beta") };
        modLister.DiscoverAll().Returns(firstList, secondList);

        var service = new ModDiscoveryService(modLister, NullLogger<ModDiscoveryService>.Instance);

        var first = service.DiscoverAll();
        var second = service.DiscoverAll(refresh: true);

        first.Should().BeSameAs(firstList);
        second.Should().BeSameAs(secondList, "refresh should bypass the cache and return fresh data");
        modLister.Received(2).DiscoverAll();
    }

    [Fact]
    public void Methods_DelegateToIModLister()
    {
        var modLister = Substitute.For<IModLister>();
        var mod = MakeMod("TestMod");
        var folders = new[] { new ExtractableFolder(mod, "Defs", null, "1.4") };
        var refs = new[] { MakeMod("Core") };

        modLister.ReadMetadata("/mods/TestMod").Returns(mod);
        modLister.GetExtractableFolders(mod).Returns(folders);
        modLister.FindReferenceMods(mod).Returns(refs);

        var service = new ModDiscoveryService(modLister, NullLogger<ModDiscoveryService>.Instance);

        service.ReadMetadata("/mods/TestMod").Should().Be(mod);
        service.GetExtractableFolders(mod).Should().BeEquivalentTo(folders);
        service.FindReferenceMods(mod).Should().BeEquivalentTo(refs);
    }
}
