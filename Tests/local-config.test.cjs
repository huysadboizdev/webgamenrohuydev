const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '..');
function config(file) {
  const sandbox = {window:{}};
  vm.runInNewContext(fs.readFileSync(path.join(root,file),'utf8'),sandbox);
  return sandbox.window.NRO_WEB_CONFIG;
}
test('local browser, next build and gateway route to the same local TCP server',()=>{
  const served=config('Builds/WebGL/config.js');
  const template=config('Assets/WebGLTemplates/NroWeb/config.js');
  const gateway=JSON.parse(fs.readFileSync(path.join(root,'Gateway/config.json'),'utf8'));
  assert.equal(served.serverList,'NRO NGU:127.0.0.1:14445:0,0,0');
  assert.equal(served.serverList,template.serverList);
  assert.deepEqual(gateway.targets,['127.0.0.1:14445']);
  assert.equal(gateway.port,8086);
});
