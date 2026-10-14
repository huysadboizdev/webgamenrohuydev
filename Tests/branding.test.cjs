const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const {once} = require('node:events');
const {createGateway} = require('../Gateway/server.cjs');
const root = path.resolve(__dirname, '..');
// SHA-256 recorded from the original PRJ 3in1 Goc PlayerSettings icon.
const referenceHash = '01c40628783b36b2873ee7be89f3a8a4ac1a7708e7b13de55c1a94ff552defd0';
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
test('browser and iOS bookmark receive the original 3 in 1 icon', async () => {
  const template = fs.readFileSync(path.join(root, 'Assets/WebGLTemplates/NroWeb/index.html'), 'utf8');
  assert.match(template, /rel="icon"[^>]*href="iconlogo.png"/);
  assert.match(template, /rel="apple-touch-icon"[^>]*href="iconlogo.png"/);
  const server = createGateway({root:path.join(root,'Assets/WebGLTemplates/NroWeb')});
  server.listen(0,'127.0.0.1');
  await once(server,'listening');
  try {
    const response = await fetch('http://127.0.0.1:'+server.address().port+'/iconlogo.png');
    assert.equal(response.status,200);
    assert.equal(response.headers.get('content-type'),'image/png');
    assert.equal(hash(Buffer.from(await response.arrayBuffer())), referenceHash);
    assert.equal(hash(fs.readFileSync(path.join(root,'Assets/Branding/iconlogo.png'))), referenceHash);
  } finally { await server.shutdown(); }
});
