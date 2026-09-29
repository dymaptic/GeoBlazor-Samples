// Build-time route preparation for the Fleet Dashboard sample. Run it by hand, not during the
// build and not at application startup: it queries the public OSRM demo server (OpenStreetMap road
// data) for a driving line between each hand-picked endpoint pair and writes routes.geojson beside
// this script. The committed GeoJSON is the runtime source of truth; no routing service runs after
// this step.
//
// Usage: node prepare-routes.mjs
//
// Requires network access. OSRM's demo server is rate limited and intended for light use, so this
// script issues one request per route. Result coordinates are not edited by hand; re-running the
// script regenerates the same file when OSM data is unchanged.

import { writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const outputPath = join(here, "routes.geojson");

// One-way paths. The simulator travels each path forward and then back, so endpoints are real
// places rather than a closed loop. Coordinates are chosen on named roads in the Pocono region.
const routes = [
    {
        id: "swiftwater",
        name: "Swiftwater loop",
        from: [-75.3430, 41.0897], // PA-611, Swiftwater
        to: [-75.3575, 41.1220]    // PA-611, Mount Pocono
    },
    {
        id: "pocono",
        name: "Mount Pocono run",
        from: [-75.3575, 41.1220], // Mount Pocono
        to: [-75.4074, 41.1069]    // PA-940, Pocono Summit
    },
    {
        id: "tannersville",
        name: "Tannersville line",
        from: [-75.3052, 41.0401], // PA-611, Tannersville
        to: [-75.3190, 41.0668]    // PA-611, Scotrun
    },
    {
        id: "manor",
        name: "Pocono Manor link",
        from: [-75.3840, 41.1001], // Pocono Manor
        to: [-75.3430, 41.0897]    // PA-611, Swiftwater
    }
];

const features = [];
for (const route of routes) {
    const coordinates = `${route.from[0]},${route.from[1]};${route.to[0]},${route.to[1]}`;
    const url = `https://router.project-osrm.org/route/v1/driving/${coordinates}` +
        "?overview=full&geometries=geojson";
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error(`OSRM request for '${route.id}' failed with HTTP ${response.status}.`);
    }

    const body = await response.json();
    if (body.code !== "Ok" || !body.routes?.length) {
        throw new Error(`OSRM returned '${body.code}' for '${route.id}'.`);
    }

    features.push({
        type: "Feature",
        properties: { id: route.id, name: route.name },
        geometry: { type: "LineString", coordinates: body.routes[0].geometry.coordinates }
    });
    console.log(`${route.id}: ${body.routes[0].geometry.coordinates.length} points, ` +
        `${Math.round(body.routes[0].distance)} m`);
}

const geoJson = {
    type: "FeatureCollection",
    name: "fleet-dashboard-routes",
    // Provenance lives in README.md next to this file. Keep this document free of timestamps
    // so regeneration is diff-stable.
    features
};

await writeFile(outputPath, `${JSON.stringify(geoJson, null, 2)}\n`, "utf8");
console.log(`Wrote ${outputPath}`);
