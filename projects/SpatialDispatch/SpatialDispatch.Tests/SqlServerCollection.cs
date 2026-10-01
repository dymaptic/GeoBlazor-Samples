namespace SpatialDispatch.Tests;

/// <summary>
///     Groups every test class that needs the real database into one xUnit collection.
/// </summary>
/// <remarks>
///     This has to be a collection fixture rather than a class fixture. <c>IClassFixture</c> builds a
///     separate instance per test class, and <see cref="SqlServerDispatchFixture" /> drops and recreates
///     <c>SpatialDispatch_Tests</c> on the way in, so a second class using it in parallel deleted the
///     database out from under the first one: "the database does not exist, or the database is not in a
///     state that allows access checks." A collection fixture is created once and shared, and xUnit does
///     not run the classes in one collection concurrently.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerDispatchFixture>
{
    public const string Name = "SQL Server";
}
