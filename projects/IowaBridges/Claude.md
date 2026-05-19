You are a senior .NET + Blazor + GIS engineer. Generate a complete, buildable GeoBlazor app.

## Mission

  Create a Blazor Web App (.NET 10) using dymaptic.GeoBlazor.Core 4.4.4 and Interactive Server render mode in 20 minutes or less.

App purpose: visualize Iowa bridge condition data (NBI) with:

- condition-based symbology,
- sidebar filtering (condition, year built, county),
- bridge popups,
- "Find Bridges at Risk" query + highlight workflow.

## Hard Requirements

1. Use **GeoBlazor 4.4.4 APIs only** — no custom JS, no JSInterop, no `.js` files. If something seems to require JS interop, re-read the docs; GeoBlazor wraps it.
2. Use https://docs.geoblazor.com/pages/gettingStarted#project-setup for project setup, or start with the GeoBlazor templates (`dotnet new geoblazor -o IowaBridges -int Server`).
3. If this prompt conflicts with docs or real schema, **follow docs/schema** and add a short code comment noting the deviation.
4. Keep code nullable-aware (`<Nullable>enable</Nullable>`) and warning-free.
5. Do not invent API names: verify against the 4.4.4 docs/NuGet surface.
6. Output complete code edits for all changed files.
7. Use `Task`-returning async methods for any state changes on GeoBlazor components after first render (`SetDefinitionExpression`, `QueryFeatures`, `GoTo`, etc.).
8. Must have a running application within 20 minutes. Prioritize running code over feature completion. Check the time when you start and monitor it throughout.

## Authoritative Sources (must consult first)

- GeoBlazor docs: https://docs.geoblazor.com (Getting Started, FeatureLayer, Renderers, Popups, Widgets, Query, Authentication if relevant).
- NuGet API/version reference for `dymaptic.GeoBlazor.Core` 4.4.4
- ArcGIS layer metadata via `?f=json` (for real field names/types)
- Microsoft Blazor render modes (project structure rules): https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes
- If you run into compiler error/warning `BL0005`, the correct solution is to use parameterized constructors with named parameters, not to suppress the error.

## Project Structure

- `IowaBridges` (Server host)

**Critical render mode rule:** components with maps must use `InteractiveServer` render modes.

For this app: The project's `Components/Pages/` folder should contain only what's required for the host (`App.razor`, `Routes.razor`, error pages). Delete the template's `Counter.razor` and `Weather.razor` from the server project; create the map page at `IowaBridges/Components/Pages/Home.razor`.

Other rules:

- Add `dymaptic.GeoBlazor.Core` 4.4.4 to **`IowaBridges`** (the project that hosts the interactive components).
- Put matching `appsettings.Development.json` values in `IowaBridges/appsettings.Development.json`
- Register in `Program.cs`: `builder.Services.AddGeoBlazor(builder.Configuration);`
- Add GeoBlazor `@using` entries in `IowaBridges/Components/_Imports.razor` — at minimum: `dymaptic.GeoBlazor.Core`, `Components`, `Components.Layers`, `Components.Popups`, `Components.Renderers`, `Components.Symbols`, `Components.Widgets`.
- Add required GeoBlazor/Esri CSS links in `App.razor` `<head>` per 4.4.4 docs (the `_content/dymaptic.GeoBlazor.Core` and ESRI theme stylesheet).
- Set the https url in `launchSettings.json` to https://localhost:7654 and always run with `dotnet run -lp https`.

## Configuration

In the `appsettings.Development.json` file (project root), use these exact values:

