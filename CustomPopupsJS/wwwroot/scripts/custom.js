import {arcGisObjectRefs} from "/_content/dymaptic.GeoBlazor.Core/js/arcGisJsInterop.js"

export function getProperty(obj, prop) {
    return obj[prop];
}
export async function configureLayer(dotnetObj, layerId, mapViewId) {
    console.log("Configure Layer");
    const measureThisAction = {
        title: "Take Action",
        id: "take-some-action",
        className: "esri-icon-edit"
    };
    let layer = arcGisObjectRefs[layerId];
    let view = arcGisObjectRefs[mapViewId];
    view.popup.dockEnabled = true;
    view.popup.dockOptions.buttonEnabled = false;
    layer.graphics.forEach(g => {
        g.popupTemplate.actions = [measureThisAction];
    });
    view.popup.on("trigger-action", function (event) {
        if (event.action.id === "take-some-action") {
            console.log("Take Action JS");
            dotnetObj.invokeMethodAsync("TakeAction", DotNet.createJSObjectReference(view.popup.selectedFeature));
            //dotnetObj.invokeMethodAsync("TakeAction", arcGisObjectRefs.buildDotNetGraphic(event.graphic));
        }
    });
    console.log(layer);


}
