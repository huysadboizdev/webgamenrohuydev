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
}
