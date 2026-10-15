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
environment. Since .NET 10 that environment is fixed when the client is built, not read from the
`Blazor-Environment` response header: `dotnet build` and `dotnet run` produce a Development client,
and `dotnet publish` produces a Production one unless you pass
`-p:WasmApplicationEnvironmentName=Development` (see [Measuring performance](#measuring-performance)).

## Live layers

Under "Display preview controls", a "Live layers" group toggles three services that someone else
hosts and keeps current. They are the contrast to the fleet pipeline: each service publishes its own
stream, and the map only re-requests it on an interval, with no store, frames, coalescing, or batching.
All are off by default and draw below the routes and vehicles. Toggling one changes the layer's
visibility in place and does not touch the vehicle layer, selection, or the update loop.

| Layer | Service | Refresh |
|-------|---------|---------|
| NOAA radar | `radar_base_reflectivity` MapServer (`MapImageLayer`) | 5 minutes |
| NWS watches and warnings | `NWS_Watches_Warnings_v1` FeatureServer layer 6 (`FeatureLayer`) | 3 minutes |
| Esri traffic | `World/Traffic` MapServer (`MapImageLayer`), needs an ArcGIS API key | 5 minutes |

`RefreshInterval` is in minutes (the ArcGIS JS unit). Radar and warnings are public, so they are sent
with `ExcludeApiKey` and work without any key. Radar is empty where the US has no weather; zoom out to
see it.

The traffic toggle appears only when an ArcGIS API key is configured. GeoBlazor reads a top-level
`ArcGISApiKey` setting, so add it next to `GeoBlazor` in the gitignored
`appsettings.Development.json` (the template carries an empty placeholder) and rebuild:

```json
{ "ArcGISApiKey": "YOUR_ARCGIS_API_KEY", "GeoBlazor": { "RegistrationKey": "..." } }
```

Like the registration key, the API key is downloaded by the browser at runtime, so use a key
restricted to the traffic service and your demo origin. The layer definitions live in
`FleetDashboard.Client/Rendering/HostedLayers.cs`.

## Running

```bash
dotnet run --project projects/FleetDashboard/FleetDashboard.Server/FleetDashboard.Server.csproj
```

Then open <http://localhost:5068>. The server hosts the WebAssembly client; there is no separate
client process.

## Measuring performance

Measure a published build, not `dotnet run` or `dotnet build` output. A built (unpublished)
WebAssembly client runs with debugging enabled, and on the stress preset its map edits took about 30
times longer than the same code published: a `batched` edit median of about 2,500 ms against 84 ms.

```bash
dotnet publish projects/FleetDashboard/FleetDashboard.Server/FleetDashboard.Server.csproj -c Release \
  -o publish -p:WasmApplicationEnvironmentName=Development
cd publish
./FleetDashboard.Server --environment Development --urls http://localhost:5068
```

`-p:WasmApplicationEnvironmentName=Development` keeps the published client in the Development
environment so it still reads the registration key from `appsettings.Development.json`. Without it
the client boots as Production and the map reports a missing registration key.

To compare ahead-of-time compilation, add `-p:RunAOTCompilation=true` (it needs the `wasm-tools`
workload). On the stress preset it shortened a 500-truck edit from about 75 ms to about 54 ms, and
grew `dotnet.native.wasm` from 2.9 MB to 51 MB.

## Validating

```bash
dotnet build projects/FleetDashboard/FleetDashboard.slnx -c Release
dotnet test projects/FleetDashboard/FleetDashboard.Tests/FleetDashboard.Tests.csproj -c Release --no-build
```

## Pipeline modes

The Mode picker shows a display name followed by the URL token. Its guidance follows the selected
mode before **Apply and reload**; selecting a mode alone does not change the running pipeline. The
guidance columns in the table below copy that picker text; `FleetDashboard.Shared/ScenarioSettings.cs`
stays the source of truth.

The picker lists the modes in the order they are demonstrated, and the two raw-queue entries sit together
so the cap is the only difference between them. Four entries run the same pipeline: `ready`, `scheduled`,
`resilient`, and `viewport` all keep the bounded per-truck store with one `ApplyEdits` call per 250 ms
tick, and `viewport` adds the server-side extent filter described below. They differ in the load each
demonstration uses and in what the presenter does with them, not in that pipeline. Only `baseline`
defaults to the stress load.

| Display name | URL token (`mode`) | What you are watching | How to demonstrate it | Same pipeline as |
|---|---|---|---|---|
| Finished dashboard | `ready` | Normal load: one report per truck, one ApplyEdits call every 250 ms. Same pipeline as scheduled and resilient. | Open with no query string. Select Truck 007 and watch its marker step every 250 ms while Pending stays at 0. | `scheduled`, `resilient` |
| Raw queue | `baseline` | Every report queued in arrival order and drawn one at a time, with no per-tick cap. Under stress it reaches the 20,000 safety stop. | Open ?mode=baseline, which applies the stress load. Watch Pending climb past 500 until the red safety stop banner appears. | none |
| Raw queue with 250 record cap per tick | `pipeline` | The same raw queue, but the map is handed at most 250 records a tick, which keeps it drained at the normal load. | Open ?mode=pipeline at the normal load. Select Truck 007 and follow one report from Pending in the strip to its marker. | none |
| Keep the latest per truck | `latest` | One waiting report per truck, latest wins. One ApplyEdits call per changed truck, about 59 a second. | Open ?mode=latest&preset=stress. Watch Pending hold at or under 500, with map calls a second near 59. | none |
| Batched ApplyEdits calls | `batched` | The same per-truck state, but one ApplyEdits call carries every changed truck: about 7 calls a second. | Open ?mode=batched&preset=stress. Watch map calls a second drop from about 59 to about 7 with the same trucks moving. | none |
| Batched ApplyEdits with 250 ms tick | `scheduled` | The same batched ApplyEdits calls, on a 250 ms page tick instead of 100 ms: about 3.7 calls a second. | Open ?mode=scheduled&preset=stress. The tick lengthens from 100 ms to 250 ms, so watch map calls a second fall to about 3.7 with the motion unchanged. | `ready`, `resilient` |
| Filter by view extent | `viewport` | The server filters each frame by the map's view extent, plus the selected truck. The rest arrive once a second. | Open ?mode=viewport&preset=stress, select Truck 007, then stop panning so the extent settles. Watch trucks per frame fall toward 191 for the recorded run. | `ready`, `scheduled`, `resilient` (adds a server-side extent filter) |
| Failure scenarios | `resilient` | No pipeline change: this runs the same pipeline as ready and scheduled, with the failure controls at hand. | Open ?mode=resilient. Pause truck 007, and watch it go stale after 5 seconds, then break the connection and watch Reconnecting clear. | `ready`, `scheduled` |

The dashboard chooses one of eight pipeline modes from a `mode` query-string value and defaults to
`ready`. `ready`, `scheduled`, `resilient`, and `viewport` capture a bounded latest report per vehicle
and apply collection map edits at a 250 ms cadence. `latest` and `batched` use the same bounded state at
100 ms, with `latest` issuing one edit per vehicle and `batched` issuing one collection edit.
`pipeline` instead keeps every observation in a raw FIFO queue at 100 ms, with one awaited edit per
vehicle and a cap of 250 records per tick. `baseline` reads the same raw queue but applies one awaited
edit per report, in arrival order, with no per-tick cap. Both modes share the safety stop described
below. Only `baseline` defaults to the stress workload; every other mode defaults to normal. Add a
`preset` query parameter (`normal`, `stress`, `quiet`, or `ceiling`) to override the mode default. An
unrecognized `mode` or `preset` falls back with a visible notice.

`viewport` (implementation #5, "only send what the map can see") shrinks what the browser
receives rather than what the map draws. The client tells the server its map extent once the view
has held still for 300 ms, and which truck is selected. The server still produces one complete frame
per tick, but sends this connection only the vehicles inside the extent and the selected truck on
every frame. Every other vehicle rides along at most once per second, staggered across the second,
with its real observation time, so the vehicle list stays complete and nothing turns stale. Filtered
frames are marked partial and the client merges them onto the roster it holds. A connection's first
frame, and any frame after the roster changes, goes out whole so removals still arrive. Other tabs
keep receiving full frames, and `GetSnapshot` stays complete. Zoom in or select a truck and watch
**Vehicles/frame** drop. At the stress preset the opening view holds the whole fleet, so it still
receives all 500 vehicles per frame; selecting truck 007 drops it to about 191, the trucks in view
plus the out-of-view share for that frame. That count depends on where the trucks sit once the extent
settles; the recorded run carried 191. In this mode Records/s counts what actually arrived,
not the merged roster.

The raw-queue safety stop halts intake, not map updates, when pending records reach 20,000. It shows
a red banner with the rejected-record count and keeps telemetry flowing, and the map keeps draining
what it already holds. For `baseline`, Pending counts queued reports plus reports handed to the map
but not yet applied, so the stop trips only when the map falls behind the read rate. Click **Discard
backlog and resume** to clear it; in `baseline` that discards both the queue and the map's backlog.
`baseline` ignores reports until the map is ready, so Pending reads 0 during map startup while
Records/s is already nonzero. The scenario strip reports records per second, vehicles in the latest
received frame, pending count, map calls, edit latency percentiles, failure count, latest map batch
size, superseded state, and reconnect count.

Open **Display preview controls** below the map to pause truck 007, pause all reports, hide the fleet
locally, simulate a map failure, break the connection with a real server abort, or reset the
scenario. The **Mode** and **Load** pickers at the top let you reconfigure the dashboard: **Apply and
reload** restarts the server run and reloads through the same `?mode=&preset=` query string. A report becomes stale after five seconds. Map failures keep telemetry visible and expose
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
4. **Send the latest, not a queue.** Snapshot the store on the frame clock and send it, building the
   frame only when it is sent, so a slow send delays the next frame instead of queuing a stale one.
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
