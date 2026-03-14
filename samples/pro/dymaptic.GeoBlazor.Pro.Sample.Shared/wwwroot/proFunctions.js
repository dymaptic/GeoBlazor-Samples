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

window.setInterceptors = (core, wfsServers) => {
    if (!wfsServers) {
        return;
    }
    core.esriConfig.request.interceptors.push({
        before: (params) => {
            let service = getCaseInsensitive(params.requestOptions.query, 'service');
            if (service === 'wfs' || service === 'wms') {
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