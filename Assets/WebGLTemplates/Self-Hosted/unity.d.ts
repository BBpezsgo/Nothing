export type PreprocessorVariables = {
	/**
	 * The Company Name defined in the Player Settings.
	 */
	COMPANY_NAME: string

	/**
	 * The Product Name defined in the Player Settings.
	 */
	PRODUCT_NAME: string

	/**
	 * The Version defined in the Player Settings.
	 */
	PRODUCT_VERSION: string

	/**
	 * The Default Canvas Width defined in the Player Settings > Resolution and Presentation.
	 */
	WIDTH: number

	/**
	 * The Default Canvas Height in the Player Settings > Resolution and Presentation.
	 */
	HEIGHT: number

	/**
	 * This's set to the "Dark" value when Splash Style Player Settings > Splash Image is set to Light on Dark, otherwise it's set to the "Light" value.
	 */
	SPLASH_SCREEN_STYLE: 'Dark' | 'Light'

	/**
	 * Represents the Background Color defined in a form of a hex triplet.
	 */
	BACKGROUND_COLOR: string

	/**
	 * The Unity version.
	 */
	UNITY_VERSION: string

	/**
	 * This's set to true if the Development Build option is enabled.
	 */
	DEVELOPMENT_PLAYER: boolean

	/**
	 * This's set to Gzip or Brotli, depending on the compression method you use and the type of decompressor included in the build.
	 * If neither is included, the variable is set to an empty string.
	 */
	DECOMPRESSION_FALLBACK: 'Gzip' | 'Brotli' | ''

	/**
	 * This is set to true if the current build is a WebAssembly build.
	 */
	USE_WASM: boolean

	/**
	 * This is set to true if the current build uses threads.
	 */
	USE_THREADS: boolean

	/**
	 * This is set to true if the current build supports the WebGL1.0 graphics API.
	 */
	USE_WEBGL_1_0: boolean

	/**
	 * This is set to true if the current build supports the WebGL2.0 graphics API.
	 */
	USE_WEBGL_2_0: boolean

	/**
	 * This is set to true if the current build uses indexedDB caching for the downloaded files.
	 */
	USE_DATA_CACHING: boolean

	/**
	 * This is set to the filename of the build loader script.
	 */
	LOADER_FILENAME: string

	/**
	 * This is set to the filename of the main data file.
	 */
	DATA_FILENAME: string

	/**
	 * This is set to the filename of the build framework script.
	 */
	FRAMEWORK_FILENAME: string

	/**
	 * This is set to the filename of the WebAssembly module when the current build is a WebAssembly build, otherwise it's set to the filename of the asm.js module.
	 */
	CODE_FILENAME: string

	/**
	 * This is set to the filename of the memory file when memory is stored in an external file, otherwise it's set to an empty string.
	 */
	MEMORY_FILENAME: string

	/**
	 * This is set to the filename of the JSON file containing debug symbols when the current build is using debug symbols, otherwise it's set to an empty string.
	 */
	SYMBOLS_FILENAME: string

	/**
	 * This is set to the filename of the background image when the background image is selected in the Player Settings > Splash Image, otherwise it's set to an empty string.
	*/
	BACKGROUND_FILENAME: string
}

export function UnityPreprocessorVariables(): PreprocessorVariables

declare global {
    /**
     * @param canvas Unity runtime uses the `canvas` object to render the game.
     * @param config The config object contains the build configuration, such as the code and data URLs, product and company name, and version.
     * @param onProgress The WebGL loader calls the onProgress callback object every time the download progress updates.The progress argument that comes with the onProgress callback determines the loading progress as a value between 0.0 and 1.0.
     */
    function createUnityInstance(canvas: HTMLCanvasElement, config: UnityBuildConfig, onProgress: (progress: number) => void): Promise<UnityInstance>
}

