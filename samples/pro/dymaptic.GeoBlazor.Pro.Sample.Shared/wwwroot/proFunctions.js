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
                let path = params.url.replace('https://', '');
                params.url = `https://${location.host}/proxy?url=${path}`;
            }
        }
    })
}

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

(function () {
    const STATUS_BANNER_KEY = 'gb-status-banner-dismissed';

    function applyBannerState() {
        let banner = document.getElementById('gb-status-banner');
        if (!banner) {
            return;
        }
        try {
            if (sessionStorage.getItem(STATUS_BANNER_KEY) === banner.dataset.message) {
                banner.remove();
            }
        } catch {
            // sessionStorage unavailable (private mode)
        }
    }

    document.addEventListener('click', (e) => {
        let closeButton = e.target.closest('.gb-status-banner-close');
        if (!closeButton) {
            return;
        }
        let banner = closeButton.closest('#gb-status-banner');
        if (!banner) {
            return;
        }
        try {
            sessionStorage.setItem(STATUS_BANNER_KEY, banner.dataset.message);
        } catch {
            // sessionStorage unavailable (private mode)
        }
        banner.remove();
    });

    function registerEnhancedLoad() {
        if (window.Blazor) {
            window.Blazor.addEventListener('enhancedload', applyBannerState);
            return true;
        }
        return false;
    }

    document.addEventListener('DOMContentLoaded', () => {
        applyBannerState();
        if (!registerEnhancedLoad()) {
            let attempts = 0;
            let timer = setInterval(() => {
                if (registerEnhancedLoad() || ++attempts > 50) {
                    clearInterval(timer);
                }
            }, 100);
        }
    });
})();
// Sets the starting camera of the page's SceneView. Used by the WebStyleSymbols3D sample because the
// SceneView Tilt/ZIndex parameters do not reach the camera in GeoBlazor 4.6.2 (dymaptic/GeoBlazor#540).
window.goToSceneCamera = function (longitude, latitude, heightAboveGround, tilt, heading) {
    const element = document.querySelector('arcgis-scene');
    if (!element) {
        return false;
    }

    const applyCamera = async () => {
        const view = element.view;
        if (!element.isConnected || !view) {
            return false;
        }

        await view.when();

        // The camera z is an absolute elevation, so lift it above the sampled ground by the requested height.
        // If the ground sampler is not ready, fall back to an absolute elevation that clears the local
        // terrain (about 460 m at this location).
        let z = 1000;
        try {
            const ground = view.groundView?.elevationSampler?.queryElevation({ longitude, latitude });
            if (ground && Number.isFinite(ground.z)) {
                z = ground.z + heightAboveGround;
            }
        } catch (error) {
            // Sampling the ground is best-effort; the absolute fallback above is used instead.
        }

        await view.goTo({ position: { longitude, latitude, z }, tilt, heading });
        return true;
    };

    // The scene is built more than once during startup (prerender then interactive render), and a rebuild
    // resets the camera, so apply the target a few times over the first few seconds. Runs detached so the
    // caller's JS interop call returns immediately. The captured element keeps every retry on this page's
    // scene, and a rejected goTo (an abort from user interaction, for example) cannot stop the rest.
    const delays = [0, 1200, 1800];
    (async () => {
        for (const delay of delays) {
            await new Promise(resolve => setTimeout(resolve, delay));
            try {
                await applyCamera();
            } catch (error) {
                if (!error || error.name !== 'AbortError') {
                    console.warn('Scene camera update failed.', error);
                }
            }
        }
    })();

    return true;
};
