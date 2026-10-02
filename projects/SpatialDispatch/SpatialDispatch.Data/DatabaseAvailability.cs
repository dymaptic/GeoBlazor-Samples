using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace SpatialDispatch.Data;

/// <summary>
///     Waits for a SQL Server container to start accepting connections.
/// </summary>
/// <remarks>
///     <para>
///         <c>EnableRetryOnFailure</c> does not cover this, which is the trap. Its transient-error list
///         describes faults on a server that is already answering -- deadlocks, failovers, throttling. A
///         container that has not opened port 1433 yet surfaces as SQL error 258, "the wait operation timed
///         out", which is not on that list, so the first <c>MigrateAsync</c> throws instead of retrying.
///     </para>
///     <para>
///         Probing <c>master</c> first is what actually makes <c>docker compose up -d</c> followed
///         immediately by <c>dotnet run</c> work without the operator watching for a healthy container.
///     </para>
/// </remarks>
public static class DatabaseAvailability
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     Opens a connection to <c>master</c> on a short timeout, deliberately without any retry policy.
    /// </summary>
    /// <remarks>
    ///     <c>master</c> rather than the application database, because the application database does not
    ///     exist until the migration runs. A <c>CanConnectAsync</c> probe would report false on a perfectly
    ///     healthy server for that reason alone.
    /// </remarks>
    public static async Task<bool> CanReachServerAsync(
        string connectionString,
        int connectTimeoutSeconds = 3,
        CancellationToken cancellationToken = default)
    {
        (bool reachable, _) = await TryReachServerAsync(connectionString, connectTimeoutSeconds, cancellationToken);

        return reachable;
    }

    /// <summary>
    ///     Polls the server until it answers, or throws once <paramref name="timeout" /> has elapsed.
    /// </summary>
    /// <remarks>
    ///     Every attempt's <see cref="SqlException" /> is kept, not just discarded, so the timeout message can
    ///     name the actual failure. A server that never opens the port and one that opens it but rejects the
    ///     configured login both look identical from the caller's side without this: "start the container"
    ///     is the wrong advice for the second case, which is what a cold clone with a local SQL Express
    ///     instance already holding port 1433 hits.
    /// </remarks>
    public static async Task WaitForServerAsync(
        string connectionString,
        TimeSpan timeout,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
        bool announced = false;
        SqlException? lastError = null;

        while (true)
        {
            (bool reachable, SqlException? error) =
                await TryReachServerAsync(connectionString, connectTimeoutSeconds: 3, cancellationToken);

            if (reachable)
            {
                return;
            }

            lastError = error;

            if (DateTimeOffset.UtcNow >= deadline)
            {
                string detail = lastError is null
                    ? "No connection attempt completed before the timeout elapsed."
                    : $"The last attempt failed with SQL error {lastError.Number}: {lastError.Message}";

                throw new InvalidOperationException(
                    $"SQL Server did not accept connections within {timeout.TotalSeconds:0} seconds. {detail} "
                    + "If the container is not running, start it with `docker compose up -d` and check "
                    + "`docker compose ps`. If it is running, the connection string's credentials do not "
                    + "match this server, which happens when another local SQL instance already owns port "
                    + "1433.");
            }

            if (!announced)
            {
                logger?.LogInformation(
                    "Waiting for SQL Server to accept connections. A freshly started container takes a few "
                    + "seconds to tens of seconds.");

                announced = true;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

    /// <summary>
    ///     Opens the probe connection and reports the <see cref="SqlException" /> on failure, so a caller can
    ///     tell a closed port from a rejected login instead of just a boolean.
    /// </summary>
    private static async Task<(bool Reachable, SqlException? Error)> TryReachServerAsync(
        string connectionString,
        int connectTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        SqlConnectionStringBuilder builder = new(connectionString)
        {
            ConnectTimeout = connectTimeoutSeconds,
            InitialCatalog = "master"
        };

        try
        {
            await using SqlConnection connection = new(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            return (true, null);
        }
        catch (SqlException ex)
        {
            return (false, ex);
        }
    }
}