export type UnityBuildConfig = {
    dataUrl: string
    frameworkUrl: string
    codeUrl?: string
    memoryUrl?: string
    symbolsUrl?: string
    streamingAssetsUrl?: string
    companyName: string
    productName: string
    productVersion: string
    showBanner: (msg: string, type: 'error' | 'warning' | 'info') => void
}

export interface UnityInstance {
    SetFullscreen(enabled: 1 | 0): void
    Quit(): Promise<any>
    SendMessage(...args: Array<any>): undefined
    readonly Module: UnityModule
}

export interface UnityModule {
    // #region Internal
    
    AL(): unknown
    AsciiToString(): unknown
    Browser(): unknown
    CatchInfo(): unknown
    DNS(): unknown
    EGL(): unknown
    ENV(): unknown
    ERRNO_CODES(): unknown
    ERRNO_MESSAGES(): unknown
    ExceptionInfo(): unknown
    ExceptionInfoAttrs(): unknown
    FS(): unknown
    FS_createDataFile(parent, name, data, canRead, canWrite, canOwn): unknown
    FS_createDevice(): unknown
    FS_createFolder(): unknown
    FS_createLazyFile(): unknown
    FS_createLink(): unknown
    FS_createPath(parent, path, canRead, canWrite): undefined
    FS_createPreloadedFile(): unknown
    FS_unlink(): unknown
    GAI_ERRNO_MESSAGES(): unknown
    GL(): unknown
    GLEW(): unknown
    GLFW(): unknown
    GLFW_Window(): unknown
    GLUT(): unknown
    HEAP8: Int8Array
    HEAP16: Int16Array
    HEAP32: Int32Array
    HEAPF32: Float32Array
    HEAPF64: Float64Array
    HEAPU8: Uint8Array
    HEAPU16: Uint16Array
    HEAPU32: Uint32Array
    IDBFS(): unknown
    IDBStore(): unknown
    JSEvents(): unknown
    JS_Accelerometer(): unknown
    JS_Accelerometer_callback(): unknown
    JS_Accelerometer_eventHandler(): unknown
    JS_Accelerometer_frequency(): unknown
    JS_Accelerometer_frequencyRequest(): unknown
    JS_Accelerometer_lastValue(): unknown
    JS_Accelerometer_multiplier(): unknown
    JS_ComputeGravity(): unknown
    JS_DefineAccelerometerMultiplier(): unknown
    JS_DeviceMotion_add(): unknown
    JS_DeviceMotion_eventHandler(): unknown
    JS_DeviceMotion_remove(): unknown
    JS_DeviceOrientation_eventHandler(): unknown
    JS_DeviceSensorPermissions(): unknown
    JS_GravitySensor(): unknown
    JS_GravitySensor_callback(): unknown
    JS_GravitySensor_eventHandler(): unknown
    JS_GravitySensor_frequencyRequest(): unknown
    JS_Gyroscope(): unknown
    JS_Gyroscope_callback(): unknown
    JS_Gyroscope_eventHandler(): unknown
    JS_Gyroscope_frequencyRequest(): unknown
    JS_LinearAccelerationSensor(): unknown
    JS_LinearAccelerationSensor_callback(): unknown
    JS_LinearAccelerationSensor_eventHandler(): unknown
    JS_LinearAccelerationSensor_frequency(): unknown
    JS_LinearAccelerationSensor_frequencyRequest(): unknown
    JS_OrientationSensor(): unknown
    JS_OrientationSensor_callback(): unknown
    JS_OrientationSensor_eventHandler(): unknown
    JS_OrientationSensor_frequencyRequest(): unknown
    JS_RequestDeviceSensorPermissions(): unknown
    JS_ScreenOrientation_appliedLockType(): unknown
    JS_ScreenOrientation_callback(): unknown
    JS_ScreenOrientation_eventHandler(): unknown
    JS_ScreenOrientation_requestedLockType(): unknown
    JS_ScreenOrientation_timeoutID(): unknown
    MEMFS(): unknown
    PATH(): unknown
    PATH_FS(): unknown
    PIPEFS(): unknown
    Pointer_stringify(s, len): unknown
    Protocols(): unknown
    QuitCleanup(): void
    SDL(): unknown
    SDL_audio(): unknown
    SDL_gfx(): unknown
    SDL_ttfContext(): unknown
    SDL_unicode(): unknown
    SOCKFS(): unknown
    SYSCALLS(): unknown
    SendMessage(gameObject, func, param): unknown
    SetFullscreen(fullscreen): unknown
    Sockets(): unknown
    SystemInfo: {
        width: number
        height: number
        userAgent: string
        browser: 'Firefox' | 'Opera' | 'Edge' | 'Samsung Browser' | 'Internet Explorer' | 'Internet Explorer' | 'Chrome' | 'Chrome on iOS Safari' | 'Firefox on iOS Safari' | 'Safari' | 'Unknown browser'
        browserVersion: string | 'Unknown version'
        mobile: boolean
        gpu: string | 'Unknown GPU'
        hasCursorLock: boolean
        hasFullscreen: boolean
        hasThreads: boolean
        hasWasm: boolean
        hasWasmThreads: false
        hasWebGL: number
        language: string
        os: 'Windows' | 'Android' | 'iPhoneOS' | 'iPadOS' | 'FreeBSD' | 'OpenBSD' | 'Linux' | 'MacOS' | 'Search Bot' | 'Unknown OS'
        osVersion: string | 'Unknown OS Version'
    }
    TTY(): unknown
    UNETWebSocketsInstances(): unknown
    UNWIND_CACHE(): unknown
    UTF8ArrayToString(): unknown
    UTF8ToString(): unknown
    UTF16ToString(): unknown
    UTF32ToString(): unknown
    UnityCache: typeof UnityCache
    WEBAudio(): unknown
    abort(): unknown
    abortHandler(message: any): boolean
    activeWebCams(): unknown
    addFunction(): unknown
    addOnExit(): unknown
    addOnInit(): unknown
    addOnPostRun(): unknown
    addOnPreMain(): unknown
    addOnPreRun(): unknown
    addRunDependency(id): unknown
    alignFunctionTables(): unknown
    allocate(): unknown
    allocateUTF8(): unknown
    allocateUTF8OnStack(): unknown
    asm: object
    asmjsMangle(): unknown
    autoResumeAudioContext(): unknown
    battery(): unknown
    cacheControl(url: any): 'must-revalidate' | 'no-store'
    cachedFetch(resource: string | { url: any }, init: { control: any, company: any, product: any }): unknown
    callMain(): unknown
    callRuntimeCallbacks(): unknown
    callUserCallback(): unknown
    calledRun: true
    cameraAccess(): unknown
    canvas: HTMLCanvasElement
    ccall(ident, returnType, argTypes, args, opts): unknown
    checkStackCookie(): unknown
    checkWasiClock(): unknown
    clearInterval(id: number): void
    codeUrl: string
    companyName: string
    computeUnpackAlignedImageSize(): unknown
    convertI32PairToI53(): unknown
    convertU32PairToI53(): unknown
    createContext(canvas, useWebGL, setInModule, webGLContextAttributes): unknown
    ctx: object
    currentFullscreenStrategy(): unknown
    cwrap(ident, returnType, argTypes, opts): unknown
    dataUrl: string
    deinitializers: Array<((() => void) | unknown)>
    demangle(): unknown
    demangleAll(): unknown
    disableAccessToMediaDevices(): unknown
    disabledCanvasEvents: Array<string>
    doRequestFullscreen(): unknown
    downloadProgress: {} | { dataUrl: object }
    emscriptenWebGLGet(): unknown
    emscriptenWebGLGetBufferBinding(): unknown
    emscriptenWebGLGetIndexed(): unknown
    emscriptenWebGLGetTexPixelData(): unknown
    emscriptenWebGLGetUniform(): unknown
    emscriptenWebGLGetVertexAttrib(): unknown
    emscriptenWebGLValidateMapBufferTarget(): unknown
    emscripten_realloc_buffer(): unknown
    exceptionCaught(): unknown
    exceptionLast(): unknown
    exception_addRef(): unknown
    exception_decRef(): unknown
    fetchWithProgress(resource: URL | RequestInfo, init?: RequestInit): Promise<any>
    fillBatteryEventData(): unknown
    fillDeviceMotionEventData(): unknown
    fillDeviceOrientationEventData(): unknown
    fillFullscreenChangeEventData(): unknown
    fillGamepadEventData(): unknown
    fillMouseEventData(): unknown
    fillOrientationChangeEventData(): unknown
    fillPointerlockChangeEventData(): unknown
    fillVisibilityChangeEventData(): unknown
    findCanvasEventTarget(): unknown
    findEventTarget(): unknown
    find_closing_parens_index(): unknown
    formatString(): unknown
    frameworkUrl: string
    fs(): unknown
    funcWrappers(): unknown
    getBoundingClientRect(): unknown
    getCanvasElementSize(): unknown
    getCompilerSetting(): unknown
    getDynCaller(): unknown
    getEnvStrings(): unknown
    getExecutableName(): unknown
    getFuncWrapper(): unknown
    getFunctionTables(): unknown
    getHostByName(): unknown
    getLEB(): unknown
    getRandomDevice(): unknown
    getSocketAddress(): unknown
    getSocketFromFD(): unknown
    getTempRet0(): unknown
    getUserMedia(): unknown
    getValue(): unknown
    hasSRGBATextures(): unknown
    heapAccessShiftForWebGLHeap(): unknown
    heapObjectForWebGLType(): unknown
    hideEverythingExceptGivenElement(): unknown
    inetNtop4(): unknown
    inetNtop6(): unknown
    inetPton4(): unknown
    inetPton6(): unknown
    intArrayFromString(): unknown
    intArrayToString(): unknown
    intervals: object
    jsAudioAddPendingBlockedAudio(): unknown
    jsAudioCreateChannel(): unknown
    jsAudioCreateCompressedSoundClip(): unknown
    jsAudioCreateUncompressedSoundClip(): unknown
    jsAudioCreateUncompressedSoundClipFromCompressedAudio(): unknown
    jsAudioCreateUncompressedSoundClipFromPCM(): unknown
    jsAudioGetMimeTypeFromType(): unknown
    jsAudioMixinSetPitch(): unknown
    jsAudioPlayBlockedAudios(): unknown
    jsAudioPlayPendingBlockedAudio(): unknown
    jsDomCssEscapeId(): unknown
    jsStackTrace(): unknown
    jsSupportedVideoFormats(): unknown
    jsUnsupportedVideoFormats(): unknown
    jsVideoAddPendingBlockedVideo(): unknown
    jsVideoAllAudioTracksAreDisabled(): unknown
    jsVideoAttemptToPlayBlockedVideos(): unknown
    jsVideoEnded(): unknown
    jsVideoPendingBlockedVideos(): unknown
    jsVideoPlayPendingBlockedVideo(): unknown
    jsVideoRemovePendingBlockedVideo(): unknown
    jsWebRequestGetResponseHeaderString(): unknown
    jstoi_q(): unknown
    jstoi_s(): unknown
    keepRuntimeAlive(): unknown
    lengthBytesUTF8(): unknown
    lengthBytesUTF16(): unknown
    lengthBytesUTF32(): unknown
    listenOnce(): unknown
    locateFile(url: any): any
    mainThreadEM_ASM(): unknown
    maybeCStringToJsString(): unknown
    maybeExit(): unknown
    miniTempWebGLFloatBuffers(): unknown
    mmapAlloc(): unknown
    pauseMainLoop(): unknown
    polyfillSetImmediate(): unknown
    postRun: Array<unknown>
    preRun: Array<unknown>
    preloadedAudios: object
    preloadedImages: object
    preprocess_c_code(): unknown
    prettyPrint(): unknown
    print(message: any): void
    printErr(message: any): void
    productName: string
    productVersion: string
    reSign(): unknown
    readAsmConstArgs(): unknown
    readAsmConstArgsArray(): unknown
    readBodyWithProgress(response: Response, onProgress: (arg0: { type: string; response: any; total: any; loaded: any; lengthComputable: boolean; chunk: any; }) => void, enableStreaming: boolean): any
    readI53FromI64(): unknown
    readI53FromU64(): unknown
    readSockaddr(): unknown
    reallyNegative(): unknown
    registerBatteryEventCallback(): unknown
    registerBeforeUnloadEventCallback(): unknown
    registerDeviceMotionEventCallback(): unknown
    registerDeviceOrientationEventCallback(): unknown
    registerFocusEventCallback(): unknown
    registerFullscreenChangeEventCallback(): unknown
    registerFunctions(): unknown
    registerGamepadEventCallback(): unknown
    registerKeyEventCallback(): unknown
    registerMouseEventCallback(): unknown
    registerOrientationChangeEventCallback(): unknown
    registerPointerlockChangeEventCallback(): unknown
    registerPointerlockErrorEventCallback(): unknown
    registerRestoreOldStyle(): unknown
    registerTouchEventCallback(): unknown
    registerUiEventCallback(): unknown
    registerVisibilityChangeEventCallback(): unknown
    registerWheelEventCallback(): unknown
    removeFunction(): unknown
    removeRunDependency(id): unknown
    remove_cpp_comments_in_shaders(): unknown
    requestAnimationFrame(func): unknown
    requestFullScreen(): unknown
    requestFullscreen(lockPointer, resizeCanvas): unknown
    requestPointerLock(): unknown
    restoreHiddenElements(): unknown
    restoreOldWindowedStyle(): unknown
    resumeMainLoop(): unknown
    run(args): unknown
    runAndAbortIfError(): unknown
    runtimeKeepaliveCounter(): unknown
    runtimeKeepalivePop(): unknown
    runtimeKeepalivePush(): unknown
    screenOrientation(): unknown
    setCanvasElementSize(): unknown
    setCanvasSize(width, height, noUpdates): unknown
    setErrNo(): unknown
    setFileTime(): unknown
    setInterval(func: TimerHandler, ms: number | undefined): number
    setLetterbox(): unknown
    setMainLoop(): unknown
    setTempRet0(): unknown
    setValue(): unknown
    softFullscreenResizeWebGLRenderTarget(): unknown
    specialHTMLTargets(): unknown
    stackAlloc(): unknown
    stackRestore(): unknown
    stackSave(): unknown
    stackTrace(): unknown
    stackTraceRegExp: RegExp
    stderr: undefined
    stdin: undefined
    stdout: undefined
    streamingAssetsUrl: string
    stringToAscii(): unknown
    stringToNewUTF8(): unknown
    stringToUTF8(): unknown
    stringToUTF8Array(): unknown
    stringToUTF16(): unknown
    stringToUTF32(): unknown
    syscallMmap2(): unknown
    syscallMunmap(): unknown
    tempFixedLengthArray(): unknown
    traverseStack(): unknown
    unSign(): unknown
    uncaughtExceptionCount(): unknown
    videoInstanceIdCounter(): unknown
    videoInstances(): unknown
    warnOnce(): unknown
    webSocketInstances(): unknown
    webglApplyExplicitProgramBindings(): unknown
    webglContextAttributes: { preserveDrawingBuffer: boolean, powerPreference: number }
    webglGetUniformLocation(): unknown
    websocket: object
    withBuiltinMalloc(): unknown
    wr(): unknown
    writeArrayToMemory(): unknown
    writeAsciiToMemory(): unknown
    writeGLArray(): unknown
    writeI53ToI64(): unknown
    writeI53ToI64Clamped(): unknown
    writeI53ToI64Signaling(): unknown
    writeI53ToU64Clamped(): unknown
    writeI53ToU64Signaling(): unknown
    writeSockaddr(): unknown
    writeStackCookie(): unknown
    writeStringToMemory(): unknown
    _InjectProfilerSample(): unknown
    _SendMessage(): unknown
    _SendMessageFloat(): unknown
    _SendMessageString(): unknown
    _SetFullscreen(): unknown
    ___cxa_can_catch(): unknown
    ___cxa_demangle(): unknown
    ___cxa_is_pointer_type(): unknown
    ___errno_location(): unknown
    ___wasm_call_ctors(): unknown
    __get_daylight(): unknown
    __get_timezone(): unknown
    __get_tzname(): unknown
    _emscripten_main_thread_process_queued_calls(): unknown
    _emscripten_stack_get_end(): unknown
    _emscripten_stack_get_free(): unknown
    _emscripten_stack_init(): unknown
    _emscripten_webgl_get_current_context(): unknown
    _emscripten_webgl_make_context_current(contextHandle): unknown
    _fflush(): unknown
    _free(): unknown
    _htonl(): unknown
    _htons(): unknown
    _main(): unknown
    _malloc(): unknown
    _memalign(): unknown
    _memset(): unknown
    _ntohs(): unknown
    _setNetworkCallback(): unknown
    _setThrew(): unknown
    _strlen(): unknown
    get ALLOC_NORMAL(): unknown
    get ALLOC_STACK(): unknown
    get INITIAL_MEMORY(): unknown
    get arguments(): unknown
    get noExitRuntime(): unknown
    get quit(): unknown
    get read(): unknown
    get readAsync(): unknown
    get readBinary(): unknown
    get setWindowTitle(): unknown
    get thisProgram(): unknown
    get wasmBinary(): unknown

