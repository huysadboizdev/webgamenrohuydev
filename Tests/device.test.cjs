const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const file = path.join(__dirname, '../Assets/WebGLTemplates/NroWeb/device.js');
const classify = fs.existsSync(file) ? require(file).classify : () => 'pc';
for (const [name, ua, platform, touches, expected] of [
  ['Windows PC', 'Mozilla/5.0 Windows NT 10.0', 'Win32', 0, 'pc'],
  ['Windows touch PC', 'Mozilla/5.0 Windows NT 10.0', 'Win32', 10, 'pc'],
  ['Android', 'Mozilla/5.0 Linux Android 14', 'Linux armv8l', 5, 'android'],
  ['iPhone', 'Mozilla/5.0 iPhone CPU iPhone OS 18', 'iPhone', 5, 'ios'],
  ['iPad', 'Mozilla/5.0 iPad CPU OS 18', 'iPad', 5, 'ios'],
  ['iPad desktop UA', 'Mozilla/5.0 Macintosh Intel Mac OS X', 'MacIntel', 5, 'ios'],
  ['Mac desktop', 'Mozilla/5.0 Macintosh Intel Mac OS X', 'MacIntel', 0, 'pc']
]) test(name, () => assert.equal(classify({userAgent: ua, platform, maxTouchPoints: touches}), expected));
