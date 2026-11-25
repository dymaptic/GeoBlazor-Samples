> **GeoBlazor V5 update.** This project was originally written against dymaptic.GeoBlazor.Pro 0.9.11 (server project) and dymaptic.GeoBlazor.Core 3.0.1 (client project). It now references the single `dymaptic.GeoBlazor` package (V5) and targets net10.0. If you are following a blog post written for the earlier version, the code here no longer matches it exactly.

TODO:

add the following to your secrets.json in the WebApp project:
```
{
  "ArcGISApiKey": "YourAPIKey"
} 
```

If you are using an ApiKey supplied from the server add the following to your appsettings.json file in the WebApp.Client project:
```
{
  "ArcGISApiKey": "YourAPIKey"
} 
```