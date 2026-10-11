mergeInto(LibraryManager.library, {
  $NroSockets: {},
  NroWSOpen__deps: ['$NroSockets'],
  NroWSOpen: function (id, receiverPointer, hostPointer, port) {
    var receiver = UTF8ToString(receiverPointer), host = UTF8ToString(hostPointer);
    var entry = { receiver: receiver, socket: null };
    NroSockets[id] = entry;
    var active = function () { return NroSockets[id] === entry; };
    var finish = function () {
      if (!active()) return;
      delete NroSockets[id];
      if (entry.socket) { try { entry.socket.close(); } catch (_) {} }
      SendMessage(receiver, 'OnWebSocketEnded', String(id));
    };
    try {
      var config = window.NRO_WEB_CONFIG || {};
      var url = config.gatewayUrl || ((window.location.protocol === 'https:' ? 'wss://' : 'ws://') + window.location.host + '/gateway');
      var socket = new WebSocket(url); entry.socket = socket; socket.binaryType = 'arraybuffer';
      socket.onopen = function () {
        if (!active()) return;
        try {
          socket.send(new TextEncoder().encode(host + ':' + port));
          SendMessage(receiver, 'OnWebSocketOpened', String(id));
        } catch (_) { finish(); }
      };
      socket.onmessage = function (event) {
        if (!active()) return;
        try {
          var bytes = new Uint8Array(event.data), binary = '';
          // Avoid apply/spread argument limits on large inventory packets.
          for (var start = 0; start < bytes.length; start += 8192) {
            var chunk = ''; var end = Math.min(start + 8192, bytes.length);
            for (var i = start; i < end; i++) chunk += String.fromCharCode(bytes[i]);
            binary += chunk;
          }
          SendMessage(receiver, 'OnWebSocketBytes', id + '|' + btoa(binary));
        } catch (_) { finish(); }
      };
      socket.onerror = finish; socket.onclose = finish;
    } catch (_) { finish(); }
  },
  NroWSSend__deps: ['$NroSockets'],
  NroWSSend: function (id, pointer, length) {
    var entry = NroSockets[id];
    if (!entry || !entry.socket || entry.socket.readyState !== 1) return 0;
    if (length > 8 * 1024 * 1024 - (entry.socket.bufferedAmount || 0)) return 0;
    try { entry.socket.send(HEAPU8.slice(pointer, pointer + length)); return 1; } catch (_) { return 0; }
  },
  NroWSClose__deps: ['$NroSockets'],
  NroWSClose: function (id) {
    var entry = NroSockets[id]; if (!entry) return;
    delete NroSockets[id]; // Invalidate before close fires, including synchronous test sockets.
    try { if (entry.socket) entry.socket.close(); } catch (_) {}
  }
});
