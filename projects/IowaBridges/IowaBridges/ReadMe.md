# Getting Started with the GeoBlazor Web App Template

1. Get an API Key from the [ArcGIS Location Platform](https://location.arcgis.com/). 
2. Place this in your `appsetting.json`, `appsetting.Development.json`, or `secrets.json` (user secrets) files, once per rendering version of the application.
   If you chose render-mode `Auto`, you should have it in both projects. If you chose, `WebAssembly`, it will be in the `.Client`
   project, and `Server` in the main project. The Client file is inside the `wwwroot` folder.
   There should be a file with placeholders in all the expected locations.

   ```json
   {
       "ArcGISApiKey": "YourArcGISApiKey"
   }
   ```
   
   _Note: User secrets are not supported in the WebAssembly project, so you will need to use `appsettings.json` or `appsettings.Development.json` for that project._


3. Register at [licensing.dymaptic.com](https://licensing.dymaptic.com) for a free GeoBlazor Core Registration key,
   or to purchase a GeoBlazor Pro license key with additional features and support.
   Add the key to all the `appsettings.json`/`appsettings.Development.json`/`secrets.json` files:

    ```json
        {
            "ArcGISApiKey": "YourArcGISApiKey",
            "GeoBlazor": {
                // GeoBlazor Core
                "RegistrationKey": "YourGeoBlazorRegistrationKey"
                // GeoBlazor Pro
                "LicenseKey": "YourGeoBlazorProLicenseKey"
            }
        }
    ```

4. Run the web project. You should see interactive maps on both the `Home` and `Counter` pages.