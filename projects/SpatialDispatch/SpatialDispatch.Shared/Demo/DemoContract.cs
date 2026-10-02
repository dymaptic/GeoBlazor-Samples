using SpatialDispatch.Shared.Contracts;

namespace SpatialDispatch.Shared.Demo;

/// <summary>
///     The seed records from <c>docs/demo-contract.md</c>, in one place.
/// </summary>
/// <remarks>
///     Task 2 serves the dashboard from these records directly. Task 3 uses the same records to seed SQL
///     Server, so the demo contract has exactly one representation in code and the presentation cannot drift
///     from what the document promises. Every coordinate is longitude-first WGS 84 (SRID 4326).
/// </remarks>
public static class DemoContract
{
    /// <summary>WGS 84. Every stored coordinate uses it, and geography distances then come back in meters.</summary>
    public const int Srid = 4326;

    public const int CanonicalJobId = 1001;
    public const int NorthValleyId = 1;
    public const int SouthValleyId = 2;
    public const int RecommendedTechnicianId = 203;

    // The smallest rectangle containing both territory boundaries. SQL Server geography measures its
    // southwest-to-northeast diagonal at 45,268.94 m, rounded up so every location inside remains eligible.
    public const double CompanyExtentWest = -75.920000;
    public const double CompanyExtentSouth = 41.180000;
    public const double CompanyExtentEast = -75.585000;
    public const double CompanyExtentNorth = 41.500000;
    public const double CompanyExtentDiagonalMeters = 45_269d;

    public static readonly Territory NorthValley = new(NorthValleyId, "North Valley");
    public static readonly Territory SouthValley = new(SouthValleyId, "South Valley");

    public static IReadOnlyList<Territory> Territories { get; } = [NorthValley, SouthValley];

    /// <summary>
    ///     Territory boundaries as Well-Known Text, exactly as recorded in <c>docs/demo-contract.md</c>.
    /// </summary>
    /// <remarks>
    ///     These are strings rather than geometry objects because this assembly is downloaded to the
    ///     browser, and the Blazor WebAssembly client has no reason to carry NetTopologySuite. The data
    ///     project parses them once while seeding. Vertex order follows SQL Server's left-hand rule, so the
    ///     small local area is the polygon interior rather than the rest of the globe.
    /// </remarks>
    public static IReadOnlyDictionary<int, string> BoundaryWktByTerritoryId { get; } =
        new Dictionary<int, string>
        {
            [NorthValleyId] =
                "POLYGON ((-75.830000 41.352000, -75.610000 41.352000, -75.585000 41.500000, " +
                "-75.835000 41.500000, -75.830000 41.352000))",
            [SouthValleyId] =
                "POLYGON ((-75.920000 41.180000, -75.650000 41.180000, -75.610000 41.352000, " +
                "-75.830000 41.352000, -75.920000 41.180000))"
        };

    /// <summary>
    ///     The dispatcher's open jobs. Job 1001 is the one the talk opens and closes on; the two routine
    ///     jobs exist so the priority ordering in the list means something and so one selection lands in
    ///     each territory.
    /// </summary>
    public static IReadOnlyList<Job> Jobs { get; } =
    [
        new Job(
            CanonicalJobId,
            "Summit Sports Catering",
            "Walk-in freezer temperature alarm",
            JobPriority.Urgent,
            "PNC Field, Moosic, Pennsylvania",
            new GeoPoint(-75.683967, 41.360406)),
        new Job(
            1002,
            "Anthracite Brewing Co.",
            "Glycol chiller running warm",
            JobPriority.Routine,
            "Wilkes-Barre, Pennsylvania",
            new GeoPoint(-75.860000, 41.245000)),
        new Job(
            1003,
            "Lackawanna Print Works",
            "Rooftop unit short-cycling",
            JobPriority.Routine,
            "Scranton, Pennsylvania",
            new GeoPoint(-75.660000, 41.400000))
    ];

    /// <summary>
    ///     The territory whose boundary contains each job. Task 3 replaces this lookup with a translated
    ///     <c>STContains</c> query; the shape of the answer does not change.
    /// </summary>
    public static IReadOnlyDictionary<int, int> ContainingTerritoryByJobId { get; } =
        new Dictionary<int, int>
        {
            [CanonicalJobId] = NorthValleyId,
            [1002] = SouthValleyId,
            [1003] = NorthValleyId
        };