```json
{
  "ArcGISApiKey": "AAPTabcZYLzID0KRH7Z-vtj_VBA..qqAD6DDw4lhRqH8D9QLdfv191VSBewGFqLdNmLyUGkQHJZDBaCInGn9xpJEMOqdp1lZ5twyBkN0SQiAlcWnQ8IC_dkCx5B9u13jtZZJkmAZ33h3PHn5RCdCzsqBi2VninJL3bTRct0fxFrtgLG5rUvxQZutWTOA3brR4lbsQtiWCwG9TZGfZb_Bpdp13W7APDbBNQQ98v9LMUj6dqJ2peJzyBgrsPLf6heCNMu01OFV9efa5jGXxEhw87SmT2lsNAT1_eMcYnQuI",
  "GeoBlazor": {
    "LicenseKey": "eyJNYWpvclZlcnNpb24iOm51bGwsIkVtYWlsIjoidGltLnB1cmR1bUBkeW1hcHRpYy5jb20iLCJMaWNlbnNlVHlwZSI6IkZyZWUiLCJTb2Z0d2FyZSI6Ikdlb0JsYXpvckNvcmUiLCJSZWZlcnJlcnMiOltdLCJBcHBOYW1lcyI6W10sIkxpY2Vuc2VWZXJzaW9uIjoxfQ=="
  }
}
```

## Data Sources

**Primary bridges layer (verified working):** 
- `https://services.arcgis.com/xOi1kZaI0eWDREZv/ArcGIS/rest/services/NTAD_National_Bridge_Inventory/FeatureServer/0`
- Schema
	- State filter: STATE_CODE_001 (string, '19' = Iowa)
	- Condition: DECK_COND_058, SUPERSTRUCTURE_COND_059, SUBSTRUCTURE_COND_060 (string '0'–'9', plus 'N' for not-applicable)
	-  Year built: YEAR_BUILT_027 (integer)
    - Average daily traffic: ADT_029 (integer)
    - County FIPS subcode: COUNTY_CODE_003 (string, 3-digit Iowa county FIPS)
    - Structure name/ID: STRUCTURE_NUMBER_008
    - Operating rating: OPERATING_RATING_064 (double) — not OPER_RATING_064
    - Inspection date: DATE_OF_INSPECT_090 (string YYYYMM)

This is the FHWA National Bridge Inventory hosted on the NTAD ArcGIS service. It contains all 50 states, so the bridge `FeatureLayer` **must** be filtered to Iowa using the state code as a baseline `DefinitionExpression`. Iowa's NBI state code is `19`. The field is `STATE_CODE_001`.

The Iowa filter must be combined with (not replaced by) any user-applied filters. Implement this by always prefixing the filter expression with the Iowa clause, e.g.:

```
(STATE_CODE_001 = '19') AND (<user filters here>)
```

**County boundaries (reference only, not interactive):** 
- `https://services.arcgis.com/vPD5PVLI6sfkZ5E4/arcgis/rest/services/Iowa_County_Boundaries_Census/FeatureServer/0/`
- Schema
	- OBJECTID (OID)
	- COUNTYNS (String)
	- GEOID (String)
	- GEOIDFQ (String)
	- IADOMID (Integer)
	- IADOMIDPAD (String)
	- NAME (String)
	- DISPLAYNAME (String)
	- AREALANDMI2 (Double)
	- AREAWATERMI2 (Double)
	- AREATOTALMI2 (Double)
	- INTPTLAT (String)
	- INTPTLON (String)
	- ACS5YRHOUSEHOLDS (Integer)
	- CEN2020POP (Integer)
	- URBANPOP (Integer)
	- RURALPOP (Integer)
	- PERURBAN (Double)
	- URBANRURAL (String)
	- Shape__Area (Double)
	- Shape__Length (Double)
- If this URL 404s, omit the county boundary layer entirely — it's nice-to-have, not required. The county _filter_ (dropdown) uses the FIPS code on the bridge layer itself and does not depend on this reference layer.

## UI + Behavior

### Map

- Basemap: `arcgis-navigation`
- Center: longitude `-93.6`, latitude `42.0`
- Zoom: `7`
- Layout: map fills viewport minus header; use `height: 100vh` minus the header height on the map container.

### Bridge Renderer

Because NBI condition is stored as a string with `'N'` for "not applicable" (common on culverts), use a `UniqueValueRenderer` with an Arcade `ValueExpression` that computes the minimum of the three condition fields, treating `'N'` as missing:

