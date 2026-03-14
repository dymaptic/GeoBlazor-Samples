# GeoBlazor Samples Development Agent

You are helping develop sample applications for the GeoBlazor ecosystem.

## Key Context
- This is the **Samples** repository: https://github.com/dymaptic/GeoBlazor-Samples
- Core library: https://github.com/dymaptic/GeoBlazor
- Pro library: https://github.com/dymaptic/GeoBlazor.Pro

## Repository Layout
- `samples/core/` - Core (open-source) sample applications
- `samples/pro/` - Pro (commercial) sample applications
- `projects/` - Community/blog project examples

## Sample Architecture
- **`*.Sample.Shared`** - Shared Razor pages referenced by all hosting models
- **`*.Sample.WebApp`** - Blazor Server + WebAssembly interactive host
- **`*.Sample.Wasm`** - Standalone WebAssembly
- **`*.Sample.Maui`** - MAUI Hybrid

## Adding New Samples
1. Create a new Razor page in the appropriate `Sample.Shared/Pages/` directory
2. Use the same header pattern as existing samples:
   - Link to the ArcGIS JavaScript SDK sample page
   - Link to the data source (e.g., feature service URL) if applicable
3. Add navigation entry to `NavMenu.razor`
4. Test in both Server and WebAssembly render modes

## Building with Project References
When developing GeoBlazor Core/Pro alongside samples:
```bash
dotnet build <sample.csproj> /p:UseProjectReferences=true /p:CoreProjectPath=<path> /p:ProProjectPath=<path>
```

## Configuration
Samples need `appsettings.json` with `ArcGISApiKey` and optionally `GeoBlazor:LicenseKey` (for Pro samples). These files are gitignored.

## Live Demo
Pro samples deploy to https://samples.geoblazor.com via GitHub Actions in the GeoBlazor.Pro repository.
