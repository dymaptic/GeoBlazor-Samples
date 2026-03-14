# CLAUDE.md - GeoBlazor Samples

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains all sample applications for the [GeoBlazor](https://github.com/dymaptic/GeoBlazor) ecosystem. GeoBlazor is a Blazor component library that brings ArcGIS Maps SDK for JavaScript capabilities to .NET applications using pure C#.

## Related Repositories

| Repository        | URL                                              | Purpose                              |
|-------------------|--------------------------------------------------|--------------------------------------|
| **This Repo**     | https://github.com/dymaptic/GeoBlazor-Samples    | Sample applications                  |
| GeoBlazor (Core)  | https://github.com/dymaptic/GeoBlazor             | Open-source Blazor mapping library   |
| GeoBlazor Pro     | https://github.com/dymaptic/GeoBlazor.Pro         | Commercial extension with 3D support |

## Repository Structure

```
GeoBlazor-Samples/
├── samples/
│   ├── core/                                    # Core (open-source) samples
│   │   ├── dymaptic.GeoBlazor.Core.Sample.Shared/   # Shared sample pages (Razor)
│   │   ├── dymaptic.GeoBlazor.Core.Sample.WebApp/   # Blazor Server + WASM host
│   │   ├── dymaptic.GeoBlazor.Core.Sample.Wasm/     # Standalone WASM
│   │   ├── dymaptic.GeoBlazor.Core.Sample.Maui/     # MAUI Hybrid
│   │   ├── dymaptic.GeoBlazor.Core.Sample.OAuth/    # OAuth authentication demo
│   │   └── dymaptic.GeoBlazor.Core.Sample.TokenRefresh/  # Token refresh demo
│   └── pro/                                     # Pro (commercial) samples
│       ├── dymaptic.GeoBlazor.Pro.Sample.Shared/     # Shared Pro sample pages
│       ├── dymaptic.GeoBlazor.Pro.Sample.WebApp/     # Pro Blazor Server + WASM host
│       ├── dymaptic.GeoBlazor.Pro.Sample.Wasm/       # Pro standalone WASM
│       └── dymaptic.GeoBlazor.Pro.Sample.Maui/       # Pro MAUI Hybrid
├── projects/                                    # Community/blog project examples
│   ├── CustomPopups/
│   ├── NationFinder/
│   ├── DesMoineBusRoutes/
│   ├── SolarTracker/
│   └── ...
└── packages/                                    # Local NuGet package cache
```

## Common Commands

### Build and Run Samples

Samples reference GeoBlazor via NuGet packages by default. To run locally:

```bash
# Run Core WebApp sample
dotnet run --project samples/core/dymaptic.GeoBlazor.Core.Sample.WebApp/dymaptic.GeoBlazor.Core.Sample.WebApp/dymaptic.GeoBlazor.Core.Sample.WebApp.csproj

# Run Pro WebApp sample
dotnet run --project samples/pro/dymaptic.GeoBlazor.Pro.Sample.WebApp/dymaptic.GeoBlazor.Pro.Sample.WebApp/dymaptic.GeoBlazor.Pro.Sample.WebApp.csproj
```

### Build with Project References (for development)

When developing GeoBlazor Core/Pro alongside samples, use MSBuild properties to switch from NuGet to project references:

```bash
dotnet build <sample.csproj> /p:UseProjectReferences=true /p:CoreProjectPath=<path-to-core> /p:ProProjectPath=<path-to-pro>
```

## Configuration

Sample apps require an `appsettings.json` with:

```json
{
  "ArcGISApiKey": "<your-api-key>",
  "GeoBlazor": {
    "LicenseKey": "<pro-license-key-if-using-pro>"
  }
}
```

These files are in `.gitignore` to avoid committing secrets.

## Adding New Samples

1. Create a new Razor page in the appropriate `Sample.Shared/Pages/` directory
2. Use the same header pattern as existing samples (links to ArcGIS JS sample + data source)
3. Add navigation entry to `NavMenu.razor`
4. Test in both Server and WebAssembly render modes

## Live Demo

The Pro samples are deployed at https://samples.geoblazor.com via GitHub Actions in the GeoBlazor.Pro repository.

## Dependencies

- .NET 8.0+ SDK
- ArcGIS API key (free from [ArcGIS Developers](https://developers.arcgis.com/))
- GeoBlazor Pro license key (for Pro samples only)
