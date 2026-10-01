using Microsoft.EntityFrameworkCore;
using SpatialDispatch.Data;
using SpatialDispatch.Data.Entities;
using SpatialDispatch.Shared.Demo;

namespace SpatialDispatch.Tests;

/// <summary>
///     Covers the reseed-on-drift path: a database that no longer holds exactly the contract's row counts
///     must come back matching the contract on the next seed, not be left as-is.
/// </summary>
/// <remarks>
///     <see cref="SqlServerDispatchFixture" /> only ever seeds a freshly dropped database, so nothing else
///     exercises the branch in <see cref="DemoDataSeeder.SeedAsync" /> that fires when a row count is
///     already wrong. This test mutates one table's count itself. Which row it removes does not matter: a
///     mismatch anywhere makes <c>SeedAsync</c> delete and reinsert all three tables, so the database ends
///     the test bit-identical to the one every other test in this collection expects.
/// </remarks>
[Collection(SqlServerCollection.Name)]
public class DemoDataSeederTests(SqlServerDispatchFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Seeding_again_repairs_a_technician_count_that_no_longer_matches_the_contract()
    {
        Assert.SkipUnless(fixture.IsAvailable, SqlServerDispatchFixture.SkipReason);

        Technician technician = await fixture.Db.Technicians.SingleAsync(t => t.Id == 201, Ct);
        fixture.Db.Technicians.Remove(technician);
        await fixture.Db.SaveChangesAsync(Ct);

        Assert.Equal(DemoContract.Technicians.Count - 1, await fixture.Db.Technicians.CountAsync(Ct));

        // SeedAsync deletes with ExecuteDeleteAsync, which bypasses the change tracker, and then adds new
        // instances carrying the same primary keys the tracker may still hold from this or an earlier test
        // in the shared collection. Production always seeds on a freshly scoped context that has never
        // tracked anything; this shared test context is not one, so it is cleared to match.
        fixture.Db.ChangeTracker.Clear();

        await DemoDataSeeder.SeedAsync(fixture.Db, Ct);

        Assert.Equal(DemoContract.Territories.Count, await fixture.Db.Territories.CountAsync(Ct));
        Assert.Equal(DemoContract.Technicians.Count, await fixture.Db.Technicians.CountAsync(Ct));
        Assert.Equal(DemoContract.Jobs.Count, await fixture.Db.Jobs.CountAsync(Ct));

        // Restored, not merely recounted: the removed technician's row is back so the rest of this
        // collection sees the same database the fixture originally seeded.
        Technician restored = await fixture.Db.Technicians.SingleAsync(t => t.Id == 201, Ct);
        Assert.Equal("Morgan Lee", restored.Name);
    }
}
