using Microsoft.EntityFrameworkCore;
using SpatialDispatch.Data;

namespace SpatialDispatch.Tests;

/// <summary>
///     A migrated, seeded SQL Server database for the spatial query tests, or a clear reason why there
///     isn't one.
/// </summary>
/// <remarks>
///     These tests need the real database engine: the whole point is that <c>STContains</c> and
///     <c>STDistance</c> run in SQL Server, so an in-memory or SQLite substitute would test nothing. That
///     makes them the one part of the suite with an external prerequisite, so when the container is not
///     running they skip with an explanation rather than failing. A cold clone can still run
///     <c>dotnet run --project tests/SpatialDispatch.Tests</c> and get a green result covering everything
///     else.
/// </remarks>
public sealed class SqlServerDispatchFixture : IAsyncLifetime
{
    /// <summary>A database of its own, so running the tests never disturbs the one used on stage.</summary>
    private const string DefaultConnectionString =
        "Server=localhost,1433;Database=SpatialDispatch_Tests;User Id=sa;Password=Dispatch!2026demo;"
        + "Encrypt=True;TrustServerCertificate=True";

    public const string SkipReason =
        "SQL Server is not reachable. Start it with `docker compose up -d` to run the spatial query tests.";

    private DispatchDbContext? _db;

    /// <summary>False when the database could not be reached, which makes every dependent test skip.</summary>
    public bool IsAvailable { get; private set; }

    public DispatchDbContext Db =>
        _db ?? throw new InvalidOperationException("The database is unavailable; the test should have skipped.");

    public async ValueTask InitializeAsync()
    {
        string connectionString =
            Environment.GetEnvironmentVariable("SPATIALDISPATCH_TEST_CONNECTION") ?? DefaultConnectionString;

        // A short probe deliberately without the retry-and-wait the application uses: a developer with no
        // container running should learn that in a couple of seconds, not after a two-minute wait.
        if (!await DatabaseAvailability.CanReachServerAsync(connectionString))
        {
            return;
        }

        DbContextOptions<DispatchDbContext> options = new DbContextOptionsBuilder<DispatchDbContext>()
            .UseSqlServer(connectionString, sqlServer => sqlServer.UseNetTopologySuite())
            .Options;

        _db = new DispatchDbContext(options);

        // Dropped first so a schema or seed change in the demo contract cannot leave a stale row behind and
        // quietly invalidate the acceptance checks below.
        await _db.Database.EnsureDeletedAsync();
        await _db.Database.MigrateAsync();
        await DemoDataSeeder.SeedAsync(_db);

        IsAvailable = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_db is not null)
        {
            await _db.DisposeAsync();
        }
    }
}
