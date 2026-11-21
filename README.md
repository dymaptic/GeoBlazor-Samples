# GeoBlazor Samples

Sample applications and projects demonstrating [GeoBlazor](https://github.com/dymaptic/GeoBlazor), a Blazor component library for ArcGIS Maps SDK for JavaScript. GeoBlazor V5 is one NuGet package, `dymaptic.GeoBlazor`, and these samples use it throughout.

## Repository Structure

### `samples/` - Library Samples

The sample apps that ship with GeoBlazor and run at samples.geoblazor.com. They demonstrate the full range of components and features.

- `dymaptic.GeoBlazor.Sample.Shared` - Shared sample pages and components
- `dymaptic.GeoBlazor.Sample.Wasm` - Standalone WebAssembly sample runner
- `dymaptic.GeoBlazor.Sample.WebApp` - Blazor Web App sample runner (Server + WASM), the deployed host
- `dymaptic.GeoBlazor.Sample.Maui` - MAUI hybrid sample runner
- `dymaptic.GeoBlazor.Sample.OAuth` - OAuth authentication sample
- `dymaptic.GeoBlazor.Sample.TokenRefresh` - Token refresh pattern sample

### `projects/` - Full Project Examples

Real-world example applications built with GeoBlazor. Several accompany blog posts. Each README names the GeoBlazor version the app was originally written against.

- **CustomPopups** - Custom popup functionality
- **CustomPopupsJS** - Custom popups with JavaScript integration
- **DesMoineBusRoutes** - Bus route mapping
- **FieldAssetInspector** - MAUI field inspection app
- **IowaBridges** - Bridge condition map
- **MuseumsOfChicago** - Museum location finder
- **NationFinder** / **NationFinder2** - Country search applications
- **PointsOnAMapBlog** - Points of interest mapping
- **ShipmentTracker** - Shipment tracking (needs `../dymaptic.Blazor.StateManagement`, which is not in this repository)
- **SimpleSearch** - Search widget walkthrough
- **SolarTracker** - Day/night terminator

## Getting Started

### Using the NuGet Package (Default)

By default, samples reference the latest `dymaptic.GeoBlazor` NuGet package (`Version="*"`).

```bash
# Run a sample
cd samples/dymaptic.GeoBlazor.Sample.Wasm
dotnet run

# Run a project example
cd projects/DesMoineBusRoutes
dotnet run
```

### Using the Local Library Source

To develop against local GeoBlazor source code, set `UseProjectReferences=true`:

```bash
dotnet build /p:UseProjectReferences=true \
  /p:GeoBlazorProjectPath=../GeoBlazor/src/dymaptic.GeoBlazor
```

`GeoBlazorProjectPath` defaults to `../GeoBlazor/src/dymaptic.GeoBlazor`, the sibling checkout. Set it explicitly when the library is somewhere else. This replaces the old `CoreProjectPath` and `ProProjectPath` properties. If you keep a `local.props` that sets either one, rename it to `GeoBlazorProjectPath`, because nothing reads the old names.

#### Making the IDE agree

Passing `/p:UseProjectReferences=true` on the command line only affects that build, so your IDE still evaluates the projects with the property unset, and references, highlighting, and analyzers resolve against the NuGet package instead of your local source. To fix that, create a `local.props` beside `Directory.Build.props` (gitignored, imported automatically):

```xml
<Project>
    <PropertyGroup>
        <UseProjectReferences>true</UseProjectReferences>
    </PropertyGroup>
</Project>
```

Reload the solution afterwards. This applies to plain `dotnet build` too, so the command-line flags above become unnecessary.

### Testing a Pre-Release Package

`nuget.config` includes a `geoblazor_local` feed at `./geoblazor_local`. Pack the library into that folder to build the samples against a package that is not on nuget.org yet.

## Configuration

Most samples require an ArcGIS API key. Add it to `appsettings.json` or user secrets:

```json
{
  "ArcGISApiKey": "YOUR_API_KEY"
}
```

A GeoBlazor registration key is optional. Without one, every feature works and the library prints one reminder to the console.

## Related Repositories

- [GeoBlazor](https://github.com/dymaptic/GeoBlazor) - Open-source Blazor mapping library
