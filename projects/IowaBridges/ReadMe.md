# Iowa Bridges

A Blazor Web App (.NET 10) built with [GeoBlazor](https://geoblazor.com) Core 4.4.4 that visualizes Iowa bridge condition data from the FHWA National Bridge Inventory (NBI). Features condition-based symbology, sidebar filters (condition, year built, county), bridge popups, and a "Find Bridges at Risk" query workflow.

This sample was built end-to-end with Claude Code. The full story — including the prompt (see [`Claude.md`](./Claude.md)) and lessons learned about agentic GeoBlazor development — is on the GeoBlazor blog:

**[Building with Claude Code and GeoBlazor](https://geoblazor.com/blog/building-with-claude-code-and-geoblazor)**

## Run It

```bash
cd IowaBridges
dotnet run -lp https
```

Then browse to <https://localhost:7654>.

## Configuration

Add your ArcGIS API key and (optionally) a GeoBlazor license/registration key to `IowaBridges/appsettings.Development.json`:

```json
{
  "ArcGISApiKey": "YOUR_ARCGIS_API_KEY",
  "GeoBlazor": {
    "RegistrationKey": "YOUR_GEOBLAZOR_CORE_REGISTRATION_KEY"
  }
}
```

Get an ArcGIS API key at the [ArcGIS Location Platform](https://location.arcgis.com/) and a free GeoBlazor Core registration key at [licensing.dymaptic.com](https://licensing.dymaptic.com).

## Data

- **Bridges:** [NTAD National Bridge Inventory](https://services.arcgis.com/xOi1kZaI0eWDREZv/ArcGIS/rest/services/NTAD_National_Bridge_Inventory/FeatureServer/0), filtered to Iowa (`STATE_CODE_001 = '19'`).
- **County boundaries:** [Iowa County Boundaries (Census)](https://services.arcgis.com/vPD5PVLI6sfkZ5E4/arcgis/rest/services/Iowa_County_Boundaries_Census/FeatureServer/0/).
