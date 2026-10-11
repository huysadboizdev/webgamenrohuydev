const {chromium,expect}=require('@playwright/test');
const {createGateway}=require('./server.cjs');
const {once}=require('node:events');
const fs=require('node:fs');
const {createHash}=require('node:crypto');
(async()=>{
 const server=createGateway();server.listen(0,'127.0.0.1');await once(server,'listening');
 const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 const logs=[],errors=[],dialogs=[],completed=[];
 try{
  const page=await browser.newPage({viewport:{width:1280,height:800}});
  page.on('console',m=>logs.push(m.text()));page.on('pageerror',e=>errors.push(e.message));
  page.on('dialog',async d=>{dialogs.push(d.message());await d.dismiss();});
  await page.goto('http://127.0.0.1:'+server.address().port);
  await page.waitForFunction(()=>gameInstance!==null,{},{timeout:180000});
  await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game1')),{timeout:30000}).toBe(true);
  for(const tab of [1,0]){
   await page.locator('#tab'+(tab+1)).click();
   await page.locator('#unity-canvas').focus();
   await page.waitForTimeout(2500);
   const box=await page.locator('#unity-canvas').boundingBox();
   const size=await page.locator('#unity-canvas').evaluate(c=>({w:c.width,h:c.height}));
   const zoom=size.w*size.h<480000?1:2;
   await page.mouse.click(box.x+box.width*0.5,box.y+box.height/2,{delay:120});
   const versionFile=createHash('sha256').update('ResVersion').digest('hex').toUpperCase()+'.bin';
   const saved=await expect.poll(()=>page.evaluate(async({tab,versionFile})=>{
    for(const {name} of await indexedDB.databases()){
     if(!name.startsWith('/idbfs'))continue;
     const db=await new Promise((resolve,reject)=>{const r=indexedDB.open(name);r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error);});
     try{
      if(!db.objectStoreNames.contains('FILE_DATA'))continue;
      const records=await new Promise((resolve,reject)=>{const values=[];const r=db.transaction('FILE_DATA').objectStore('FILE_DATA').openCursor();r.onsuccess=()=>{const c=r.result;if(!c){resolve(values);return;}values.push({key:c.key,value:c.value});c.continue();};r.onerror=()=>reject(r.error);});
      const prefix='/nro-Tab'+(tab+1)+'/';
      if(records.some(r=>String(r.key).includes(prefix)&&String(r.key).endsWith(versionFile)&&r.value.contents?.length>2))return records.filter(r=>String(r.key).includes(prefix)&&r.value.contents).length;
     }finally{db.close();}
    }
    return 0;
   },{tab,versionFile}),{timeout:90000,intervals:[500,1000]}).toBeGreaterThanOrEqual(1434);
   completed.push(tab+1);
   await page.screenshot({path:'../Tests/evidence/resource-download-tab'+(tab+1)+'.png'});
  }
  expect(dialogs).toEqual([]);expect(errors).toEqual([]);
  expect(logs.filter(s=>/RuntimeError:|DivideByZeroException|Exception:/.test(s))).toEqual([]);
  console.log('PASS both tabs download 1434 resources and persist ResVersion without browser traps');
 }finally{
  fs.writeFileSync('../Tests/evidence/resource-download-results.json',JSON.stringify({logs,errors,dialogs,completed},null,2));
  await browser.close();await server.shutdown();
 }
})().catch(e=>{console.error(e);process.exitCode=1;});
