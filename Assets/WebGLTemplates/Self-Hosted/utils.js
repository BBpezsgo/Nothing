/**
 * @param {any} query
 */
function GetElement(query) {
    const result = document.querySelector(query)
    if (!result) throw new Error(`Element "${query}" not found`)
    return result
}

function Elements() {
    /** @type {HTMLDivElement} */
    const container = GetElement("#unity-container")
    /** @type {HTMLCanvasElement} */
    const canvas = GetElement("#unity-canvas")
    /** @type {HTMLDivElement} */
    const loadingBar = GetElement("#unity-loading-bar")
    /** @type {HTMLDivElement} */
    const progressBarFull = GetElement("#unity-progress-bar-full")
    /** @type {HTMLDivElement} */
    const fullscreenButton = GetElement("#unity-fullscreen-button")

    /** @type {HTMLDivElement} */
    const errorPopup = GetElement("#unity-error-popup")
    /** @type {HTMLDivElement} */
    const warningPopup = GetElement("#unity-warning-popup")
    /** @type {HTMLDivElement} */
    const infoPopup = GetElement("#unity-info-popup")
    /** @type {HTMLParagraphElement} */
    const errorPopupText = GetElement("#unity-error-popup>p")
    /** @type {HTMLParagraphElement} */
    const warningPopupText = GetElement("#unity-warning-popup>p")
    /** @type {HTMLParagraphElement} */
    const infoPopupText = GetElement("#unity-info-popup>p")

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
function ShowPopup(msg, type) {
    if (type === 'error') {
        /** @type {HTMLDivElement} */
        const errorPopup = GetElement("#unity-error-popup")
        /** @type {HTMLParagraphElement} */
        const errorPopupText = GetElement("#unity-error-popup>p")

        errorPopup.classList.add('show')
        errorPopupText.innerText = msg
    } else if (type === 'warning') {
        /** @type {HTMLDivElement} */
        const warningPopup = GetElement("#unity-warning-popup")
        /** @type {HTMLParagraphElement} */
        const warningPopupText = GetElement("#unity-warning-popup>p")

        warningPopup.classList.add('show')
        warningPopupText.innerText = msg
    } else if (type === 'info') {
        /** @type {HTMLDivElement} */
        const infoPopup = GetElement("#unity-info-popup")
        /** @type {HTMLParagraphElement} */
        const infoPopupText = GetElement("#unity-info-popup>p")

        infoPopup.classList.add('show')
        infoPopupText.innerText = msg
    }
}

/**
 * @param {'error' | 'warning' | 'info'} type
 */
function OnPopupClosed(type) {
    if (type === 'error') {
        
    } else if (type === 'warning') {
        
    } else if (type === 'info') {
        /** @type {HTMLParagraphElement} */
        const infoPopupText = GetElement("#unity-info-popup>p")

        if (infoPopupText.innerText === 'The game has quitted!') {
            /** @type {HTMLCanvasElement} */
            const canvas = GetElement("#unity-canvas")

            canvas.classList.add('game-quitted')
        }
    }
}
