using FluentAssertions;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Infrastructure.Output;

namespace RimworldExtractor.Infrastructure.Tests.Output;

public class PolicyBasedConflictResolverTests
{
    [Theory]
    [InlineData(DuplicatesPolicy.Overwrite, ConflictDecision.Overwrite)]
    [InlineData(DuplicatesPolicy.KeepOriginal, ConflictDecision.KeepOriginal)]
    [InlineData(DuplicatesPolicy.Stop, ConflictDecision.Abort)]
    public async Task ResolveAsync_MapsPolicyToDecision(DuplicatesPolicy policy, ConflictDecision expected)
    {
        var resolver = new PolicyBasedConflictResolver(policy);

        var decision = await resolver.ResolveAsync(
            new ConflictContext("/some/file.xml", "XML"),
            TestContext.Current.CancellationToken);

        decision.Should().Be(expected);
    }
}
