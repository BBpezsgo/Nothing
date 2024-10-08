const elements = getElements()
const Preferences = unityPreprocessorVariables()

const buildUrl = "Build"
const loaderUrl = buildUrl + "/" + Preferences.LOADER_FILENAME

/** @type {import('./unity').UnityBuildConfig} */
const config = {
    dataUrl: buildUrl + "/" + Preferences.DATA_FILENAME,
    frameworkUrl: buildUrl + "/" + Preferences.FRAMEWORK_FILENAME,
    streamingAssetsUrl: "StreamingAssets",
    companyName: Preferences.COMPANY_NAME,
    productName: Preferences.PRODUCT_NAME,
    productVersion: Preferences.PRODUCT_VERSION,
    showBanner: showPopup,
}

if (Preferences.USE_WASM) {
    config.codeUrl = buildUrl + "/" + Preferences.CODE_FILENAME
}
if (Preferences.MEMORY_FILENAME) {
    config.memoryUrl = buildUrl + "/" + Preferences.MEMORY_FILENAME
}
if (Preferences.SYMBOLS_FILENAME) {
    config.symbolsUrl = buildUrl + "/" + Preferences.SYMBOLS_FILENAME
}

// By default Unity keeps WebGL canvas render target size matched with
// the DOM size of the canvas element (scaled by window.devicePixelRatio)
// Set this to false if you want to decouple this synchronization from
// happening inside the engine, and you would instead like to size up
// the canvas DOM size and WebGL render target sizes yourself.
// config.matchWebGLToCanvasSize = false;

if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
    // Mobile device style: fill the whole browser client area with the game canvas:

    var meta = document.createElement('meta')
    meta.name = 'viewport'
    meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes'
    document.getElementsByTagName('head')[0].appendChild(meta)
    elements.container.className = "unity-mobile"
    elements.canvas.className = "unity-mobile"

    // To lower canvas resolution on mobile devices to gain some
    // performance, uncomment the following line:
    // config.devicePixelRatio = 1;

    showPopup('WebGL builds are not supported on mobile devices.', 'error')
} else {
    // Desktop style: Render the game canvas in a window that can be maximized to fullscreen:

    elements.canvas.style.width = Preferences.WIDTH + "px"
    elements.canvas.style.height = Preferences.HEIGHT + "px"
}

if (Preferences.BACKGROUND_FILENAME) {
    elements.canvas.style.background = "url('" + buildUrl + "/" + Preferences.BACKGROUND_FILENAME.replace(/'/g, '%27') + "') center / cover"
}
elements.loadingBar.style.display = "block"

function onQuit() {
    showPopup('The game has quitted!', 'info')
}

function onScriptLoaded() {
    createUnityInstance(
        elements.canvas,
        config,
        progress => { elements.progressBarFull.style.width = 100 * progress + "%" }
    )
        .then(unityInstance => {
            elements.loadingBar.style.display = 'none'
            elements.fullscreenButton.onclick = () => {
                unityInstance.SetFullscreen(1)
            }
            unityInstance.Module.onQuit = onQuit
        })
        .catch(alert)
}

const script = document.createElement("script")
script.src = loaderUrl
script.onload = onScriptLoaded
document.body.appendChild(script)
