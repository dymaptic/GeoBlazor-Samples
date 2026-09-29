# Route data

`routes.geojson` holds the four road-following paths the simulated fleet travels. The simulator,
the deterministic client source, and the map's route layer all read this one file, so a drawn route
and a vehicle's path can never disagree.

The file is prepared once, by hand, and committed. No routing service runs when the application
starts, and nothing in the sample calls a routing API at runtime.

## Provenance

- **Road geometry:** OpenStreetMap contributors, accessed through the public OSRM demo routing
  server (`https://router.project-osrm.org`) with `overview=full&geometries=geojson`.
- **Query:** one driving route per hand-picked endpoint pair, matching the four named routes. The
  coordinates are the returned road-following line, not straight-line interpolation.
- **Endpoints:** PA-611 between Swiftwater and Mount Pocono (`swiftwater`), Mount Pocono to Pocono
  Summit on PA-940 (`pocono`), PA-611 between Tannersville and Scotrun (`tannersville`), and Pocono
  Manor to Swiftwater (`manor`).
- **License:** OpenStreetMap data is offered under the Open Data Commons Open Database License
  (ODbL). OSRM's demo server is used only to prepare this file; the committed GeoJSON is the
  runtime artifact.

## Regenerating

```bash
cd projects/FleetDashboard/data
node prepare-routes.mjs
```

The script prints each route's point count and length and overwrites `routes.geojson` beside itself.
It needs network access and issues four requests in total. The output has no timestamps, so
regenerating from unchanged road data produces the same file.

## Guarantees the catalog checks at load

`FleetDashboard.Shared.Routing.FleetRoutes` validates every feature before the application uses it:

- each route has an `id`, a `name`, and at least two coordinate pairs;
- coordinates are in longitude/latitude order and within valid world ranges;
- no single segment jumps more than 5 km, so a route cannot teleport across the map.

These are demonstration paths, not navigation directions, calculated optimal routes, or claims about
legal truck access.
