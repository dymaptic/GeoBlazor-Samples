using Microsoft.EntityFrameworkCore;

namespace SpatialDispatch.Tests;

/// <summary>
///     Checks that the spatial indexes exist and that the prerequisite they quietly depend on holds.
/// </summary>
/// <remarks>
///     The clustered primary key is the part worth a test rather than a comment. A spatial index cannot be
///     created on a table whose primary key is nonclustered, and the failure arrives as error 1908 naming
///     the constraint, which reads like a key problem rather than an index problem. EF Core's default gives
///     us a clustered key today; a later <c>IsClustered(false)</c> on either indexed table, Territories or
///     Technicians, would break the migration, and this is where that shows up. Jobs carries no spatial
///     index, so its clustered key is checked here only for completeness.
/// </remarks>
[Collection(SqlServerCollection.Name)]
public class SpatialIndexTests(SqlServerDispatchFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Both_searched_geography_columns_have_a_spatial_index()
    {
        SkipIfUnavailable();

        List<string> spatialIndexes = await fixture.Db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sys.indexes WHERE type_desc = 'SPATIAL'")
            .OrderBy(name => name)
            .ToListAsync(Ct);

        // Jobs.Location is absent on purpose: it is the literal operand passed to STContains and
        // STDistance, never the column searched, so an index on it would be maintained and never read.
        Assert.Equal(["SIX_Technicians_Location", "SIX_Territories_Boundary"], spatialIndexes);
    }

    [Theory]
    [InlineData("Territories")]
    [InlineData("Technicians")]
    [InlineData("Jobs")]
    public async Task Every_primary_key_is_clustered(string table)
    {
        SkipIfUnavailable();

        List<string> primaryKeyKind = await fixture.Db.Database
            .SqlQueryRaw<string>(
                """
                SELECT i.type_desc AS Value
                FROM sys.indexes i
                WHERE i.is_primary_key = 1 AND i.object_id = OBJECT_ID({0})
                """,
                table)
            .ToListAsync(Ct);

        Assert.Equal(["CLUSTERED"], primaryKeyKind);
    }

    private void SkipIfUnavailable()
    {
        Assert.SkipUnless(fixture.IsAvailable, SqlServerDispatchFixture.SkipReason);
    }
}
