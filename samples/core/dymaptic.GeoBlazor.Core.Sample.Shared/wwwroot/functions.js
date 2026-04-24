window.scrollToNav = (page) => {
    let navItems = document.getElementsByTagName('a');
    let navItem = Array.from(navItems).find(i => i.href.endsWith(page));
    if (navigator.userAgent.indexOf('Firefox') === -1) {
        // only do this when not in FireFox
        navItem?.scrollIntoViewIfNeeded();
    } else {
        if (!elementIsVisible(navItem)) {
            navItem.scrollIntoView();
        }
    }
}

window.loadApiKeyFromLocalStorage = () => {
    return window.localStorage['ArcGISApiKey'];
}

window.saveApiKeyToLocalStorage = (apiKey) => {
    window.localStorage['ArcGISApiKey'] = apiKey;
}


window.getWidth = () => {
    return window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;
}

window.getCalciteSelectValue = (calciteSelect) => {
    return calciteSelect.selectedOption.value;
}

window.setWaitCursor = (wait) => {
    if (wait) {
        document.body.style.cursor = 'wait';
    } else {
        document.body.style.cursor = 'default';
    }
}

window.isDarkMode = () => {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function elementIsVisible(item) {

    let eleTop = item.offsetTop;
    let eleBottom = eleTop + item.clientHeight;

    return (eleTop >= 0 && eleBottom <= window.innerHeight);
}

let Core;
let arcGisObjectRefs = {};

window.initializeGeoBlazor = (core) => {
    Core = core;
    arcGisObjectRefs = Core.arcGisObjectRefs;
}

window.copyCode = function(button) {
    const wrapper = button.closest('.code-highlight-wrapper');
    const copyIcon = button.querySelector('.copy-icon');
    const checkIcon = button.querySelector('.check-icon');

    // Find the visible code block (light or dark theme)
    const lightTheme = wrapper.querySelector('.code-light-theme');
    const darkTheme = wrapper.querySelector('.code-dark-theme');

    // Determine which theme is currently visible
    const visibleTheme = window.getComputedStyle(lightTheme).display !== 'none' ? lightTheme : darkTheme;
    const codeElement = visibleTheme.querySelector('pre');

    if (codeElement) {
        // Get the text content
        const code = codeElement.textContent;

        // Copy to clipboard
        navigator.clipboard.writeText(code).then(() => {
            // Show success feedback
            copyIcon.style.display = 'none';
            checkIcon.style.display = 'block';
            button.classList.add('copied');

            // Reset after 2 seconds
            setTimeout(() => {
                copyIcon.style.display = 'block';
                checkIcon.style.display = 'none';
                button.classList.remove('copied');
            }, 2000);
        }).catch(err => {
            console.error('Failed to copy code:', err);
        });
    }
}