    readonly shouldQuit: boolean

    // #endregion

    // #region User
    
    onQuit?: () => void

    Quit(): void
    SendMessage(): void
    SetFullscreen(): void

    // #endregion
}

/**
 * A request cache that uses the browser Index DB to cache large requests
 */
declare class UnityCache {
    isConnected: Promise<void>
    openDBTimeout: null
    database: null | any

    /**
     * Name and version of unity cache database
     */
    UnityCacheDatabase: {
        name: string
        version: number
    }
    /**
     * Name and version of request store database
     */
    RequestStore: {
        name: string
        version: number
    }
    /**
     * Name and version of web assembly store database
     */
    WebAssemblyStore: {
        name: string
        version: number
    }

    /**
     * Singleton accessor. Returns unity cache instance
     */
    static getInstance(): UnityCache

    /**
     * Destroy unity cache instance. Returns a promise that waits for the
     * database connection to be closed.
     */
    static destroyInstance(): Promise<void>

    /**
     * Clear the unity cache. 
     * @returns A promise that resolves when the cache is cleared.
     */
    static clearCache(): Promise<void>


    /**
     * Execute an operation on the cache
     * @param store The name of the store to use
     * @param operation The operation to to execute on the cache
     * @param parameters Parameters for the operation
     * @returns A promise to the cache entry
     */
    execute(store: string, operation: string, parameters: Array<any>): Promise<any>

    /**
     * Load a request from the cache.
     * @param url The url of the request 
     * @returns A promise that resolves to the cached result or null if request is not in cache.
     */
    loadRequest(url: string): Promise<Object>

    /**
     * Store a request in the cache
     * @param request The request to store
     * @returns A promise that resolves when the request is stored in the cache.
     */
    storeRequest(request: Object): Promise<void>

    /**
     * Close database connection.
     */
    close(): Promise<any>
}

declare function unityFramework(module: UnityModule, ...args: Array<any>): void
