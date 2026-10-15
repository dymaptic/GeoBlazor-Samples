window.initialize = () => {
    window.addEventListener('resize', () => {
        let navmenu = document.querySelector('.navbar__menu-item-container');
        let lowerNav = document.querySelector('#lower-nav-container');
        if (window.hasSmallWidth() && !navmenu.classList.contains('collapse')) {
            navmenu.classList.add('collapse');
        }
        if (window.hasSmallWidth() && !lowerNav.classList.contains('lower-collapse')) {
            lowerNav.classList.add('lower-collapse');
        }
    })
}

window.initialize();

window.hasSmallWidth = function () {
    return window.innerWidth <= 1075;
}

window.setInterceptors = (core) => {
    core.esriConfig.request.interceptors.push({
        before: (params) => {
            let service = getCaseInsensitive(params.requestOptions.query, 'service');
            if (service === 'wfs' || service === 'wms'
                || (!params.url.includes('arcgis')
                    && params.requestOptions?.headers
                    && Object.hasOwn(params.requestOptions.headers, 'accept')
                    && params.requestOptions.headers['accept'].includes('json'))) {
                // Relative so the proxy is reached over whatever scheme the app is served on.
                let path = params.url.replace(/^https?:\/\//, '');
                params.url = `/proxy?url=${path}`;
            }
        }
    })
}

// region Calcite slider interop

// Esri's own "Intro to ImageryTileLayer" sample drives the raster stretch with a bare
// <calcite-slider> rather than the ArcGIS Slider widget. ArcGIS widgets whose host element sits
// inside <arcgis-map> are hijacked into the view's floating UI corners, so a generic Calcite
// control is the supported way to place a slider inside a LayerList item panel. GeoBlazor Pro's
// bundle already registers calcite-slider, so no extra script or CDN reference is needed.

const calciteSliderListeners = {};

window.initCalciteSlider = async function (sliderId, dotNetRef, callbackMethod) {
    let slider = await getCalciteSlider(sliderId);
    if (slider === null) return;
    let controller = new AbortController();
    calciteSliderListeners[sliderId] = controller;
    // calciteSliderChange fires when the thumb is released. Esri's sample uses calciteSliderInput,
    // which fires on every drag frame and would cost a .NET interop round trip each time.
    slider.addEventListener('calciteSliderChange',
        () => dotNetRef.invokeMethodAsync(callbackMethod, readCalciteSliderValues(slider)),
        { signal: controller.signal });
}

window.configureCalciteSlider = async function (sliderId, min, max, step, ticks, values) {
    let slider = await getCalciteSlider(sliderId);
    if (slider === null) return;
    slider.max = max;
    slider.min = min;
    slider.step = step;
    slider.ticks = ticks;
    // An array value renders a two-thumb range slider, a single number renders one thumb.
    slider.value = values.length > 1 ? values : values[0];
}

window.disposeCalciteSlider = function (sliderId) {
    calciteSliderListeners[sliderId]?.abort();
    delete calciteSliderListeners[sliderId];
}

async function getCalciteSlider(sliderId) {
    await customElements.whenDefined('calcite-slider');
    let slider = document.getElementById(sliderId);
    if (slider === null) {
        console.warn(`No calcite-slider found with id "${sliderId}".`);
    }
    return slider;
}

function readCalciteSliderValues(slider) {
    return Array.isArray(slider.value) ? slider.value : [slider.value ?? 0];
}

// endregion

function getCaseInsensitive(obj, key) {
    if (obj && typeof obj === "object") {
        const lowerKey = key.toLowerCase();
        for (const k in obj) {
            if (k.toLowerCase() === lowerKey) {
                return obj[k].toLowerCase();
            }
        }
    }
    return undefined;
}

// Walks the light DOM and every open shadow root, because ArcGIS map components move slotted widgets into their own
// shadow tree.
function queryComposed(root, selector) {
    const found = root.querySelector(selector);
    if (found) {
        return found;
    }
    for (const element of root.querySelectorAll('*')) {
        const nested = element.shadowRoot ? queryComposed(element.shadowRoot, selector) : null;
        if (nested) {
            return nested;
        }
    }
    return null;
}

// Starts the Editor's multi-feature workflow on the features of the titled layer whose Name is in `names`. The names
// are matched in a query because the layer's OBJECTID values are not the integers the C# side wrote.
window.startMultiFeatureEdit = async (layerTitle, names) => {
    const map = queryComposed(document, 'arcgis-map')?.map;
    const editor = queryComposed(document, 'arcgis-editor');
    const layer = map?.layers.find(l => l.title === layerTitle);
    if (!editor || !layer) {
        throw new Error(`The editor or the "${layerTitle}" layer is not on the page yet. Try again in a moment.`);
    }
    const quoted = names.map(n => `'${n.replaceAll("'", "''")}'`).join(',');
    const objectIds = await layer.queryObjectIds({where: `Name IN (${quoted})`});
    await editor.startUpdateFeaturesWorkflow({layer, objectIds});
};
