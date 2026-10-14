const {chromium, expect} = require('@playwright/test');
const {createGateway} = require('./server.cjs');
const {once} = require('node:events');
const fs = require('node:fs');
const path = require('node:path');

(async () => {
  const server = createGateway(); server.listen(0, '127.0.0.1'); await once(server, 'listening');
  const browser = await chromium.launch({headless: true});
  const results = [];
  try {
    for (const kind of ['android-enter', 'android-later', 'android-reject', 'android-unsupported', 'ios', 'pc']) {
      const mobile = kind !== 'pc';
      const context = await browser.newContext({viewport: {width: 844, height: 390}, isMobile: mobile, hasTouch: mobile, deviceScaleFactor: mobile ? 3 : 1,
        userAgent: kind.startsWith('android') ? 'Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/128.0.0.0 Mobile Safari/537.36' : kind === 'ios' ? 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1' : undefined});
      if (kind === 'android-reject') await context.addInitScript(() => {Element.prototype.requestFullscreen = () => Promise.reject(new Error('fixture denial'));});
      if (kind === 'android-unsupported') await context.addInitScript(() => {Object.defineProperty(document, 'fullscreenEnabled', {get: () => false});});
      const page = await context.newPage(); const errors = []; page.on('pageerror', e => errors.push(String(e)));
      // Isolate shell sequencing from Unity/GPU initialization. Fullscreen API remains real.
      await page.route('**/*.loader.js', route => route.fulfill({contentType: 'text/javascript', body: `window.fixtureStarts=0;window.fixtureMessages=[];window.createUnityInstance=function(canvas){window.fixtureStarts++;return Promise.resolve({SendMessage:function(...args){window.fixtureMessages.push(args);}});};`}));
      await page.goto('http://127.0.0.1:' + server.address().port);
      if (['android-enter', 'android-later', 'android-reject'].includes(kind)) {
        await expect(page.locator('#nro-fullscreen-prompt')).toBeVisible();
        expect(await page.evaluate(() => window.fixtureStarts || 0)).toBe(0);
        await page.getByRole('button', {name: kind === 'android-later' ? 'Để sau' : 'Bật toàn màn hình', exact: true}).tap();
      } else await expect(page.locator('#nro-fullscreen-prompt')).toHaveCount(0);
      await page.waitForFunction(() => window.gameInstance !== null);
      expect(await page.evaluate(() => window.fixtureStarts)).toBe(1);
      if (kind === 'android-enter') {
        await expect.poll(() => page.evaluate(() => document.fullscreenElement?.id)).toBe('game');
        await page.evaluate(() => NroTabs.changed(1)); await expect(page.locator('#nro-fullscreen-prompt')).toHaveCount(0);
        await page.evaluate(() => {NroFullscreenController.offer();});
        await page.getByRole('button', {name: 'Thoát toàn màn hình', exact: true}).tap();
        await expect.poll(() => page.evaluate(() => document.fullscreenElement === null)).toBe(true);
        await page.evaluate(() => {NroFullscreenController.offer();}); await page.getByRole('button', {name: 'Bật toàn màn hình', exact: true}).tap();
        await expect.poll(() => page.evaluate(() => document.fullscreenElement?.id)).toBe('game');
        await page.evaluate(() => document.exitFullscreen());
        await expect.poll(() => page.evaluate(() => document.fullscreenElement === null)).toBe(true);
        expect(await page.evaluate(() => window.fixtureStarts)).toBe(1);
        expect(await page.evaluate(() => fixtureMessages.some(m => m[1] === 'ClearBrowserInput'))).toBe(true);
      }
      const geometry = await page.locator('#unity-canvas').evaluate(c => {const r=c.getBoundingClientRect();return {width:c.width,height:c.height,right:r.right,bottom:r.bottom};});
      expect(geometry.right).toBeLessThanOrEqual(845); expect(geometry.bottom).toBeLessThanOrEqual(391);
      expect(errors).toEqual([]); results.push({kind, bootStarts: 1, geometry, errors}); await context.close();
    }
    fs.writeFileSync(path.join(__dirname, '../Tests/evidence/fullscreen-shell.json'), JSON.stringify({unityLoader: 'fixture', fullscreenAPI: 'real Chromium', results}, null, 2));
    console.log('PASS real fullscreen enter/menu exit/external exit, one boot, skip/denial/unsupported, iOS/PC unchanged');
  } finally {await browser.close(); await new Promise(resolve => server.close(resolve));}
})().catch(error => {console.error(error); process.exitCode = 1;});
