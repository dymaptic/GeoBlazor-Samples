> **GeoBlazor V5 update.** This project was originally written against dymaptic.GeoBlazor.Core 3.2.1. It now references the single `dymaptic.GeoBlazor` package (V5) and targets net10.0. If you are following a blog post written for the earlier version, the code here no longer matches it exactly.

# Nation Finder 2

This is an updated version of the Nation Finder app. It is slimmed down to hopefully run better on a smaller Azure instance. There is no Signal-R, and most of the client/server round trips are removed. It designed to run as a server app but without pre-compiling. It uses the MemoryCache to store a leader board and the serialized nation names.


## To Run This:

You will need to configure your appsettings.json file, which has been ignored in this repository. It should look like
this (**Don't forget to update your ArcGIS API Key**):

```
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ArcGISApiKey": "Put your ArcGIS API Key Here"
}

```