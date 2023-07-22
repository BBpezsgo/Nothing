const elements = Elements()

const buildUrl = "Build"
const loaderUrl = buildUrl + "/{{{ LOADER_FILENAME }}}"

const config = {
    dataUrl: buildUrl + "/{{{ DATA_FILENAME }}}",
    frameworkUrl: buildUrl + "/{{{ FRAMEWORK_FILENAME }}}",
#if USE_WASM
    codeUrl: buildUrl + "/{{{ CODE_FILENAME }}}",
#endif
#if MEMORY_FILENAME
    memoryUrl: buildUrl + "/{{{ MEMORY_FILENAME }}}",
#endif
#if SYMBOLS_FILENAME
    symbolsUrl: buildUrl + "/{{{ SYMBOLS_FILENAME }}}",
#endif
    streamingAssetsUrl: "StreamingAssets",
    companyName: {{{ JSON.stringify(COMPANY_NAME) }}},
    productName: {{{ JSON.stringify(PRODUCT_NAME) }}},
    productVersion: {{{ JSON.stringify(PRODUCT_VERSION) }}},
    showBanner: ShowPopup,
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

    ShowPopup('WebGL builds are not supported on mobile devices.', 'error')
} else {
    // Desktop style: Render the game canvas in a window that can be maximized to fullscreen:

    elements.canvas.style.width = "{{{ WIDTH }}}px"
    elements.canvas.style.height = "{{{ HEIGHT }}}px"
}

#if BACKGROUND_FILENAME
elements.canvas.style.background = "url('" + buildUrl + "/{{{ BACKGROUND_FILENAME.replace(/'/g, '%27') }}}') center / cover"
#endif
elements.loadingBar.style.display = "block"

function OnQuit() {
    ShowPopup('The game has quitted!', 'info')
}

function OnScriptLoaded() {
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
        unityInstance.Module.onQuit = OnQuit
    })
    .catch(alert)
}

const script = document.createElement("script")
script.src = loaderUrl
script.onload = OnScriptLoaded
document.body.appendChild(script)