```
var d = IIF($feature.DECK_COND_058 == 'N', 99, Number($feature.DECK_COND_058));
var s = IIF($feature.SUPERSTRUCTURE_COND_059 == 'N', 99, Number($feature.SUPERSTRUCTURE_COND_059));
var b = IIF($feature.SUBSTRUCTURE_COND_060 == 'N', 99, Number($feature.SUBSTRUCTURE_COND_060));
var c = Min(d, s, b);
When(c <= 4, 'Poor', c <= 6, 'Fair', c <= 9, 'Good', 'Unknown')
```

Each `UniqueValueInfo` will have a `Symbol` based on the value of the ValueExpression (colors come from the design palette below):

- 0–4 → `--color-poor` — Poor
- 5–6 → `--color-fair` — Fair
- 7–9 → `--color-good` — Good
- default (99 = unknown / culvert) → small grey symbol

Symbol: `SimpleMarkerSymbol`, circle, size ~6, white outline 0.5.

### Sidebar (collapsible, ~320px wide, left-docked)

- **Condition** — three checkboxes (Good / Fair / Poor), all checked by default.
- **Year built** — dual-handle range slider. Hardcode to `1900–2025` to avoid complex queries.
- **County** — dropdown: "All Counties" + 99 Iowa counties (name → 3-digit FIPS) from a static `IowaCounties` helper class. The 3-digit `COUNTY_CODE_003` values run odd: `001, 003, 005, … 197`.

On any filter change, build one SQL definition expression (prefixed with the Iowa state clause as described under Data Sources) and apply via:

```csharp
await bridgeLayer.SetDefinitionExpression(expr);
```

Example combined expression:

```sql
STATE_CODE_001 = '19'
  AND (DECK_COND_058 IN ('0','1','2','3','4')
       OR SUPERSTRUCTURE_COND_059 IN ('0','1','2','3','4')
       OR SUBSTRUCTURE_COND_060 IN ('0','1','2','3','4'))
  AND YEAR_BUILT_027 BETWEEN 1920 AND 1980
  AND COUNTY_CODE_003 = '013'
```

Debounce the year slider (~250 ms) so it doesn't fire on every tick.

When selecting a county, the map should also zoom to that location.

### Popups

Attach a `PopupTemplate` to the bridge layer with a `FieldsPopupContent` and `FieldInfo` entries using friendly labels (not raw field names):

- Structure name / ID
- Composite condition (lowest of deck/super/substructure)
- Year built
- ADT
- Load rating (`OPER_RATING_064` or schema-equivalent)
- County name (translated from FIPS via `IowaCounties`)
- Inspection date (formatted)

### Widgets (positioned so they don't overlap the sidebar)

- `<LayerListWidget Position="OverlayPosition.TopRight" />`
- `<LegendWidget Position="OverlayPosition.BottomRight" />`
- `<SearchWidget Position="OverlayPosition.TopLeft" />`
- `<ScaleBarWidget Position="OverlayPosition.BottomLeft" Unit="ScaleUnit.Dual" />` — ScaleUnit values are NonMetric, Metric, Dual, Imperial. There is no DualMetric.

### "Find Bridges at Risk"

Button at the top of the sidebar. When clicked:

1. Build a `Query` with `Where =`:
    
    ```
    STATE_CODE_001 = '19'  AND (DECK_COND_058 IN ('0','1','2','3','4')       OR SUPERSTRUCTURE_COND_059 IN ('0','1','2','3','4')       OR SUBSTRUCTURE_COND_060 IN ('0','1','2','3','4'))  AND YEAR_BUILT_027 < 1960  AND ADT_029 > 5000
    ```
    
2. Set `OutFields = ["*"]` and `ReturnGeometry = true`.
3. Call `await bridgeLayer.QueryFeatures(query)`.
4. Add a `GraphicsLayer` (created at startup, kept as a field) and replace its graphics with the results, symbolized as a hollow accent-color ring (size ~16, no fill, 3 px stroke in `--color-accent`) — this draws a ring around the existing colored point so both are visible.
5. Display the count in the sidebar ("47 bridges at risk").
6. Compute the extent of results and `await MapView.GoTo(extent.Expand(1.2))`.
7. Provide a "Clear" button that empties the GraphicsLayer.

