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
  NroInputClose: function () { if (window.NroInput) window.NroInput.close(); }
});
