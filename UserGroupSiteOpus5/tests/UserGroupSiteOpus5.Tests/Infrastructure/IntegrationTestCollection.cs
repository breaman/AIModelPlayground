namespace UserGroupSiteOpus5.Tests.Infrastructure;

/// <summary>
/// Groups the tests that boot the application through <c>WebApplicationFactory</c> so they never
/// start a host at the same time.
/// </summary>
/// <remarks>
/// <c>HostFactoryResolver</c> captures the host the entry point builds through process-wide static
/// state. Two factories starting concurrently race on it, and the loser fails with "The entry
/// point exited without ever building an IHost" — which presents as a different integration test
/// failing on each run. Sharing a collection serialises them; the rest of the suite still runs in
/// parallel.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public class IntegrationTestCollection
{
    /// <summary>The collection name to place on each host-booting test class.</summary>
    public const string Name = "Integration";
}
