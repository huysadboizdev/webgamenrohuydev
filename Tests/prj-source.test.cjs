const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..');
test('Both scenes use PRJ native Main classes; sample gameplay is absent',()=>{
 assert(!fs.existsSync(path.join(root,'Assets/Scripts/Scriptsweb')));
 for(const [tab,scene] of [['Game1','NROL'],['Game2','NROL1']]) {
  const base=path.join(root,'Assets/Scripts/Assembly-CSharp',tab);
  for(const file of ['Main.cs','Controller.cs','ModFunc.cs','GameScr.cs','Service.cs']) assert(fs.existsSync(path.join(base,file)),`${tab}/${file}`);
  const guid=fs.readFileSync(path.join(base,'Main.cs.meta'),'utf8').match(/guid: (\w+)/)[1];
  assert(fs.readFileSync(path.join(root,'Assets/Scenes',scene+'.unity'),'utf8').includes(guid));
 }
});
test('Native server capability declarations survive browser adaptation',()=>{
 for(const tab of ['Game1','Game2']) {
  const source=fs.readFileSync(path.join(root,'Assets/Scripts/Assembly-CSharp',tab,'Service.cs'),'utf8');
  for(const flag of ['IOP32','INV200','BOSS1','BOSS2']) assert(source.includes(flag),`${tab}: ${flag}`);
  assert(source.includes('TrySend'),`${tab}: combat send guard`);
 }
});
