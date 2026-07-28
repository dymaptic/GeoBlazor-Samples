# GeoBlazor Samples

Sample applications and projects demonstrating [GeoBlazor](https://github.com/dymaptic/GeoBlazor) - a Blazor component library for ArcGIS Maps SDK for JavaScript.

## Repository Structure

### `samples/` - Library Samples

Built-in sample applications that ship with GeoBlazor Core and Pro. These demonstrate the full range of components and features.

- **`samples/core/`** - GeoBlazor Core samples
  - `dymaptic.GeoBlazor.Core.Sample.Shared` - Shared sample pages and components
  - `dymaptic.GeoBlazor.Core.Sample.Wasm` - Standalone WebAssembly sample runner
  - `dymaptic.GeoBlazor.Core.Sample.WebApp` - Blazor Web App sample runner (Server + WASM)
  - `dymaptic.GeoBlazor.Core.Sample.Maui` - MAUI hybrid sample runner
  - `dymaptic.GeoBlazor.Core.Sample.OAuth` - OAuth authentication sample
  - `dymaptic.GeoBlazor.Core.Sample.TokenRefresh` - Token refresh pattern sample

- **`samples/pro/`** - GeoBlazor Pro samples
  - `dymaptic.GeoBlazor.Pro.Sample.Shared` - Shared Pro sample pages and components
  - `dymaptic.GeoBlazor.Pro.Sample.Wasm` - Standalone WebAssembly Pro sample runner
  - `dymaptic.GeoBlazor.Pro.Sample.WebApp` - Blazor Web App Pro sample runner (Server + WASM)
  - `dymaptic.GeoBlazor.Pro.Sample.Maui` - MAUI hybrid Pro sample runner

### `projects/` - Full Project Examples

Real-world example applications built with GeoBlazor:

- **CustomPopups** - Custom popup functionality
- **CustomPopupsJS** - Custom popups with JavaScript integration
- **DesMoineBusRoutes** - Bus route mapping
- **MuseumsOfChicago** - Museum location finder
- **NationFinder** / **NationFinder2** - Country search applications
- **PointsOnAMapBlog** - Points of interest mapping
- **ShipmentTracker** - Shipment tracking
- **SolarTracker** - Solar panel location tracking

## Getting Started

### Using NuGet Packages (Default)

By default, samples reference the latest GeoBlazor NuGet packages (`Version="*"`).

```bash
# Run a Core sample
cd samples/core/dymaptic.GeoBlazor.Core.Sample.Wasm
dotnet run

# Run a Pro sample (requires Pro license)
cd samples/pro/dymaptic.GeoBlazor.Pro.Sample.Wasm
dotnet run

# Run a project example
cd projects/DesMoineBusRoutes
dotnet run
```

### Using Local Project References

To develop against local GeoBlazor source code, set `UseProjectReferences=true` and configure the project paths:

```bash
dotnet build /p:UseProjectReferences=true \
  /p:CoreProjectPath=../GeoBlazor/src/dymaptic.GeoBlazor.Core \
  /p:ProProjectPath=../GeoBlazor.Pro/src/dymaptic.GeoBlazor.Pro
```

Default project paths (when not explicitly set):
- `CoreProjectPath` -> `../GeoBlazor/src/dymaptic.GeoBlazor.Core`
- `ProProjectPath` -> `../src/dymaptic.GeoBlazor.Pro` when this repo is nested inside GeoBlazor.Pro,
  otherwise `../GeoBlazor.Pro/src/dymaptic.GeoBlazor.Pro` for sibling checkouts

#### Making the IDE agree

Passing `/p:UseProjectReferences=true` on the command line only affects that build — your IDE still
evaluates the projects with the property unset, so references, highlighting, and analyzers resolve
against the NuGet packages instead of your local source. To fix that, create a `local.props` beside
`Directory.Build.props` (gitignored, imported automatically):

```xml
<Project>
    <PropertyGroup>
        <UseProjectReferences>true</UseProjectReferences>
    </PropertyGroup>
</Project>
```

Reload the solution afterwards. This applies to plain `dotnet build` too, so the command-line flags
above become unnecessary.

## Configuration

Most samples require an ArcGIS API key. Add it to `appsettings.json` or user secrets:

```json
{
  "ArcGISApiKey": "YOUR_API_KEY"
}
```

Pro samples additionally require a GeoBlazor Pro license key.

## Related Repositories

- [GeoBlazor (Core)](https://github.com/dymaptic/GeoBlazor) - Open-source Blazor mapping library
- [GeoBlazor Pro](https://github.com/dymaptic/GeoBlazor.Pro) - Commercial extension with 3D support and advanced features
