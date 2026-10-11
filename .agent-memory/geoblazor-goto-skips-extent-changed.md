# Programmatic MapView.GoTo does not raise OnExtentChanged (GeoBlazor Core 4.6.2)

GeoBlazor Core 4.6.2's JavaScript `goToExtent` sets `notifyExtentChanged = false` while a
programmatic `MapView.GoTo(extent)` runs, so zooming from .NET never raises `OnExtentChanged`.
After your own `GoTo`, read `MapView.GetExtent()` instead of waiting for the event. The HomeWidget
zooms through ArcGIS's native `goTo` and does raise the event. Applies to any sample that tracks the
view extent (FleetDashboard `viewport` mode).

Evidence: FleetDashboard `viewport` mode, branch `feature/fleet-viewport-and-hosted-layers`, commit
`cc427c3`. Before it, selecting truck 007 (which zooms via `GoTo`) left vehicles per frame at 500;
after reading `GetExtent()` post-`GoTo`, it dropped to about 105 in a Playwright run.
Verified: 2026-10-09
