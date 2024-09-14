const http = require('http')
const Path = require('path')
const fs = require('fs')
const mime = require('mime')
const Handlebars = require('handlebars')

/**
 * @param {http.ServerResponse} res
 * @param {{
*   code: number,
*   message: string,
*   details?: Array<{ key: string, value: string }>
* }} error
*/
function sendError(res, error) {
    res.writeHead(error.code, error.message)
    res.write(Handlebars.compile(fs.readFileSync(Path.join(__dirname, '/web/errors/error.hbs'), 'utf-8'))(error))
    res.end()
}

/**
* @param {http.ServerResponse} res
* @param {string} path
*/
function sendFile(res, path) {
    let _path = path
    const filepath = _path

    let encoding = null

    if (_path.toLowerCase().endsWith('.br')) {
        encoding = 'br'
        _path = _path.substring(0, _path.length - 3)
    }

    // @ts-ignore
    const mimeType = mime.getType(_path)

    if (encoding) res.setHeader('Content-Encoding', encoding)
    if (mimeType) res.setHeader('Content-Type', mimeType)

    res.writeHead(200)
    fs.createReadStream(filepath).pipe(res)
}

module.exports = { sendError, sendFile }