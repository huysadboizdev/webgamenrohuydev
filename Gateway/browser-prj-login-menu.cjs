const {chromium,expect}=require('@playwright/test');
const fs=require('node:fs');
(async()=>{
 const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 const logs=[],errors=[];
 try{
  const page=await browser.newPage({viewport:{width:1280,height:800}});
  page.on('console',m=>logs.push(m.text()));page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:8086/?v=prj-20261014');
  await page.waitForFunction(()=>window.gameInstance && document.querySelector('#status').style.display==='none',null,{timeout:180000});
  await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game1')),{timeout:30000}).toBe(true);
  const frames=n=>page.evaluate(n=>new Promise(resolve=>{function step(){if(--n<=0)resolve();else requestAnimationFrame(step);}requestAnimationFrame(step);}),n);
  await frames(360);
  const box=await page.locator('#unity-canvas').boundingBox();
  await page.mouse.click(box.x+box.width/2,box.y+box.height*.95,{delay:100});
  await frames(180);
  await page.screenshot({path:'../Tests/evidence/prj-release-20261014/login-menu.png'});
  expect(errors).toEqual([]);
  expect(logs.filter(s=>/Exception:|RuntimeError:|undefined symbol/.test(s))).toEqual([]);
  fs.writeFileSync('../Tests/evidence/prj-release-20261014/login-menu.log',logs.join('\n'));
  console.log('PASS selecting local server renders without runtime errors; no account login submitted');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
