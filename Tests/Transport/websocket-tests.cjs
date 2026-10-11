const fs = require('fs'); const vm = require('vm'); const assert = require('assert');
const path = require('path');
const library = {}; const calls = []; const sockets = [];
class Socket {
  constructor(url) { this.url = url; this.readyState = 0; this.sent = []; sockets.push(this); }
  send(data) { this.sent.push(Buffer.from(data)); }
  close() { this.readyState = 3; if (this.onclose) this.onclose(); }
  open() { this.readyState = 1; this.onopen(); }
  message(bytes) { this.onmessage({data: Uint8Array.from(bytes).buffer}); }
}
const context = { LibraryManager: {library}, mergeInto: Object.assign, WebSocket: Socket,
  window: {location: {protocol: 'https:', host: 'game.example'}},
  UTF8ToString: x => x, SendMessage: (...args) => calls.push(args),
  TextEncoder, Uint8Array, HEAPU8: Uint8Array.from([7,8,9]), btoa: s => Buffer.from(s,'binary').toString('base64') };
vm.createContext(context);
vm.runInContext(fs.readFileSync(path.join(__dirname,'../../Assets/Plugins/WebGL/WebSocket.jslib'),'utf8'),context);
for (const key of Object.keys(library)) if (key.startsWith('$')) context[key.slice(1)] = library[key];
library.NroWSOpen(1, 'Bridge', 'localhost', 14445);
assert.equal(sockets.length,1,'open creates websocket'); assert.equal(sockets[0].url,'wss://game.example/gateway');
sockets[0].open(); assert.equal(sockets[0].sent[0].toString(),'localhost:14445','first binary frame is gateway route');
assert.deepEqual(calls.shift(),['Bridge','OnWebSocketOpened','1']);
sockets[0].message([229,0,1,8]); assert.deepEqual(calls.shift(),['Bridge','OnWebSocketBytes','1|5QABCA==']);
assert.equal(library.NroWSSend(1,0,3),1); assert.deepEqual([...sockets[0].sent[1]],[7,8,9]);
sockets[0].bufferedAmount = 8 * 1024 * 1024;
assert.equal(library.NroWSSend(1,0,3),0,'websocket backpressure must fail send before exceeding 8MB');
sockets[0].bufferedAmount = 0;
const staleMessage = sockets[0].onmessage; const staleClose = sockets[0].onclose;
library.NroWSClose(1); staleMessage({data: new ArrayBuffer(0)}); staleClose(); assert.equal(calls.length,0,'intentional close ignores stale callbacks');
context.window.NRO_WEB_CONFIG = {gatewayUrl:'ws://127.0.0.1:9000/gateway'};
library.NroWSOpen(2,'Bridge','localhost',14445); assert.equal(sockets[1].url,'ws://127.0.0.1:9000/gateway');
sockets[1].onerror(); sockets[1].onclose(); assert.deepEqual(calls,[['Bridge','OnWebSocketEnded','2']],'error/close reports once');
assert.equal(library.NroWSSend(2,0,3),0,'closed send fails');
// All four transports coexist; closing one never closes a sibling.
for (let id=3;id<7;id++) { library.NroWSOpen(id,'Bridge','localhost',14445); sockets[id-1].open(); }
library.NroWSClose(4); assert.equal(sockets[2].readyState,1); assert.equal(sockets[4].readyState,1); assert.equal(sockets[5].readyState,1);
console.log('PASS: websocket route, same-origin TLS, URL override, binary send/receive, stale callback protection, deduplicated errors, four isolated sockets');