## Visual Design

The app should feel like a piece of considered infrastructure software, not a generic admin template. Think of the aesthetic as **"civic engineering deliverable, modernized"** — surveyor notebooks, NOAA charts, USGS quadrangles — confident with data, comfortable with monospace, restrained with color. The map is the show; the chrome supports it.

### Microcopy

Small things that add personality without being precious:

- Loading state on first map load: `Loading bridge data…` in monospace, muted.
- Empty state when filters return zero bridges: `No bridges match these filters.` centered over the map area with a small icon.
- After a successful "Find Bridges at Risk": pluralize correctly — `1 bridge at risk` vs `8 bridges at risk`. With these criteria (pre-1960, condition ≤ 4, ADT > 5000) the Iowa-wide count is typically single or low double digits, not ~47.
- Tooltip on the at-risk button: `Bridges built before 1960, condition ≤ 4, ADT > 5,000`.

### What to avoid

- Generic Bootstrap defaults (default blue primary button, rounded corners everywhere, light card-on-white).
- Material Design ripples or floating action buttons.
- Emoji in the UI.
- Gradients other than the very subtle ones already implied by the surface stack.
- Animated splash screens.

## File Organization

Create under `IowaBridges/Components/Bridges/`:

- `BridgeMap.razor` — owns the `MapView`, the bridge `FeatureLayer`, the `GraphicsLayer`, and the at-risk query method. Exposes events for filter changes.
- `BridgeSidebar.razor` — filter UI, "Find Bridges at Risk" button, result count, "Clear" button. Raises events upward.
- `BridgeFilterState.cs` — record/class holding current filter values, with a `ToDefinitionExpression()` method that always includes the Iowa state clause.
- `IowaCounties.cs` — static name ↔ 3-digit FIPS mapping for all 99 counties.
- `BridgeRendererFactory.cs` — builds the `ClassBreaksRenderer` using the condition colors above.

Create the map page at `IowaBridges/Components/Pages/Home.razor` with `@page "/"` and `@rendermode InteractiveAuto`. Remove the template's `Counter.razor` and `Weather.razor` from the server project.

## Delivery Format

Return:

1. The running application in Chrome or Edge, verified with the playwright mcp server or chrome dev tools mcp.
2. Prioritize getting the app running _before_ implementing all features. Use `dotnet watch run -lp https` to be able to edit and update while running.

## Success Criteria

When done, running `dotnet watch run -lp https` in the project folder should yield:

1. App builds with zero warnings and zero errors.
2. Map loads centered on Iowa with the navigation basemap visible.
3. Only Iowa bridges are rendered (state filter applied as baseline).
4. Bridges render in the design-palette colors (Good / Fair / Poor) with no missing tiles or 401 errors.
5. Header, sidebar, and typography match the visual design section (Inter + JetBrains Mono, warm-dark palette, monospace section headers).
6. Toggling a condition checkbox immediately filters visible bridges and the header count updates.
7. Adjusting the year slider re-filters within ~300 ms of release (debounced).
8. Selecting a county filters to that county's bridges.
9. Clicking a bridge opens a popup with friendly labels and a formatted inspection date.
10. Clicking "Find Bridges at Risk" outlines matching bridges with an accent-color ring, shows a non-zero count, and zooms the map to fit them. "Clear" removes the outlines.
11. All four widgets render without overlapping the sidebar.

Verify by actually running and using playwright or chrome dev mcp to test. If any step fails, prefer fixing the root cause over working around it. Leave the application running when you are done for me to test.

## Out of Scope

- Authentication / login flows
- Saving/exporting filter state
- Mobile-first redesign (basic responsive only)
- Unit tests (a manual smoke test against the success criteria is enough for v1)