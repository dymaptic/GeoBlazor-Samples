# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Repo Is

A collection of sample and demo applications for [GeoBlazor](https://github.com/dymaptic/GeoBlazor) — a Blazor component library wrapping ArcGIS Maps SDK for JavaScript. This is NOT the GeoBlazor library itself; these are consuming applications that demonstrate its usage.

## Build & Run

Each sample/project is an independent .NET application with its own `.sln` or `.csproj`:

```bash
# Run any individual sample or project
cd samples/core/dymaptic.GeoBlazor.Core.Sample.Wasm
dotnet run

cd projects/DesMoineBusRoutes
dotnet run
```

There is no top-level solution file. Build/run at the individual project level.

**Never suppress the launch profile.** Do not pass `--no-launch-profile`, and do not override `--urls` in a way that bypasses `Properties/launchSettings.json`. Every profile in these samples sets `ASPNETCORE_ENVIRONMENT=Development`; without it the app starts in Production, where `MapStaticAssets`' development runtime handler cannot resolve `_framework` assets (these hosts have no `wwwroot` on disk). The failure is silent and easy to misread: `_framework/blazor.web.js` returns **HTTP 200 with 0 bytes**, `window.Blazor` stays `undefined`, and the page renders fully but is never interactive — no console error, no server error. The map still appears, because the `<arcgis-*>` web components self-initialize from prerendered HTML, so the page looks healthy while every event handler is dead.

To run on a specific port, add or pick a profile (`--launch-profile <name>`) rather than dropping profiles entirely. This affects local `dotnet run` against build output only; `dotnet publish` uses a different static-asset pipeline and is unaffected (samples.geoblazor.com runs in Production without issue).

### Using Local GeoBlazor Source

To develop against local GeoBlazor repos instead of NuGet packages:

```bash
dotnet build /p:UseProjectReferences=true \
  /p:CoreProjectPath=../GeoBlazor/src/dymaptic.GeoBlazor.Core \
  /p:ProProjectPath=../GeoBlazor.Pro/src/dymaptic.GeoBlazor.Pro
```

The `Directory.Build.props` at the repo root controls this toggle. The `Choose` blocks in individual `.csproj` files switch between `PackageReference` (NuGet, default) and `ProjectReference` (local source) based on `UseProjectReferences`.

Command-line `/p:` flags don't reach the IDE's project-model evaluation, so Rider/VS keep resolving against the NuGet packages (wrong references, wrong highlighting). For persistent local-source development, set the property in a gitignored `local.props` beside `Directory.Build.props` — it's imported first, before the parent-props import and the `Choose` conditions that read it. `ProProjectPath` defaults correctly for both the nested-submodule and sibling-checkout layouts; only `local.props` (or `/p:`) is needed.

### Local NuGet Packages

`nuget.config` adds a `./packages` local feed alongside nuget.org. Drop `.nupkg` files in `packages/` for local testing.

## Repository Layout

### `samples/` — Library Samples

Official GeoBlazor sample apps, split into Core and Pro. These target the latest released .NET (see each `.csproj`) and use `Version="*"` for GeoBlazor packages (always latest).

- **`samples/core/`** — Core samples use a Shared library pattern:
  - `*.Sample.Shared` — Razor class library with all sample pages/components
  - `*.Sample.Wasm` — Standalone Blazor WebAssembly host
  - `*.Sample.WebApp` — Blazor Web App host (Server + WASM with `.Client` project)
  - `*.Sample.Maui` — MAUI hybrid host
  - `*.Sample.OAuth` / `*.Sample.TokenRefresh` — Auth-specific samples

- **`samples/pro/`** — Pro samples mirror the Core structure but add Pro-specific features (3D, advanced widgets). Pro Shared references Core Shared.

### `projects/` — Real-World Examples (mixed targets)

Independent demo apps with varying .NET targets and hosting models. .NET target and GeoBlazor version vary per project and drift over time — **always check the project's `.csproj` for the actual versions.** Hosting models:

| Project | Hosting Model | GeoBlazor |
|---------|---------------|-----------|
| ShipmentTracker | Blazor Web App (Server + WASM) | Pro (pinned) |
| SimpleSearch | Blazor Web App (Server + WASM) | Core (latest) |
| NationFinder | Blazor Web App (Server + WASM) | Core (latest) |
| NationFinder2 | Blazor Server | Core (latest) |
| CustomPopups | Blazor Server | Core (pinned) |
| CustomPopupsJS | Blazor Server | Core (pinned) |
| DesMoineBusRoutes | Blazor Server | Core (pinned) |
| MuseumsOfChicago | Blazor Web App | Core (latest) |
| PointsOnAMapBlog | Blazor WASM | Core (pinned) |
| SolarTracker | Blazor WASM | Core (pinned) |
| FieldAssetInspector | Uno Platform + MAUI Embedding (BlazorWebView) | Core (latest) |

## Configuration

Most samples require an **ArcGIS API key** in `appsettings.json` or user secrets:

```json
{ "ArcGISApiKey": "YOUR_API_KEY" }
```

Pro samples additionally need a **GeoBlazor Pro license key**. Projects with `UserSecretsId` support `dotnet user-secrets`.

## Key Patterns

- **Shared library pattern**: Core and Pro samples put all Razor pages in a `*.Sample.Shared` RCL, then reference it from multiple hosting projects (WASM, WebApp, MAUI). This keeps demo code in one place.
- **GeoBlazor package toggle**: `Directory.Build.props` + `Choose` blocks in `.csproj` files enable switching between NuGet packages and local project references via MSBuild property.
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
