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