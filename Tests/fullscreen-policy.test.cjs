const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const modulePath = path.join(__dirname, '../Assets/WebGLTemplates/NroWeb/fullscreen.js');
const api = require(modulePath);

// A small DOM adapter exercises the controller without depending on browser packages.
class Element {
  constructor(tag) { this.tagName = tag; this.children = []; this.style = {}; this.listeners = {}; this.attributes = {}; this.disabled = false; }
  setAttribute(name, value) { this.attributes[name] = value; }
  appendChild(child) { this.children.push(child); child.parentNode = this; return child; }
  removeChild(child) { this.children.splice(this.children.indexOf(child), 1); child.parentNode = null; }
  addEventListener(name, callback) { (this.listeners[name] ||= []).push(callback); }
  dispatch(name) { for (const callback of this.listeners[name] || []) callback({target: this}); }
  click() { if (!this.disabled) this.dispatch('click'); }
}
function setup({device = 'android', enabled = true, request = 'accept', missing = false} = {}) {
  const document = new Element('document');
  document.createElement = tag => new Element(tag);
  document.fullscreenEnabled = enabled;
  document.fullscreenElement = null;
  const game = new Element('main');
  const canvas = game.appendChild(new Element('canvas'));
  const keyboard = game.appendChild(new Element('input'));
  const calls = [], changes = [];
  if (!missing) game.requestFullscreen = options => {
    calls.push({element: game, options});
    if (request === 'throw') throw new Error('Denied');
    if (request === 'reject') return Promise.reject(new Error('Denied'));
    if (request === 'nochange') return Promise.resolve();
    if (request === 'pending') return new Promise(() => {});
    document.fullscreenElement = game;
    document.dispatch('fullscreenchange');
    return Promise.resolve();
  };
  document.exitFullscreen = () => { calls.push({exit: true}); document.fullscreenElement = null; document.dispatch('fullscreenchange'); return Promise.resolve(); };
  const controller = api.create({device, document, game, onChange: state => changes.push(state)});
  const prompt = () => game.children.find(node => node.id === 'nro-fullscreen-prompt');
  const button = action => prompt().children.flatMap(node => node.children.length ? node.children : [node]).find(node => node.attributes['data-fullscreen-action'] === action);
  return {controller, game, document, calls, changes, prompt, button, canvas, keyboard};
}

test('supported Android waits for one initial decision inside game', async () => {
  const s = setup();
  assert.equal(s.controller.isSupported, true);
  let settled = false;
  const pending = s.controller.prepare(); pending.then(() => { settled = true; });
  await Promise.resolve();
  assert.equal(settled, false);
  assert.equal(s.prompt().parentNode, s.game);
  assert.equal(s.prompt().attributes.role, 'dialog');
  assert.equal(s.prompt().children[0].textContent, 'Chơi toàn màn hình để có thêm diện tích?');
  assert.equal(s.button('enter').textContent, 'Bật toàn màn hình');
  assert.equal(s.button('later').textContent, 'Để sau');
  s.button('later').click();
  assert.equal((await pending).decision, 'later');
  assert.equal(s.prompt(), undefined);
  assert.equal(s.calls.length, 0);
});

for (const options of [{enabled: false}, {missing: true}, {device: 'pc'}, {device: 'ios'}]) {
  test(`initial prompt bypasses ${JSON.stringify(options)}`, async () => {
    const s = setup(options);
    assert.equal(s.controller.isSupported, false);
    await s.controller.prepare();
    await s.controller.offer();
    assert.equal(s.prompt(), undefined);
    assert.equal(s.calls.length, 0);
  });
}

test('accept requests game fullscreen synchronously with navigation UI hidden', async () => {
  const s = setup(); const pending = s.controller.prepare();
  const accept = s.button('enter'); accept.click(); accept.click();
  assert.equal(s.calls.length, 1);
  assert.deepEqual(s.calls[0].options, {navigationUI: 'hide'});
  assert.equal(s.calls[0].element, s.game);
  assert(s.game.children.includes(s.canvas)); assert(s.game.children.includes(s.keyboard));
  assert.equal((await pending).active, true);
  assert.equal(s.changes.at(-1).active, true);
});

for (const request of ['reject', 'throw', 'nochange']) {
  test(`${request} continues startup without falsely claiming fullscreen`, async () => {
    const s = setup({request}); const pending = s.controller.prepare();
    s.button('enter').click();
    assert.equal((await pending).active, false);
    assert.equal(s.prompt(), undefined);
    assert.equal(s.calls.length, 1);
  });
}

test('repeated prepare reuses decision and never asks after tab changes', async () => {
  const s = setup(); const first = s.controller.prepare();
  assert.equal(s.controller.prepare(), first);
  s.button('later').click(); await first;
  assert.equal(s.controller.prepare(), first);
  assert.equal(s.prompt(), undefined);
});

test('later menu offer can enter then exit without another initial decision', async () => {
  const s = setup(); const initial = s.controller.prepare(); s.button('later').click(); await initial;
  const enter = s.controller.offer(); assert.equal(s.controller.offer(), enter);
  s.button('enter').click(); assert.equal((await enter).active, true);
  const exit = s.controller.offer(); assert.equal(s.button('exit').textContent, 'Thoát toàn màn hình');
  s.button('exit').click(); assert.equal((await exit).active, false);
  assert.equal(s.calls.length, 2);
  assert.equal(s.controller.prepare(), initial);
});

test('Back fullscreenchange reports actual exit independently of menu', async () => {
  const s = setup(); const pending = s.controller.prepare(); s.button('enter').click(); await pending;
  s.document.fullscreenElement = null; s.document.dispatch('fullscreenchange');
  assert.equal(s.changes.at(-1).active, false);
  assert.equal(s.prompt(), undefined);
});

test('fullscreenerror releases startup even when request promise remains pending', async () => {
  const s = setup({request: 'pending'}); const pending = s.controller.prepare(); s.button('enter').click();
  s.document.dispatch('fullscreenerror');
  assert.equal((await pending).active, false);
  assert.equal(s.prompt(), undefined);
});

test('menu exit rejection preserves the actual fullscreen state', async () => {
  const s = setup(); const initial = s.controller.prepare(); s.button('enter').click(); await initial;
  s.document.exitFullscreen = () => Promise.reject(new Error('Denied exit'));
  const pending = s.controller.offer(); s.button('exit').click();
  const result = await pending;
  assert.equal(result.active, true);
  assert.equal(result.error.message, 'Denied exit');
  assert.equal(s.prompt(), undefined);
});
