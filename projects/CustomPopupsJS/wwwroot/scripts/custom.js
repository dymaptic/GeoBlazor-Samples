import {arcGisObjectRefs} from "/_content/dymaptic.GeoBlazor.Core/js/arcGisJsInterop.js"

export async function configureLayer(layerId, mapViewId) {
    console.log("Configure Layer");
    const measureThisAction = {
        title: "Edit Me",
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
    console.log(layer);


}