    /// <summary>
    ///     The technician roster. Every location lies inside that technician's assigned territory.
    /// </summary>
    /// <remarks>
    ///     Technicians 205 to 209 fill out the map so both territories look staffed and the ranking has
    ///     something to rank. All five are farther from job 1001 than Casey Patel, which is what keeps the
    ///     closing demo's candidate list opening with the same four names and the same four outcomes.
    /// </remarks>
    public static IReadOnlyList<DemoTechnician> Technicians { get; } =
    [
        new DemoTechnician(201, "Morgan Lee", NorthValleyId, IsAvailable: false,
            "Montage service stop", new GeoPoint(-75.680500, 41.361200)),
        new DemoTechnician(202, "Riley Chen", SouthValleyId, IsAvailable: true,
            "Moosic service stop", new GeoPoint(-75.687000, 41.350500)),
        new DemoTechnician(203, "Jordan Alvarez", NorthValleyId, IsAvailable: true,
            "Steamtown service stop", new GeoPoint(-75.671329, 41.410730)),
        new DemoTechnician(204, "Casey Patel", NorthValleyId, IsAvailable: true,
            "Dickson City service stop", new GeoPoint(-75.625401, 41.465984)),
        new DemoTechnician(205, "Avery Brooks", NorthValleyId, IsAvailable: true,
            "Archbald service stop", new GeoPoint(-75.610000, 41.487000)),
        new DemoTechnician(206, "Devon Ortiz", NorthValleyId, IsAvailable: true,
            "Clarks Summit service stop", new GeoPoint(-75.790000, 41.462000)),
        new DemoTechnician(207, "Harper Nguyen", SouthValleyId, IsAvailable: true,
            "Wilkes-Barre service stop", new GeoPoint(-75.880000, 41.230000)),
        new DemoTechnician(208, "Sam Okafor", SouthValleyId, IsAvailable: true,
            "Route 115 service stop", new GeoPoint(-75.700000, 41.220000)),
        new DemoTechnician(209, "Quinn Delgado", SouthValleyId, IsAvailable: false,
            "Bear Creek service stop", new GeoPoint(-75.760000, 41.205000))
    ];

    /// <summary>
    ///     Reference geodesic distances in meters from each job to every technician, rounded to the nearest
    ///     meter as recorded in the demo contract. The outer key is the job, the inner key the technician.
    /// </summary>
    /// <remarks>
    ///     Every value here came from <c>geography::STDistance</c> on SQL Server rather than from a formula
    ///     in this repository, which is why the in-memory provider and the database agree on the ranking.
    ///     SQL Server documents that function as accurate to within 0.25 percent, so tests assert a
    ///     tolerance rather than exact equality.
    /// </remarks>
    public static IReadOnlyDictionary<int, IReadOnlyDictionary<int, double>> ReferenceDistanceMeters { get; } =
        new Dictionary<int, IReadOnlyDictionary<int, double>>
        {
            [CanonicalJobId] = new Dictionary<int, double>
            {
                [201] = 303,
                [202] = 1_129,
                [203] = 5_688,
                [204] = 12_707,
                [205] = 15_359,
                [206] = 14_349,
                [207] = 21_894,
                [208] = 15_651,
                [209] = 18_397
            },
            [1002] = new Dictionary<int, double>
            {
                [201] = 19_812,
                [202] = 18_634,
                [203] = 24_254,
                [204] = 31_428,
                [205] = 34_057,
                [206] = 24_802,
                [207] = 2_364,
                [208] = 13_699,
                [209] = 9_489
            },
            [1003] = new Dictionary<int, double>
            {
                [201] = 4_638,
                [202] = 5_943,
                [203] = 1_522,
                [204] = 7_878,
                [205] = 10_527,
                [206] = 12_864,
                [207] = 26_378,
                [208] = 20_269,
                [209] = 23_220
            }
        };

    public static double ReferenceDistance(int jobId, int technicianId) =>
        ReferenceDistanceMeters[jobId][technicianId];

    public static Territory TerritoryById(int id) =>
        Territories.Single(t => t.Id == id);

    /// <summary>A technician as stored, before any one job turns them into a ranked candidate.</summary>
    public sealed record DemoTechnician(
        int Id,
        string Name,
        int TerritoryId,
        bool IsAvailable,
        string LocationLabel,
        GeoPoint Location);
}
