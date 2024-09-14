const Path = require('path')
const fs = require('fs')

if (!fs.existsSync(Path.join(__dirname + '/config.json'))) {
    const error =`Config file ${Path.join(__dirname + '/config.json')} does not exists!`
    console.error(error)
    throw new Error(error)
}

/** @returns {import('./config.json')} */
function Load() {
    return JSON.parse(fs.readFileSync(Path.join(__dirname + '/config.json'), 'utf-8'))
}

module.exports = { Load }