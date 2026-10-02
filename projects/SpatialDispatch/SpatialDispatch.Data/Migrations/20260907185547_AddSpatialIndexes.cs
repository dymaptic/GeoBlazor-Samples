using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpatialDispatch.Data.Migrations
{
    /// <summary>
    ///     Adds spatial indexes to the two geography columns the dispatch queries search by.
    /// </summary>
    /// <remarks>
    ///     EF Core has no model-level API for a spatial index, so this migration is hand-written SQL and the
    ///     model snapshot is unchanged. Two prerequisites are worth knowing before running it:
    ///     <list type="bullet">
    ///         <item>
    ///             The table must have a clustered primary key. Both tables below get one from EF Core's
    ///             default, which is why nothing here creates it, but a table whose primary key is
    ///             nonclustered fails with error 12008, whose message names the table and the missing
    ///             clustered primary key rather than the spatial index.
    ///         </item>
    ///         <item>
    ///             <c>GEOGRAPHY_AUTO_GRID</c> lets SQL Server pick the grid densities. The older
    ///             <c>GEOGRAPHY_GRID</c> takes a four-level <c>GRIDS</c> clause, and tuning it by hand is
    ///             not worth doing before a query is known to be slow.
    ///         </item>
    ///     </list>
    ///     <c>Jobs.Location</c> is deliberately left unindexed: it is always the literal operand handed to
    ///     <c>STContains</c> or <c>STDistance</c>, never the column being searched, so an index on it would
    ///     be maintained and never read.
    /// </remarks>
    public partial class AddSpatialIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Read by the containment query, which materializes all matches before enforcing uniqueness.
            migrationBuilder.Sql(
                """
                CREATE SPATIAL INDEX SIX_Territories_Boundary
                    ON Territories(Boundary)
                    USING GEOGRAPHY_AUTO_GRID;
                """);

            // Read by the bounded nearest-neighbor query, which uses STDistance in WHERE and ORDER BY.
            migrationBuilder.Sql(
                """
                CREATE SPATIAL INDEX SIX_Technicians_Location
                    ON Technicians(Location)
                    USING GEOGRAPHY_AUTO_GRID;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX SIX_Technicians_Location ON Technicians;");
            migrationBuilder.Sql("DROP INDEX SIX_Territories_Boundary ON Territories;");
        }
    }
}
