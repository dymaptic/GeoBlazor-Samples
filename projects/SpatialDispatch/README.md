# Spatial Dispatch

A field-service dispatch board for a fictional company in the Scranton–Wilkes-Barre area, built with
[GeoBlazor](https://www.geoblazor.com) Pro and SQL Server spatial data. It answers one question: which
service territory covers this customer, and which available technician assigned there is closest?

The sample's claim is that a spatial value is just a data type. A territory boundary and a job location sit
as `geography` columns beside ordinary text and boolean columns, and the containment and distance queries
are ordinary Entity Framework Core LINQ.

## Projects

| Project | Purpose |
|---|---|
| `SpatialDispatch` | ASP.NET Core host: Minimal API endpoints, static assets, and the Blazor WebAssembly host |
| `SpatialDispatch.Client` | Blazor WebAssembly dashboard, the GeoBlazor map, and the GeoJSON layer files |
| `SpatialDispatch.Shared` | Business contracts, service interfaces, the demo records, and the API-backed providers |
| `SpatialDispatch.Data` | Entity Framework Core model, the SQL Server spatial queries, seeding, and the GeoJSON layer shape |
| `SpatialDispatch.Tests` | xUnit v3 and bUnit tests for the contracts, the spatial queries, and the dashboard |

## Requirements

- .NET 10 SDK
- Docker, for the SQL Server container in `compose.yaml`
- A GeoBlazor Pro license key

## Configuring the connection string

The connection string is not committed in a file the host loads as configuration. The committed
`appsettings.Development.json.template` carries the same throwaway container credential that `compose.yaml`
documents, while the host project's `appsettings.Development.json`, which is gitignored, is where each
machine points at its own SQL Server. Copy the template first:

```bash
cp projects/SpatialDispatch/SpatialDispatch/appsettings.Development.json.template \
   projects/SpatialDispatch/SpatialDispatch/appsettings.Development.json
```

The copied value matches `compose.yaml`: `Server=localhost,1433` with the container's `sa` credential.
Change the port when something else already holds 1433, such as a different SQL Server container on the same
machine, and point it at the port that container publishes. The example below targets an already-running
container that publishes 14330; to use this sample's `compose.yaml` instead, change its `ports` entry to
`"14330:1433"` as well:

```json
{
  "ConnectionStrings": {
    "DispatchDatabase": "Server=127.0.0.1,14330;Database=SpatialDispatch;User Id=sa;Password=Dispatch!2026demo;Encrypt=True;TrustServerCertificate=True"
  }
}
```

Write the port into the server name. On Windows, a bare `localhost` with no port reaches a native SQL Server
on the same machine over shared memory instead of the container, and the container's password then fails as
a login error rather than a connection error.

`appsettings.Development.json` is read only in the Development environment, which the `http` and `https`
launch profiles set. A host outside Development supplies the connection string through the
`ConnectionStrings__DispatchDatabase` environment variable instead. With neither in place, the host stops at
startup with a message naming both routes. This file is on the host, never under the client's `wwwroot`, so
the credential is not downloaded to the browser the way the Pro key is.

## Configuring the Pro key

The map is drawn by GeoBlazor Pro, which needs a license key. No key is committed here. Copy the template
and paste your key into the copy:

```bash
cp projects/SpatialDispatch/SpatialDispatch.Client/wwwroot/appsettings.Development.json.template \
   projects/SpatialDispatch/SpatialDispatch.Client/wwwroot/appsettings.Development.json
```

`appsettings.Development.json` is gitignored. Set `GeoBlazor:LicenseKey` in it.

A free Core registration key does not work here. GeoBlazor accepts a Core key only in a project that
references `dymaptic.GeoBlazor.Core` directly, and this one references `dymaptic.GeoBlazor.Pro`, which brings
Core in transitively. With no key, the map panel shows GeoBlazor's own message asking for one, and
everything beside it still works: the dispatch answer, the candidate list, and assignment.

The client is WebAssembly, so everything under its `wwwroot` is downloaded by the browser. Keep the key in
the gitignored file rather than in a committed one. The client reads `appsettings.Development.json` only in
the Development environment, which the server's default profile sets.

## Optional: road routing

Routing is the one part of the application that needs a network credential, and it runs after the database
has already chosen the technician, so it never changes the recommendation. Add an ArcGIS API key beside the
license key:

```json
{
  "GeoBlazor": { "LicenseKey": "YOUR_PRO_LICENSE_KEY" },
  "ArcGISApiKey": "YOUR_ARCGIS_API_KEY"
}
```

`ArcGISApiKey` is GeoBlazor's own top-level convention, so the map and the route service read the same key.
Treat it as public: this is WebAssembly, so the browser can read it. Restrict it to the routing privilege
and to approved origins. Without both keys the map draws a dashed straight connector instead, and the
recommendation is identical. Every routing failure degrades to that same line.

## Running

Copy the connection-string template above first, since the host stops at startup without it, then start the
database and the app:

```bash
cd projects/SpatialDispatch
docker compose up -d
dotnet run --project SpatialDispatch
```

Open the URL the command prints (`http://localhost:5086` under the `http` profile). Startup waits for SQL
Server, applies the migration, and seeds the records, so a cold clone needs no manual database preparation.
It needs anywhere from ten seconds to a minute for the container to accept connections, and startup polls
for up to two minutes before it applies the migration.

There is no named volume, so `docker compose down` discards the data and the next `up` reseeds it. That is
deliberate: the database cannot drift from the compiled demo records across runs.

### Optional: Windows LocalDB instead of Docker

On Windows you can skip the container and use SQL Server LocalDB, which ships with Visual Studio.

```powershell
sqllocaldb create MSSQLLocalDB   # once, if the instance does not already exist
sqllocaldb start MSSQLLocalDB
$env:ConnectionStrings__DispatchDatabase = "Server=(localdb)\MSSQLLocalDB;Database=SpatialDispatch;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
dotnet run --project projects/SpatialDispatch/SpatialDispatch
```

The environment variable overrides the connection string in `appsettings.Development.json` for that shell
only. To reseed, drop the database and run again:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE SpatialDispatch"
```

## What you should see

1. **Open jobs** lists three jobs, with the urgent one, Summit Sports Catering, at the top.
2. Selecting it highlights North Valley on the map and recommends **Jordan Alvarez** at 5.7 km.
3. The candidate list shows why the two nearer technicians lost: Morgan Lee is unavailable and Riley Chen
   is assigned to South Valley.
4. A dashed connector is drawn from Jordan to the customer. That is the no-credentials routing fallback; a
   solid line is a real road route. Neither changes the recommendation.
5. **Assign Jordan** moves the job from Open to Assigned. Reloading the page restores it to Open, because
   assignment is presentation state in the browser and is never persisted.

## Running the tests

```bash
dotnet run --project projects/SpatialDispatch/SpatialDispatch.Tests
```

The suite covers the demo contract's acceptance checks against both providers, the spatial indexes and the
clustered primary key they depend on, the GeoJSON layer shape, and the dashboard's visible behavior:
selection, recommendation display, assignment, refresh-reset, and the loading, empty, error, and
routing-fallback states.

The spatial query tests need SQL Server, because the point is that `STContains` and `STDistance` run inside
the database. When the container is not reachable they skip with a message rather than failing, so the rest
of the suite still runs on a machine without Docker.

## Notes for anyone reading the code

- **Coordinates are longitude first, everywhere.** Well-Known Text, GeoJSON, and the `GeoPoint` record all
  take longitude before latitude.
- **GeoBlazor is confined to two files**: `Components/DispatchMap.razor` and
  `Services/ArcGisRouteService.cs`. Everything the map needs arrives as a parameter, which is what lets the
  dashboard tests stub it out, because bUnit cannot run the ArcGIS JavaScript interop.
- **The map's basemap is OpenStreetMap**, which needs no credentials, and `PromptForArcGISKey` is off.
- **Prerendering is on for the page and off for the map.** The board's answer is rendered on the server, so
  it appears while the WebAssembly bundle is still downloading, which is what removes the blank wait on a
  cold load. The map is held back until the browser has taken over, which is what keeps the ArcGIS view from
  initializing twice.
- **GeoBlazor Pro is pinned to the nuget.org release** (4.6.1), not a local build feed, so a cold clone
  restores on any machine.
- **Spatial values are `geography`, not `geometry`.** That is what makes `STDistance` return meters instead
  of degrees. Every stored value carries SRID 4326.
- **A `geography` operand with the wrong SRID makes `STContains` and `STDistance` return `NULL`** rather
  than failing, so the seeder rejects a boundary that did not come back from the parser as SRID 4326.
- **Territory polygons are wound counterclockwise.** SQL Server reads a reversed exterior ring as the
  complement, the whole globe minus the intended shape, and reports it as valid, so the wrong winding
  produces quietly wrong containment instead of an error.
- **NetTopologySuite is confined to `SpatialDispatch.Data`.** `SpatialDispatch.Shared` is downloaded to the
  browser, so territory boundaries live there as Well-Known Text strings and are parsed only while seeding.
- **`EnableRetryOnFailure` does not cover a container that is still starting.** A closed port 1433 arrives
  as SQL error 258, which is not on its transient-error list, so the first `MigrateAsync` throws.
  `DatabaseAvailability` polls the server before the migration, which is what makes the two commands above
  safe to run back to back.

## Origin

Adapted from the demo for Tim Purdum's TechBash 2026 session, "Spatial Data in .NET: From Database to
Dashboard."
