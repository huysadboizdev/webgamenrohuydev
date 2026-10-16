const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),vm=require('node:vm');
for(const file of ['Assets/WebGLTemplates/NroWeb/index.html','Builds/WebGL/index.html']) {
  const html=fs.readFileSync(path.resolve(__dirname,'..',file),'utf8');
  test(file+': original game owns account switching, shell has no extra toolbar',()=>{
    assert.doesNotMatch(html,/<nav\b|id="(?:tab1|tab2|fullscreen|device)"/);
    assert.match(html,/id="unity-canvas"/);
    assert.match(html,/id="keyboard"/);
  });
  test(file+': native account callback preserves selection without DOM buttons',()=>{
    const statement=html.match(/window.NroTabs=\{[^\n]+\};/)[0];
    const sandbox={window:{},NroInput:{close(){sandbox.closed=true;}}};
    vm.runInNewContext(statement,sandbox);
    sandbox.window.NroTabs.changed(1);
    assert.equal(sandbox.window.NroTabs.current,1);
    assert.equal(sandbox.closed,true);
  });
  test(file+': Unity data cache is immutable while bundles retain revalidation',()=>{
    const match=html.match(/cacheControl:function\(url\)\{[^}]+\}/);
    assert.ok(match,'Unity cacheControl override is required');
    const cacheControl=vm.runInNewContext('('+match[0].slice('cacheControl:'.length)+')',{config:{dataUrl:'Build/client.data'}});
    assert.equal(cacheControl('Build/client.data'),'immutable');
    assert.equal(cacheControl('Build/client.bundle'),'must-revalidate');
    assert.equal(cacheControl('Build/client.framework.js'),'no-store');
  });
  test(file+': Unity data uses persistent IndexedDB caching on mobile',()=>{
    assert.match(html,/<script src="data-cache\.js"><\/script>/);
    assert.match(html,/cachedFetch:NroDataCache\.cachedFetch/);
  });
}
test('generated Unity loader preserves the configured mobile data cache adapter',()=>{
  const html=fs.readFileSync(path.resolve(__dirname,'../Builds/WebGL/index.html'),'utf8');
  const loaderPath=html.match(/script\.src='(Build\/[^']+\.loader\.js)'/);
  assert.ok(loaderPath,'active WebGL shell must reference a generated loader');
  const loader=fs.readFileSync(path.resolve(__dirname,'../Builds/WebGL',loaderPath[1]),'utf8');
  assert.match(loader,/t=l\.companyName&&l\.productName\?l\.cachedFetch:l\.fetchWithProgress/);
});
const cacheScriptPath=path.resolve(__dirname,'../Builds/WebGL/data-cache.js');
const cacheScript=fs.readFileSync(cacheScriptPath,'utf8');
test('mobile cache adapter is valid and matches the Unity template copy',()=>{
  assert.doesNotThrow(()=>new vm.Script(cacheScript));
  assert.equal(cacheScript,fs.readFileSync(path.resolve(__dirname,'../Assets/WebGLTemplates/NroWeb/data-cache.js'),'utf8'));
});
test('mobile cache invalidates if any available server validator changes',()=>{
  assert.match(cacheScript,/if\(head\.ok&&validatorsMatch\(cached\.meta,head\)\)/);
  const instrumented=cacheScript.replace('global.NroDataCache={cachedFetch:cachedFetch};','global.NroDataCache={cachedFetch:cachedFetch,validatorsMatch:validatorsMatch};');
  const sandbox={window:{}};
  vm.runInNewContext(instrumented,sandbox);
  const response=(etag,lastModified)=>({headers:{get:(name)=>name==='ETag'?etag:lastModified}});
  const matches=sandbox.window.NroDataCache.validatorsMatch;
  assert.equal(matches({etag:'"same"',lastModified:'Mon, 01 Jan 2024 00:00:00 GMT'},response('"same"','Mon, 01 Jan 2024 00:00:00 GMT')),true);
  assert.equal(matches({etag:'"same"',lastModified:'Mon, 01 Jan 2024 00:00:00 GMT'},response('"same"','Tue, 02 Jan 2024 00:00:00 GMT')),false);
  assert.equal(matches({etag:'"old"',lastModified:'Mon, 01 Jan 2024 00:00:00 GMT'},response('"new"','Mon, 01 Jan 2024 00:00:00 GMT')),false);
  assert.equal(matches({etag:'"same"',lastModified:null},response('"same"',null)),true);
  assert.equal(matches({etag:null,lastModified:null},response(null,null)),false);
});
