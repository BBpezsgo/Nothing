const http = require('http')
const Path = require('path')
const fs = require('fs')
const mime = require('mime')
const Handlebars = require('handlebars')
const Config = require('./config-loader').Load()
const FileSender = require('./file-sender')

/**
 * @param {http.IncomingMessage} req
 * @param {http.ServerResponse} res
 */
function onRequest(req, res) {
    console.log(` << ${req.socket.remoteAddress} HTTP ${req.method} ${req.url}`)

    let path = decodeURI(req.url ?? '/')

    if (path === '/') { path = '/index.html' }

    if (path === '/assets.bin') {
        if (req.method !== 'GET') {
            res.writeHead(405)
            res.end()
            return
        }

        handlePackedAssetRequest(req, res, path)
        return
    }

    if (path.startsWith('/assets')) {
        if (req.method !== 'GET') {
            res.writeHead(405)
            res.end()
            return
        }
        
        handleAssetRequest(req, res, path)
        return
    }

    if (path.startsWith('/storage')) {        
        handleStorageRequest(req, res, path)
        return
    }

    const serverPath = Path.join(__dirname, 'web', 'app', path)
    if (fs.existsSync(serverPath)) {
        if (req.method !== 'GET') {
            res.writeHead(405)
            res.end()
            return
        }

        if (!fs.lstatSync(serverPath).isFile()) {
            res.writeHead(404)
            res.end()
            return
        }

        FileSender.sendFile(res, serverPath)
        return
    }
    
    const unityGamePath = Path.join(Config.path, path)
    if (fs.existsSync(unityGamePath)) {
        if (req.method !== 'GET') {
            res.writeHead(405)
            res.end()
            return
        }

        if (!fs.lstatSync(unityGamePath).isFile()) {
            res.writeHead(404)
            res.end()
            return
        }

        FileSender.sendFile(res, unityGamePath)
        return
    }

    if (req.method !== 'GET') {
        res.writeHead(405)
        res.end()
        return
    }
    
    console.error(`Path "${path}" not found`)
    
    FileSender.sendError(res, {
        code: 404,
        message: '"' + encodeURI(path) + '" not found',
        details: [
            { key: 'Path', value: path },
            { key: 'ServerPath', value: serverPath },
            { key: 'GamePath', value: unityGamePath },
        ]
    })
}

/**
 * @param {http.IncomingMessage} req
 * @param {http.ServerResponse} res
 * @param {string} path
 */
function handlePackedAssetRequest(req, res, path) {
    const assetsPath = Config.assets + '.bin'

    if (!fs.existsSync(assetsPath)) {
        console.error(`The packed asset path "${assetsPath}" not found!`)

        res.writeHead(404)
        res.end()
        return
    }

    console.error(`Sending file "${assetsPath}"`)
    FileSender.sendFile(res, assetsPath)
}

/**
 * @param {http.IncomingMessage} req
 * @param {http.ServerResponse} res
 * @param {string} path
 */
function handleAssetRequest(req, res, path) {
    path = path.replace('/assets', '')
    const assetsPath = Path.join(Config.assets, path)

    if (!assetsPath.replace(/\//g, '\\').startsWith(Config.assets.replace(/\//g, '\\'))) {
        console.error(`Client tried to access "${assetsPath}"`)

        res.writeHead(403)
        res.end()
        return
    }

    if (!fs.existsSync(assetsPath)) {
        console.error(`Path "${assetsPath}" does not exists`)

        res.writeHead(404)
        res.end()
        return
    }

    if (fs.lstatSync(assetsPath).isDirectory()) {
        console.error(`Sendind directory "${assetsPath}"`)

        const contents = fs.readdirSync(assetsPath)
        res.setHeader('Content-Type', 'text/plain')
        res.writeHead(200)
        res.write(contents.join('\n'))
        res.end()
        return
    }

    if (fs.lstatSync(assetsPath).isFile()) {
        console.error(`Sendind file "${assetsPath}"`)

        FileSender.sendFile(res, assetsPath)
        return
    }

    console.error(`Unknown thing "${assetsPath}"`)

    res.writeHead(400)
    res.end()
}

/**
 * @param {http.IncomingMessage} req
 * @param {http.ServerResponse} res
 * @param {string} path
 */
function handleStorageRequest(req, res, path) {
    path = path.replace('/storage', '')

    if (!path.includes('/')) {
        res.write('Specify a session id! ie.: /storage/8008135/')
        res.writeHead(400)
        res.end()
        return
    }
    const sessionID = (path.startsWith('/') ? path.substring(1) : path).split('/')[0]

    const sessionFolder = Path.join(Config.modifiable, sessionID)

    if (!sessionFolder.replace(/\//g, '\\').startsWith(Config.modifiable.replace(/\//g, '\\') + '\\' + sessionID) || !sessionFolder.replace(/\//g, '\\').startsWith(sessionFolder)) {
        console.error(`Client tried to access "${sessionFolder}"`)

        res.writeHead(403)
        res.end()
        return
    }

    if (!fs.existsSync(sessionFolder)) fs.mkdirSync(sessionFolder, { recursive: true })

    const modifiablePath = Path.join(Config.modifiable, path)

    if (req.method !== 'GET' && req.method !== 'PUT') {
        res.writeHead(405)
        res.end()
        return
    }

    if (!modifiablePath.replace(/\//g, '\\').startsWith(sessionFolder.replace(/\//g, '\\'))) {
        console.error(`Client tried to access "${modifiablePath}"`)

        res.writeHead(403)
        res.end()
        return
    }

    if (req.method === 'PUT') {
        if (Path.extname(modifiablePath) === '') {
            console.log(`Create directory "${modifiablePath}"`)
            fs.mkdirSync(modifiablePath, { recursive: true })

            res.writeHead(200)
            res.end()
            return
        } else {
            console.log(`Create file "${modifiablePath}"`)

            fs.writeFileSync(modifiablePath, '')
            req.pipe(fs.createWriteStream(modifiablePath))
            req.on('end', () => {
                console.log(`File "${modifiablePath}" has been written`)
            })

            res.writeHead(200)
            res.end()
            return
        }
    }

    if (!fs.existsSync(modifiablePath)) {
        console.error(`Path "${modifiablePath}" does not exists`)

        res.writeHead(404)
        res.end()
        return
    }

    if (fs.lstatSync(modifiablePath).isDirectory()) {
        console.error(`Sendind directory "${modifiablePath}"`)

        const contents = fs.readdirSync(modifiablePath)
        res.setHeader('Content-Type', 'text/plain')
        res.writeHead(200)
        res.write(contents.join('\n'))
        res.end()
        return
    }

    if (fs.lstatSync(modifiablePath).isFile()) {
        console.error(`Sendind file "${modifiablePath}"`)

        FileSender.sendFile(res, modifiablePath)
        return
    }

    console.error(`Unknown thing "${modifiablePath}"`)

    res.writeHead(400)
    res.end()
}

function onListening() {
    const address = server.address()
    if (!address) {
        console.log('Listening')
    } else if (typeof address === 'string') {
        console.log(`Listening on http://${address}`)
    } else {
        console.log(`Listening on http://${address.address}:${address.port}`)
    }
}

const server = http.createServer((req, res) => {
    onRequest(req, res)
    console.log(` >> ${res.statusCode}\r\n`)
})
server.listen(7777, '0.0.0.0', onListening)
