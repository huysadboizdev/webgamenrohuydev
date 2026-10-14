const {chromium,expect}=require('@playwright/test');
const {createGateway}=require('./server.cjs');
const {once}=require('node:events');
const fs=require('node:fs');
(async()=>{
 const server=createGateway();server.listen(0,'127.0.0.1');await once(server,'listening');
 const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 const logs=[],errors=[],transitions=[];
 try{
  const page=await browser.newPage({viewport:{width:1280,height:800}});
  page.on('console',m=>logs.push(m.text()));page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:'+server.address().port);
  await page.waitForFunction(()=>gameInstance!==null,{},{timeout:180000});
  await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game1')),{timeout:30000}).toBe(true);
  async function frames(count){await page.evaluate(n=>new Promise(resolve=>{function next(){if(--n<=0)resolve();else requestAnimationFrame(next);}requestAnimationFrame(next);}),count);}
  async function verify(index){await expect.poll(()=>page.evaluate(()=>NroTabs.current)).toBe(index);await frames(30);expect(await page.evaluate(()=>NroTabs.current)).toBe(index);transitions.push(index);}
  await frames(90);
  // The game button must notify the browser bridge and stay on its target.
  for(let i=0;i<8;i++){
   const box=await page.locator('#unity-canvas').boundingBox();
   const size=await page.locator('#unity-canvas').evaluate(c=>({w:c.width,h:c.height}));
   const zoom=size.w*size.h<480000?1:2;
   await page.mouse.click(box.x+30*zoom*box.width/size.w,box.y+22*zoom*box.height/size.h,{delay:100});
   await verify(i%2===0?1:0);
   if(i===0)await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game2'))).toBe(true);
  }
  // A drag held while switching must not press the opposite tab's game button.
  const box=await page.locator('#unity-canvas').boundingBox();
  await page.mouse.move(box.x+60,box.y+44);await page.mouse.down();await frames(3);
  await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','1'));await page.mouse.up();await verify(1);
  await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','0'));await verify(0);
  await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','1'));await verify(1);
  await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','0'));await verify(0);
  // Both loaded scenes must be reused during rapid requests.
  await page.evaluate(()=>{for(let i=0;i<12;i++)gameInstance.SendMessage('BrowserHost','SelectTab',String(i%2?0:1));});await verify(0);
  expect(logs.filter(s=>s.includes('NRO_WEB_DEVICE Game2')).length).toBe(1);
  expect(errors).toEqual([]);
  expect(logs.filter(s=>/Exception:|RuntimeError:|undefined symbol/.test(s))).toEqual([]);
  await page.screenshot({path:'../Tests/evidence/tab-switch-final.png'});
  fs.writeFileSync('../Tests/evidence/tab-switch-results.json',JSON.stringify({passed:true,transitions,internalSwitches:8,heldPointer:true,rapidRequests:12},null,2));
  console.log('PASS 8 in-game tab switches, bridge synchronization, held-pointer handoff, bridge requests and 12 rapid requests');
 }finally{fs.writeFileSync('../Tests/evidence/tab-switch-console.log',logs.join('\n'));await browser.close();await server.shutdown();}
})().catch(e=>{console.error(e);process.exitCode=1;});
