
export function hideUiControls(core, viewId) {
    
    let arcGisObjectRefs = core.arcGisObjectRefs;
    let view = arcGisObjectRefs[viewId];
   
    view.ui.components = [];

}