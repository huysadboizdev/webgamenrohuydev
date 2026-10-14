(function (root) {
  'use strict';

  function create(options) {
    var document = options.document;
    var game = options.game;
    var supported = options.device === 'android' && document.fullscreenEnabled === true && typeof game.requestFullscreen === 'function';
    var initial;
    var pending;

    function state(decision, error) {
      var result = {supported: supported, active: document.fullscreenElement === game, decision: decision};
      if (error) result.error = error;
      return result;
    }

    function notify() {
      if (typeof options.onChange === 'function') options.onChange(state('change'));
    }

    function finish(dialog, decision, error) {
      if (pending !== dialog) return;
      pending = null;
      if (dialog.element.parentNode) dialog.element.parentNode.removeChild(dialog.element);
      dialog.resolve(state(decision, error));
    }

    function offer() {
      if (pending) return pending.promise;
      if (!supported) return Promise.resolve(state('unsupported'));

      var exiting = document.fullscreenElement === game;
      var dialog = {busy: false};
      dialog.promise = new Promise(function (resolve) { dialog.resolve = resolve; });
      pending = dialog;
      var overlay = document.createElement('div');
      dialog.element = overlay;
      overlay.id = 'nro-fullscreen-prompt';
      overlay.setAttribute('role', 'dialog');
      overlay.setAttribute('aria-modal', 'true');
      overlay.setAttribute('aria-labelledby', 'nro-fullscreen-title');
      overlay.style.cssText = 'position:absolute;inset:0;z-index:10000;display:flex;align-items:center;justify-content:center;flex-direction:column;gap:14px;padding:16px;box-sizing:border-box;background:rgba(0,0,0,.85);color:#fff;text-align:center;font:16px sans-serif;';
      var title = document.createElement('p');
      title.id = 'nro-fullscreen-title';
      title.textContent = exiting ? 'Thoát chế độ toàn màn hình?' : 'Chơi toàn màn hình để có thêm diện tích?';
      title.style.cssText = 'margin:0;max-width:360px;';
      overlay.appendChild(title);
      var actions = document.createElement('div');
      actions.style.cssText = 'display:flex;flex-wrap:wrap;justify-content:center;gap:12px;';
      overlay.appendChild(actions);
      function button(text, action) {
        var element = document.createElement('button');
        element.type = 'button';
        element.textContent = text;
        element.setAttribute('data-fullscreen-action', action);
        element.style.cssText = 'min-height:44px;padding:10px 16px;border:1px solid #ddd;border-radius:6px;background:#fff;color:#222;font:inherit;touch-action:manipulation;';
        actions.appendChild(element);
        return element;
      }
      var accept = button(exiting ? 'Thoát toàn màn hình' : 'Bật toàn màn hình', exiting ? 'exit' : 'enter');
      var later = button('Để sau', 'later');
      later.addEventListener('click', function () { if (!dialog.busy) finish(dialog, 'later'); });
      accept.addEventListener('click', function () {
        if (dialog.busy || pending !== dialog) return;
        dialog.busy = true;
        dialog.decision = exiting ? 'exit' : 'enter';
        accept.disabled = true;
        later.disabled = true;
        var request;
        try {
          // Keep the API call in this tap handler, before any promise or await.
          request = exiting ? document.exitFullscreen() : game.requestFullscreen({navigationUI: 'hide'});
        } catch (error) {
          finish(dialog, dialog.decision, error);
          return;
        }
        Promise.resolve(request).then(function () {
          finish(dialog, dialog.decision);
        }, function (error) {
          finish(dialog, dialog.decision, error);
        });
      });
      game.appendChild(overlay);
      return dialog.promise;
    }

    document.addEventListener('fullscreenchange', function () {
      notify();
      if (pending && pending.busy) finish(pending, pending.decision);
    });
    document.addEventListener('fullscreenerror', function () {
      notify();
      if (pending && pending.busy) finish(pending, pending.decision, new Error('Fullscreen request rejected'));
    });

    return {
      isSupported: supported,
      prepare: function () {
        if (!initial) initial = supported ? offer() : Promise.resolve(state('unsupported'));
        return initial;
      },
      offer: offer
    };
  }

  var api = {create: create};
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else root.NroFullscreen = api;
})(typeof window !== 'undefined' ? window : globalThis);
