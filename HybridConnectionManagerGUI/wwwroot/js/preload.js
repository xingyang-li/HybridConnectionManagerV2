const { contextBridge, clipboard } = require('electron')

contextBridge.exposeInMainWorld('electronAPI', {
    copyToClipboard: (text) => clipboard.writeText(text)
})