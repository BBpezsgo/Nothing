/** @returns {import("./unity").PreprocessorVariables} */
function UnityPreprocessorVariables() {
	const COMPANY_NAME = {{{ JSON.stringify(COMPANY_NAME) }}}
	const PRODUCT_NAME = {{{ JSON.stringify(PRODUCT_NAME) }}}
	const PRODUCT_VERSION = {{{ JSON.stringify(PRODUCT_VERSION) }}}
	const WIDTH = {{{ JSON.stringify(WIDTH) }}}
	const HEIGHT = {{{ HEIGHT }}}
	const SPLASH_SCREEN_STYLE = {{{ JSON.stringify(SPLASH_SCREEN_STYLE) }}}
	const BACKGROUND_COLOR = {{{ JSON.stringify(BACKGROUND_COLOR) }}}
	const UNITY_VERSION = {{{ JSON.stringify(UNITY_VERSION) }}}
	const DEVELOPMENT_PLAYER = {{{ JSON.stringify(DEVELOPMENT_PLAYER) }}}
	const DECOMPRESSION_FALLBACK = {{{ JSON.stringify(DECOMPRESSION_FALLBACK) }}}
	const USE_WASM = {{{ JSON.stringify(USE_WASM) }}}
	const USE_THREADS = {{{ JSON.stringify(USE_THREADS) }}}
	const USE_WEBGL_1_0 = {{{ JSON.stringify(USE_WEBGL_1_0) }}}
	const USE_WEBGL_2_0 = {{{ JSON.stringify(USE_WEBGL_2_0) }}}
	const USE_DATA_CACHING = {{{ JSON.stringify(USE_DATA_CACHING) }}}
	const LOADER_FILENAME = {{{ JSON.stringify(LOADER_FILENAME) }}}
	const DATA_FILENAME = {{{ JSON.stringify(DATA_FILENAME) }}}
	const FRAMEWORK_FILENAME = {{{ JSON.stringify(FRAMEWORK_FILENAME) }}}
	const CODE_FILENAME = {{{ JSON.stringify(CODE_FILENAME) }}}
	const MEMORY_FILENAME = {{{ JSON.stringify(MEMORY_FILENAME) }}}
	const SYMBOLS_FILENAME = {{{ JSON.stringify(SYMBOLS_FILENAME) }}}
	const BACKGROUND_FILENAME = {{{ JSON.stringify(BACKGROUND_FILENAME) }}}

	return {
		/**
		  * The Company Name defined in the Player Settings.
		  * @type {string}
		  */
		COMPANY_NAME: COMPANY_NAME,

		/**
		  * The Product Name defined in the Player Settings.
		  * @type {string}
		  */
		PRODUCT_NAME: PRODUCT_NAME,

		/**
		  * The Version defined in the Player Settings.
		  * @type {string}
		  */
		PRODUCT_VERSION: PRODUCT_VERSION,

		/**
		  * The Default Canvas Width defined in the Player Settings > Resolution and Presentation.
		  * @type {number}
		  */
		WIDTH: WIDTH,

		/**
		  * The Default Canvas Height in the Player Settings > Resolution and Presentation.
		  * @type {number}
		  */
		HEIGHT: HEIGHT,

		/**
		  * This's set to the "Dark" value when Splash Style Player Settings > Splash Image is set to Light on Dark, otherwise it's set to the "Light" value.
		  * @type {"Dark" | "Light"}
		  */
		SPLASH_SCREEN_STYLE: SPLASH_SCREEN_STYLE,

		/**
		  * Represents the Background Color defined in a form of a hex triplet.
		  * @type {string}
		  */
		BACKGROUND_COLOR: BACKGROUND_COLOR,

		/**
		  * The Unity version.
		  * @type {string}
		  */
		UNITY_VERSION: UNITY_VERSION,

		/**
		  * This's set to true if the Development Build option is enabled.
		  * @type {boolean}
		  */
		DEVELOPMENT_PLAYER: DEVELOPMENT_PLAYER,

		/**
		  * This's set to Gzip or Brotli, depending on the compression method you use and the type of decompressor included in the build.
		  * If neither is included, the variable is set to an empty string.
		  * @type {"Gzip" | "Brotli" | ""}
		  */
		DECOMPRESSION_FALLBACK: DECOMPRESSION_FALLBACK,

		/**
		  * This is set to true if the current build is a WebAssembly build.
		  * @type {boolean}
		  */
		USE_WASM: USE_WASM,

		/**
		  * This is set to true if the current build uses threads.
		  * @type {boolean}
		  */
		USE_THREADS: USE_THREADS,

		/**
		  * This is set to true if the current build supports the WebGL1.0 graphics API.
		  * @type {boolean}
		  */
		USE_WEBGL_1_0: USE_WEBGL_1_0,

		/**
		  * This is set to true if the current build supports the WebGL2.0 graphics API.
		  * @type {boolean}
		  */
		USE_WEBGL_2_0: USE_WEBGL_2_0,

		/**
		  * This is set to true if the current build uses indexedDB caching for the downloaded files.
		  * @type {boolean}
		  */
		USE_DATA_CACHING: USE_DATA_CACHING,

		/**
		  * This is set to the filename of the build loader script.
		  * @type {string}
		  */
		LOADER_FILENAME: LOADER_FILENAME,

		/**
		  * This is set to the filename of the main data file.
		  * @type {string}
		  */
		DATA_FILENAME: DATA_FILENAME,

		/**
		  * This is set to the filename of the build framework script.
		  * @type {string}
		  */
		FRAMEWORK_FILENAME: FRAMEWORK_FILENAME,

		/**
		  * This is set to the filename of the WebAssembly module when the current build is a WebAssembly build, otherwise it's set to the filename of the asm.js module.
		  * @type {string}
		  */
		CODE_FILENAME: CODE_FILENAME,

		/**
		  * This is set to the filename of the memory file when memory is stored in an external file, otherwise it's set to an empty string.
		  * @type {string}
		  */
		MEMORY_FILENAME: MEMORY_FILENAME,

		/**
		  * This is set to the filename of the JSON file containing debug symbols when the current build is using debug symbols, otherwise it's set to an empty string.
		  * @type {string}
		  */
		SYMBOLS_FILENAME: SYMBOLS_FILENAME,

		/**
		  * This is set to the filename of the background image when the background image is selected in the Player Settings > Splash Image, otherwise it's set to an empty string.
		  * @type {string}
		  */
		BACKGROUND_FILENAME: BACKGROUND_FILENAME,
	}
}