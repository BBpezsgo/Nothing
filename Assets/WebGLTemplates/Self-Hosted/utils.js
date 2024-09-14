/**
 * @param {any} query
 */
function getElement(query) {
    const result = document.querySelector(query)
    if (!result) throw new Error(`Element "${query}" not found`)
    return result
}

function getElements() {
    /** @type {HTMLDivElement} */
    const container = getElement("#unity-container")
    /** @type {HTMLCanvasElement} */
    const canvas = getElement("#unity-canvas")
    /** @type {HTMLDivElement} */
    const loadingBar = getElement("#unity-loading-bar")
    /** @type {HTMLDivElement} */
    const progressBarFull = getElement("#unity-progress-bar-full")
    /** @type {HTMLDivElement} */
    const fullscreenButton = getElement("#unity-fullscreen-button")

    /** @type {HTMLDivElement} */
    const errorPopup = getElement("#unity-error-popup")
    /** @type {HTMLDivElement} */
    const warningPopup = getElement("#unity-warning-popup")
    /** @type {HTMLDivElement} */
    const infoPopup = getElement("#unity-info-popup")
    /** @type {HTMLParagraphElement} */
    const errorPopupText = getElement("#unity-error-popup>p")
    /** @type {HTMLParagraphElement} */
    const warningPopupText = getElement("#unity-warning-popup>p")
    /** @type {HTMLParagraphElement} */
    const infoPopupText = getElement("#unity-info-popup>p")

    return {
        container,
        canvas,
        loadingBar,
        progressBarFull,
        fullscreenButton,
        errorPopup,
        warningPopup,
        infoPopup,
        errorPopupText,
        warningPopupText,
        infoPopupText,
    }
}

/**
 * @param {string} msg
 * @param {'error' | 'warning' | 'info'} type
 */
function showPopup(msg, type) {
    if (type === 'error') {
        /** @type {HTMLDivElement} */
        const errorPopup = getElement("#unity-error-popup")
        /** @type {HTMLParagraphElement} */
        const errorPopupText = getElement("#unity-error-popup>p")

        errorPopup.classList.add('show')
        errorPopupText.innerText = msg
    } else if (type === 'warning') {
        /** @type {HTMLDivElement} */
        const warningPopup = getElement("#unity-warning-popup")
        /** @type {HTMLParagraphElement} */
        const warningPopupText = getElement("#unity-warning-popup>p")

        warningPopup.classList.add('show')
        warningPopupText.innerText = msg
    } else if (type === 'info') {
        /** @type {HTMLDivElement} */
        const infoPopup = getElement("#unity-info-popup")
        /** @type {HTMLParagraphElement} */
        const infoPopupText = getElement("#unity-info-popup>p")

        infoPopup.classList.add('show')
        infoPopupText.innerText = msg
    }
}

/**
 * @param {'error' | 'warning' | 'info'} type
 */
function onPopupClosed(type) {
    if (type === 'error') {
        
    } else if (type === 'warning') {
        
    } else if (type === 'info') {
        /** @type {HTMLParagraphElement} */
        const infoPopupText = getElement("#unity-info-popup>p")

        if (infoPopupText.innerText === 'The game has quitted!') {
            /** @type {HTMLCanvasElement} */
            const canvas = getElement("#unity-canvas")

            canvas.classList.add('game-quitted')
        }
    }
}
