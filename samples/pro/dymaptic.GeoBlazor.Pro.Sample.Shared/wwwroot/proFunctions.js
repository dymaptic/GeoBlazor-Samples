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