# Points on A Map

This is the source code that goes along with the blog over
at [blog.dymaptic.com](https://blog.dymaptic.com/how-to-put-points-on-a-map-an-introduction-to-geometry-in-geoblazor).

I wrote this as an introduction to using Geometry in GeoBlazor: drawing points, using the geometry engine, and
specifying a spatial reference.
If that sounds of interest to you, check out the blog, but otherwise this is a good basic application that shows a few
different geometries.

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