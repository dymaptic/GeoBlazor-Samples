# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Repo Is

A collection of sample and demo applications for [GeoBlazor](https://github.com/dymaptic/GeoBlazor) — a Blazor component library wrapping ArcGIS Maps SDK for JavaScript. This is NOT the GeoBlazor library itself; these are consuming applications that demonstrate its usage.

## Build & Run

Each sample/project is an independent .NET application with its own `.sln` or `.csproj`:

```bash
# Run any individual sample or project
cd samples/dymaptic.GeoBlazor.Sample.Wasm
dotnet run

cd projects/DesMoineBusRoutes
dotnet run
```

`samples.slnx` at the root holds the merged sample apps (Shared, Wasm, WebApp, Maui, OAuth, TokenRefresh). Each `projects/` app has its own `.sln` or `.csproj`; build and run those individually.

**Never suppress the launch profile.** Do not pass `--no-launch-profile`, and do not override `--urls` in a way that bypasses `Properties/launchSettings.json`. Every profile in these samples sets `ASPNETCORE_ENVIRONMENT=Development`; without it the app starts in Production, where `MapStaticAssets`' development runtime handler cannot resolve `_framework` assets (these hosts have no `wwwroot` on disk). The failure is silent and easy to misread: `_framework/blazor.web.js` returns **HTTP 200 with 0 bytes**, `window.Blazor` stays `undefined`, and the page renders fully but is never interactive — no console error, no server error. The map still appears, because the `<arcgis-*>` web components self-initialize from prerendered HTML, so the page looks healthy while every event handler is dead.

To run on a specific port, add or pick a profile (`--launch-profile <name>`) rather than dropping profiles entirely. This affects local `dotnet run` against build output only; `dotnet publish` uses a different static-asset pipeline and is unaffected (samples.geoblazor.com runs in Production without issue).

### Using Local GeoBlazor Source

To develop against the local GeoBlazor library instead of the NuGet package:

```bash
dotnet build /p:UseProjectReferences=true \
  /p:GeoBlazorProjectPath=../GeoBlazor/src/dymaptic.GeoBlazor
```

The `Directory.Build.props` at the repo root controls this toggle. The `Choose` blocks in the `samples/` `.csproj` files switch between `PackageReference` (NuGet, default) and `ProjectReference` (local source) based on `UseProjectReferences`. The `projects/` apps always use the package.

`GeoBlazorProjectPath` defaults to `../GeoBlazor/src/dymaptic.GeoBlazor`, the sibling checkout. Override it when the library lives elsewhere, for example in a git worktree.

Command-line `/p:` flags don't reach the IDE's project-model evaluation, so Rider/VS keep resolving against the NuGet package (wrong references, wrong highlighting). For persistent local-source development, set the property in a gitignored `local.props` beside `Directory.Build.props`; it's imported first, before the conditions that read it. Developers who still have a `local.props` that sets `CoreProjectPath` or `ProProjectPath` must rename the property to `GeoBlazorProjectPath`, because nothing reads the old names.

### Local NuGet Packages

`nuget.config` adds a `./geoblazor_local` feed alongside nuget.org, for validating a pre-release `dymaptic.GeoBlazor` package before it is published. Pack the library into that folder. The folder is tracked through a `.gitkeep` (NuGet fails restore with NU1301 when a local source folder is missing); its packages are gitignored.

## Repository Layout

### `samples/` — Library Samples

Official GeoBlazor sample apps, one merged set for the single `dymaptic.GeoBlazor` package. These target the latest released .NET (see each `.csproj`) and use `Version="*"` for the GeoBlazor package (always latest).

- `dymaptic.GeoBlazor.Sample.Shared` — Razor class library with all sample pages/components
- `dymaptic.GeoBlazor.Sample.Wasm` — Standalone Blazor WebAssembly host
- `dymaptic.GeoBlazor.Sample.WebApp` — Blazor Web App host (Server + WASM with `.Client` project). This is the deployed host: it also serves the `/proxy` endpoint that WFS, WMS, and OGC pages rely on.
- `dymaptic.GeoBlazor.Sample.Maui` — MAUI hybrid host
- `dymaptic.GeoBlazor.Sample.OAuth` / `dymaptic.GeoBlazor.Sample.TokenRefresh` — Auth-specific samples
- `Dockerfile.server` and `nginx.conf` — container and static-hosting files

A new page goes in `Sample.Shared/Pages` with an entry in `Shared/NavMenu.razor.cs`. Every router must wrap `RouteView` in `<CascadingValue Name="PageRouteData" ...>`, because `MainLayout` and `SourceNav` require it.

### `projects/` — Real-World Examples (mixed targets)

Independent demo apps with varying .NET targets and hosting models. .NET target and GeoBlazor version vary per project and drift over time — **always check the project's `.csproj` for the actual versions.** Hosting models:

| Project | Hosting Model | GeoBlazor |
|---------|---------------|-----------|
| ShipmentTracker | Blazor Web App (Server + WASM) | `dymaptic.GeoBlazor` 5.* |
| SimpleSearch | Blazor Web App (Server + WASM) | `dymaptic.GeoBlazor` 5.* |
| NationFinder | Blazor Web App (Server + WASM) | `dymaptic.GeoBlazor` 5.* |
| NationFinder2 | Blazor Server | `dymaptic.GeoBlazor` 5.* |
| CustomPopups | Blazor WASM | `dymaptic.GeoBlazor` 5.* |
| CustomPopupsJS | Blazor WASM | `dymaptic.GeoBlazor` 5.* |
| DesMoineBusRoutes | Blazor Server | `dymaptic.GeoBlazor` 5.* |
| MuseumsOfChicago | Blazor Web App | `dymaptic.GeoBlazor` 5.* |
| PointsOnAMapBlog | Blazor WASM | `dymaptic.GeoBlazor` 5.* |
| SolarTracker | Blazor WASM | `dymaptic.GeoBlazor` 5.* |
| FieldAssetInspector | MAUI Embedding (BlazorWebView) | `dymaptic.GeoBlazor` (central version) |

Each project's README names the older package version it was written against. ShipmentTracker needs `../dymaptic.Blazor.StateManagement`, which is not in this repository.

## Configuration

Most samples require an **ArcGIS API key** in `appsettings.json` or user secrets:

```json
{ "ArcGISApiKey": "YOUR_API_KEY" }
```

A GeoBlazor registration key is optional (`GeoBlazor:RegistrationKey`, or the `LicenseKey` alias). Without one the library prints a single console reminder and every feature still works. CI supplies a key through `BuildAppSettings`. Projects with `UserSecretsId` support `dotnet user-secrets`.

## Key Patterns

- **Shared library pattern**: the samples put all Razor pages in `Sample.Shared`, then reference it from multiple hosting projects (WASM, WebApp, MAUI). This keeps demo code in one place.
- **GeoBlazor package toggle**: `Directory.Build.props` + `Choose` blocks in `.csproj` files enable switching between the NuGet package and a local project reference via MSBuild property.
- **No tests**: This is a samples repo — there are no test projects.

## Agents

Specialized Claude Code agent configurations for GeoBlazor development are maintained in the [GeoBlazor-Agents](https://github.com/dymaptic/GeoBlazor-Agents) repository. These provide researcher/developer/reviewer triplets for .NET, JavaScript, TypeScript, GeoBlazor Core/Pro/Code-Gen, C#/JS interop, and MCP development.

### Finding Agents

Agents are available from two locations (check in this order):

1. **Local on disk** — `CLAUDE_CONFIG_DIR` environment variable points to the local config directory (currently `D:/claude files`). Agent templates live in `$CLAUDE_CONFIG_DIR/agents/` with subdirectories per domain (`dotnet/`, `geoblazor/`, `interop/`, `javascript/`, `typescript/`, `mcp-experts/`).
2. **GitHub** — If not available locally, fetch from `https://github.com/dymaptic/GeoBlazor-Agents`. The `agents/` directory mirrors the local structure. Use `gh api repos/dymaptic/GeoBlazor-Agents/contents/agents` to browse, or clone the repo.

### Key Agent Files

- `AGENTS_REFERENCE.md` — Full catalog with triggers, descriptions, and usage examples
- `agents/DESIGN.md` — Agent system design principles and patterns
- `agents/AGENT_SYSTEM_TEMPLATE.md` — Template for creating new agents

### Agent Workflow

Use the **researcher → developer → reviewer** pattern: research existing patterns and known issues first, then implement, then review. For cross-repo work use `geoblazor-architect`; for C#/JS boundary issues use `interop-architect`.
