const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

for (const file of ['Assets/WebGLTemplates/NroWeb/index.html', 'Builds/WebGL/index.html']) {
  const html = fs.readFileSync(path.join(__dirname, '..', file), 'utf8');
  const resize = html.match(/function resize\(\)\{[^\n]+\}/)[0];
  for (const [width, height, dpr] of [[844, 280, 3], [844, 390, 3], [667, 260, 2], [896, 280, 2]]) {
    test(`${file}: iPhone ${width}x${height} @${dpr} uses native device pixels`, () => {
      const context = {device: 'ios', view: {clientWidth: width, clientHeight: height}, canvas: {style: {}}, window: {devicePixelRatio: dpr}, Math};
      vm.runInNewContext(resize + ';resize();', context);
      assert.equal(context.canvas.width, width * dpr);
      assert.equal(context.canvas.height, height * dpr);
      assert.equal(context.canvas.style.width, '100%');
      assert.equal(context.canvas.style.height, '100%');
    });
  }
  test(`${file}: Safari toolbar and keyboard use the visible viewport`, () => {
    const sync = html.match(/function syncViewport\(\)\{[^\n]+\}/);
    assert(sync, 'Viewport must follow visualViewport, including its resize/scroll events');
    const context = {game: {style: {}}, window: {innerWidth: 844, innerHeight: 390, visualViewport: {width: 844, height: 280, offsetLeft: 0, offsetTop: 20}}, Math};
    vm.runInNewContext(sync[0] + ';syncViewport();', context);
    assert.equal(context.game.style.width, '844px');
    assert.equal(context.game.style.height, '280px');
    assert.equal(context.game.style.top, '20px');
    context.window.visualViewport.height = 190;
    context.window.visualViewport.offsetTop = 0;
    vm.runInNewContext('syncViewport();', context);
    assert.equal(context.game.style.height, '190px');
    assert(html.includes("visualViewport.addEventListener('resize',syncViewport)"));
    assert(html.includes("visualViewport.addEventListener('scroll',syncViewport)"));
  });
}
