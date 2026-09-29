# Fleet Dashboard

A real-time dispatcher dashboard built with [GeoBlazor](https://www.geoblazor.com) Core 4.6.2.
A Blazor WebAssembly client draws a simulated delivery fleet on an ArcGIS map while an ASP.NET Core
SignalR server streams position reports. The sample shows how to render a fast telemetry stream
without drowning the browser in redraws: retain the latest report per vehicle, hand the map one
batch per cadence tick, and give every visual its own clock.

All vehicle data is simulated. There is no live vehicle feed and no external telemetry dependency.

## Projects

| Project | Purpose |
|---|---|
| `FleetDashboard.Client` | Blazor WebAssembly dashboard: GeoBlazor map, vehicle list, speed chart |
| `FleetDashboard.Server` | ASP.NET Core host, fleet simulator, and SignalR hub |
| `FleetDashboard.Shared` | Telemetry contracts, scenario settings, and embedded route geometry |
| `FleetDashboard.Tests` | MSTest and bUnit tests for the state, streaming, and dashboard components |

## Requirements

- .NET 10 SDK
- A free GeoBlazor registration key from [licensing.dymaptic.com](https://licensing.dymaptic.com)

GeoBlazor's `MapView` calls a license check during initialization and throws without a key, so the
map does not render until one is configured. No ArcGIS API key is needed for the default
OpenStreetMap basemap. Map assets and basemap tiles require network access.

## Configuring the key

`FleetDashboard.Client` is a WebAssembly app, so everything under its `wwwroot` is downloaded by the
browser and is public at runtime. Keys therefore live in a gitignored local file, never in a
committed one.

```bash
cp projects/FleetDashboard/FleetDashboard.Client/wwwroot/appsettings.Development.json.template \
   projects/FleetDashboard/FleetDashboard.Client/wwwroot/appsettings.Development.json
```

Paste your key into `GeoBlazor:RegistrationKey` in the copy, then rebuild. The server serves client
files from a build-time manifest, so a file created after the last build is not served until
`dotnet build` (or a plain `dotnet run`) runs again. Restarting an already running `--no-build`
server is not enough; the new file 404s and the client falls back to the "no registration key"
error.

The client only reads `appsettings.Development.json` when it is running in the Development
environment, which it learns from the `Blazor-Environment` response header that
`FleetDashboard.Server/Program.cs` sets on `/_framework` requests.

## Running

```bash
dotnet run --project projects/FleetDashboard/FleetDashboard.Server/FleetDashboard.Server.csproj
```

Then open <http://localhost:5068>. The server hosts the WebAssembly client; there is no separate
client process.

## Validating

```bash
dotnet build projects/FleetDashboard/FleetDashboard.slnx -c Release
dotnet test projects/FleetDashboard/FleetDashboard.Tests/FleetDashboard.Tests.csproj -c Release --no-build
```

## Pipeline modes

The dashboard chooses one of seven pipeline modes from a `mode` query-string value and defaults to
`ready`. `ready`, `scheduled`, and `resilient` capture a bounded latest report per vehicle and apply
collection map edits at a 250 ms cadence. `latest` and `batched` use the same bounded state at
100 ms, with `latest` issuing one edit per vehicle and `batched` issuing one collection edit.
`pipeline` and `baseline` instead keep every observation in a raw FIFO queue at 100 ms, with one
awaited edit per vehicle, and both modes share the safety stop described below. Only `baseline`
defaults to the stress workload; every other mode defaults to normal. Add a `preset` query parameter
(`normal`, `stress`, `quiet`, or `ceiling`) to override the mode default. An unrecognized `mode` or
`preset` falls back with a visible notice.

The raw-queue safety stop halts map updates when the queue reaches 20,000 pending records, shows
a red banner with the rejected-record count, and keeps telemetry flowing. Click **Resume map
updates** to clear it. The scenario strip reports records per second, pending count, map calls, edit
latency percentiles, failure count, latest map batch size, superseded state, and reconnect count.

Open **Display preview controls** below the map to pause truck 007, pause all reports, hide the fleet
locally, simulate a map failure, break the connection with a real server abort, or reset the
scenario. A report becomes stale after five seconds. Map failures keep telemetry visible and expose
a retry button that pauses after three consecutive failed batches. The connection break shows a
visible `Live -> Reconnecting -> Live` recovery in about one second and increments the reconnect
counter. Navigating away during an in-flight map edit cancels the page's update loop and the edit
itself, so no work continues against a component that no longer exists.

## The patterns it demonstrates

1. **Get a map on screen.** Register GeoBlazor, add a `MapView`, and put existing positions in a
   client-side `FeatureLayer` with a `GraphicsLayer` for static lines. Give the feature layer an
   `objectIdField` so a vehicle keeps one identity.
2. **Define the contract before the transport.** The frame carries a stable vehicle ID, a
   per-vehicle sequence, and an observation timestamp separate from receipt time. Make the frame
   a complete roster snapshot so a dropped frame cannot corrupt the picture.
3. **Receive promptly, retain little.** Keep one latest report per vehicle in a store keyed by
   vehicle ID. The store can never exceed the roster, no matter how fast reports arrive.
4. **Bound the server side too.** Hold a small number of frames and drop the oldest, which is safe
   only because each frame is complete.
5. **Draw on a clock, not on arrival.** Submit batches of edits on a fixed interval, await each
   batch, and never start a second edit while one is in flight. Give the map and the chart
   different intervals, and measure before calling any interval final.
6. **Show staleness from observation time.** Compare `ObservedAtUtc` against local elapsed time,
   never against frame arrival, and re-evaluate on a timer so a silent vehicle becomes visibly
   stale on its own.
7. **Make failures visible.** A failed edit stays on screen as an error and retries a bounded
   number of times. It never counts as displayed.
8. **Clean up on navigation.** Cancel the page's update loop and any in-flight edit when the
   component goes away. The frame source and its hub connection are scoped to the app and survive
   navigation.

Coalesce state that has a current value. Never coalesce events: a transaction or an alarm
transition has to be processed before anything is replaced.

## Route data

`data/routes.geojson` holds the four road-following paths the simulated fleet travels. The
simulator, the deterministic client source, and the map's route layer all read this one file, so a
drawn route and a vehicle's path can never disagree.

The geometry is from OpenStreetMap contributors, retrieved once through the public OSRM demo routing
server with `overview=full&geometries=geojson` and committed as the runtime artifact. It is offered
under the Open Data Commons Open Database License (ODbL). The paths are demonstration geometry, not
navigation directions. `data/prepare-routes.mjs` regenerates the file against OSRM, and
[data/README.md](data/README.md) records the endpoints, the license, and the validation rules.

`FleetDashboard.Shared.Routing.FleetRoutes` validates every feature before use: each route has an
`id`, a `name`, and at least two coordinate pairs; coordinates are longitude/latitude within valid
world ranges; and no single segment jumps more than 5 km, so a route cannot teleport across the map.

## Origin

Adapted from the demo for Tim Purdum's TechBash 2026 session, "Real-Time Data Visualization in
Blazor."
