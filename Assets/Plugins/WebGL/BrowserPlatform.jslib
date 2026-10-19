mergeInto(LibraryManager.library, {
  NroSyncSaves: function () {
    var state = Module.nroSaveSync || (Module.nroSaveSync = { busy: false, dirty: false, timer: null });
    state.dirty = true;
    if (state.timer || state.busy) return;
    var flush = function () {
      state.timer = null;
      if (state.busy || !state.dirty) return;
      state.busy = true; state.dirty = false;
      FS.syncfs(false, function (error) {
        state.busy = false;
        if (error) console.error('NRO save sync failed', error);
        if (state.dirty) state.timer = setTimeout(flush, 250);
      });
    };
    state.timer = setTimeout(flush, 250);
  },
  NroTabChanged: function (tab) { if (window.NroTabs) window.NroTabs.changed(tab); },
  NroDeviceKind: function () {
    var device = window.NroDevice ? window.NroDevice.classify(navigator) : 'pc';
    return device === 'android' ? 1 : device === 'ios' ? 2 : 0;
  },
  NroFullscreenAvailable: function () { return window.NroFullscreenController && window.NroFullscreenController.isSupported ? 1 : 0; },
  NroFullscreenMenu: function () { if (window.NroFullscreenController) window.NroFullscreenController.offer(); },
  NroServerList: function () {
    var value = (window.NRO_WEB_CONFIG || {}).serverList || 'NRO NGU:127.0.0.1:14445:0,0,0';
    var size = lengthBytesUTF8(value) + 1;
    var ptr = _malloc(size);
    stringToUTF8(value, ptr, size);
    return ptr;
  },
  NroInputOpen: function (text, password, numeric) {
    if (window.NroInput) window.NroInput.open(UTF8ToString(text), !!password, !!numeric);
  },
  NroInputValue: function () {
    var value = window.NroInput ? window.NroInput.value() : '';
    var size = lengthBytesUTF8(value) + 1;
    var ptr = _malloc(size);
    stringToUTF8(value, ptr, size);
    return ptr;
  },
  NroInputState: function () { return window.NroInput ? window.NroInput.state() : 0; },
  NroInputClose: function () { if (window.NroInput) window.NroInput.close(); },
  NroCombatLogAppend: function (linePtr) {
    var state = Module.nroCombatLogState;
    if (!state) {
      var sessionId;
      try {
        sessionId = sessionStorage.getItem('nro-combat-log-session');
        if (!sessionId) {
          var stamp = new Date().toISOString().replace(/[:.]/g, '-');
          var random = Math.random().toString(36).slice(2, 10);
          sessionId = stamp + '-' + random;
          sessionStorage.setItem('nro-combat-log-session', sessionId);
        }
      } catch (error) {
        sessionId = Date.now().toString(36) + '-' + Math.random().toString(36).slice(2, 10);
      }
      state = Module.nroCombatLogState = { sessionId: sessionId, lines: [], busy: false, retryAt: 0, timer: null };
      try {
        var saved = JSON.parse(localStorage.getItem('nro-combat-log-pending') || 'null');
        if (saved && Array.isArray(saved.lines)) state.lines = saved.lines.filter(function (item) { return typeof item === 'string'; }).slice(-2000);
      } catch (error) { state.lines = []; }
      var persist = function () {
        try { localStorage.setItem('nro-combat-log-pending', JSON.stringify({ sessionId: state.sessionId, lines: state.lines })); }
        catch (error) { /* The Gateway remains the primary durable log. */ }
      };
      var flush = function () {
        if (state.busy || !state.lines.length || Date.now() < state.retryAt) return;
        var batch = state.lines.slice(0, 100);
        state.busy = true;
        fetch('/diagnostics', {
          method: 'POST',
          credentials: 'same-origin',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ sessionId: state.sessionId, lines: batch }),
          keepalive: true
        }).then(function (response) {
          if (!response.ok) throw new Error('HTTP ' + response.status);
          state.lines.splice(0, batch.length);
          persist();
          state.retryAt = 0;
        }).catch(function () {
          state.retryAt = Date.now() + 5000;
          persist();
        }).then(function () { state.busy = false; });
      };
      state.persist = persist;
      state.flush = flush;
      state.timer = setInterval(flush, 1000);
      window.addEventListener('pagehide', function () {
        if (state.busy || !state.lines.length || !navigator.sendBeacon) { persist(); return; }
        var batch = state.lines.slice(0, 100);
        var body = new Blob([JSON.stringify({ sessionId: state.sessionId, lines: batch })], { type: 'application/json' });
        if (navigator.sendBeacon('/diagnostics', body)) {
          state.lines.splice(0, batch.length);
        }
        persist();
      });
    }
    state.lines.push(UTF8ToString(linePtr));
    if (state.lines.length > 2000) state.lines.splice(0, state.lines.length - 2000);
    state.persist();
  }
});
