window.setLoadingCursor = () => {
    document.body.classList.add('waiting');
}

window.removeLoadingCursor = () => {
    document.body.classList.remove('waiting');
}