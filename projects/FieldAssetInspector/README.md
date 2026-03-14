# Field Asset Inspector

A cross-platform **Uno Platform** application demonstrating **GeoBlazor Pro** map integration via MAUI Embedding.

## Overview

This sample showcases how to embed GeoBlazor Pro mapping capabilities inside an Uno Platform app using the MAUI Embedding feature. Users can interact with an ArcGIS map to view and inspect attributes of water network field assets (valves, hydrants, mains).

### Architecture

The solution consists of three projects:

| Project | Purpose |
|---------|---------|
| **FieldAssetInspector** | Uno Platform host app — provides the native XAML shell with sidebar navigation and asset detail panel |
| **FieldAssetInspector.MauiControls** | .NET MAUI class library — wraps a `BlazorWebView` in a `ContentView` for embedding in Uno via `MauiHost` |
| **FieldAssetInspector.Razor** | Razor Class Library — contains the GeoBlazor Pro map component, feature layer configuration, and hit-test interaction logic |

```
┌─────────────────────────────────────────────────────┐
│  Uno Platform App (XAML)                            │
│  ┌──────────┐  ┌────────────────────────────────┐   │
│  │ Sidebar  │  │  MauiHost                      │   │
│  │ (XAML)   │  │  ┌──────────────────────────┐  │   │
│  │          │  │  │ BlazorWebView            │  │   │
│  │ - Asset  │  │  │  ┌────────────────────┐  │  │   │
│  │   ID     │  │  │  │ GeoBlazor Pro Map  │  │  │   │
│  │ - Type   │  │  │  │ + Feature Layers   │  │  │   │
│  │ - Status │  │  │  │ + Widgets          │  │  │   │
│  │ - Notes  │  │  │  └────────────────────┘  │  │   │
│  │          │  │  └──────────────────────────┘  │   │
│  │ [Save]   │  └────────────────────────────────┘   │
│  └──────────┘                                       │
└─────────────────────────────────────────────────────┘
```

### Supported Platforms

Via Uno Platform + MAUI Embedding:

- **Windows** (WinAppSDK)
- **Android**
- **iOS**
- **macOS** (Mac Catalyst)
- **Linux** (Skia Desktop)

> **Note:** The WebAssembly (`browserwasm`) target is not included because MAUI Embedding (and thus `BlazorWebView`) is not supported on Uno's WASM target. For a web-hosted GeoBlazor experience, see the other sample projects in this repository.

## Prerequisites

- .NET 9.0 SDK
- Uno Platform SDK (`Uno.Sdk 6.5.29` — defined in `global.json`)
- An **ArcGIS API key** ([get one free](https://developers.arcgis.com/sign-up/))
- A **GeoBlazor Pro license key** ([dymaptic.com](https://www.dymaptic.com))

## Configuration

Set your API keys in `FieldAssetInspector/appsettings.json`:

```json
{
  "ArcGISApiKey": "YOUR_API_KEY",
  "GeoBlazor": {
    "ProLicenseKey": "YOUR_PRO_LICENSE_KEY"
  }
}
```

Or use .NET user secrets for local development.

## Build & Run

```bash
cd projects/FieldAssetInspector

# Windows (WinAppSDK)
dotnet build FieldAssetInspector/FieldAssetInspector.csproj -f net9.0-windows10.0.26100

# Desktop (Skia — Linux/macOS/Windows 7+)
dotnet run --project FieldAssetInspector/FieldAssetInspector.csproj -f net9.0-desktop

# Android
dotnet build FieldAssetInspector/FieldAssetInspector.csproj -f net9.0-android

# iOS
dotnet build FieldAssetInspector/FieldAssetInspector.csproj -f net9.0-ios
```

## Features Demonstrated

- **Uno Platform XAML** — Native sidebar with `TextBlock`, `TextBox`, and `Button` controls using Material theme
- **MAUI Embedding** — `MauiHost` control embedding a `BlazorWebView` inside Uno XAML
- **GeoBlazor Pro** — `MapView` with multiple `FeatureLayer` instances, `SimpleRenderer`, `PopupTemplate`, and interactive hit-testing
- **Cross-platform** — Single codebase targeting 5+ platforms via Uno Platform

## Data Source

The sample uses Esri's public [Water Network sample service](https://sampleserver6.arcgisonline.com/arcgis/rest/services/Water_Network/FeatureServer):
- **Layer 2** — Water network point assets (valves, junctions)
- **Layer 4** — Water mains (line features)
