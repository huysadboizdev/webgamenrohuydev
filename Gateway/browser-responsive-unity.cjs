const {chromium,expect,devices}=require('@playwright/test');
const {createGateway}=require('./server.cjs');
const {once}=require('node:events'),fs=require('node:fs'),path=require('node:path');
(async()=>{
 const server=createGateway();server.listen(0,'127.0.0.1');await once(server,'listening');
 const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});const results=[];
 const output=path.join(__dirname,'../Tests/evidence');
 try{
  for(const [name,options,platform,touches,mobile,sizes] of [
   ['windows',{viewport:{width:1366,height:768}},'Win32',0,false,[[1366,768],[800,600],[1920,1080]]],
   ['windows-touch',{viewport:{width:1024,height:768},hasTouch:true},'Win32',10,false,[[1024,768],[768,1024]]],
   ['android',{...devices['Pixel 7'],viewport:{width:360,height:800}},'Linux armv8l',5,true,[[360,800],[844,390],[320,568]]],
   ['ios',{...devices['iPhone 13'],viewport:{width:375,height:812}},'iPhone',5,true,[[375,812],[844,390],[320,568]]],
   ['ipad-desktop',{viewport:{width:768,height:1024},hasTouch:true,isMobile:true,userAgent:'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15) AppleWebKit/605.1.15 Version/17.0 Safari/605.1.15'},'MacIntel',5,true,[[768,1024],[1024,768]]]
  ]){
   const context=await browser.newContext(options);
   await context.addInitScript(({platform,touches})=>{Object.defineProperty(navigator,'platform',{get:()=>platform});Object.defineProperty(navigator,'maxTouchPoints',{get:()=>touches});},{platform,touches});
   const page=await context.newPage(),logs=[],errors=[],dialogs=[];
   page.on('console',m=>logs.push(m.text()));page.on('pageerror',e=>errors.push(String(e)));page.on('dialog',async d=>{dialogs.push(d.message());await d.accept();});
   await page.goto('http://127.0.0.1:'+server.address().port);
   await page.waitForFunction(()=>window.gameInstance!==null,{},{timeout:180000});
   const flags='pc='+(mobile?'False':'True')+' mobile='+(mobile?'True':'False');
   await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game1 '+flags)),{timeout:30000}).toBe(true);
   if(mobile)await page.setViewportSize({width:844,height:390});
   await page.locator('#tab2').click();await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game2 '+flags))).toBe(true);
   for(const [width,height] of sizes){
    await page.setViewportSize({width,height});
    if(mobile&&height>width){await expect(page.locator('#rotate-device')).toBeVisible();await page.screenshot({path:path.join(output,'responsive-'+name+'-'+width+'x'+height+'-rotate.png')});results.push({name,width,height,mobile,rotationRequired:true});continue;}
    await expect(page.locator('#rotate-device')).toBeHidden();
    for(const tab of [1,2]){
     await page.locator('#tab'+tab).click();
     await expect.poll(()=>page.evaluate(()=>NroTabs.current)).toBe(tab-1);
     await page.evaluate(()=>new Promise(resolve=>{let n=20;function frame(){if(--n<=0)resolve();else requestAnimationFrame(frame);}requestAnimationFrame(frame);}));
     const geometry=await page.locator('#unity-canvas').evaluate(c=>{const r=c.getBoundingClientRect();return {w:c.width,h:c.height,x:r.x,y:r.y,displayW:r.width,displayH:r.height,right:r.right,bottom:r.bottom};});
     expect(geometry.w).toBeGreaterThan(0);expect(geometry.h).toBeGreaterThan(0);expect(geometry.right).toBeLessThanOrEqual(width+.5);expect(geometry.bottom).toBeLessThanOrEqual(height+.5);
     await page.locator('#unity-canvas').screenshot({path:path.join(output,'responsive-'+name+'-'+width+'x'+height+'-tab'+tab+'.png')});
     results.push({name,width,height,tab,mobile,geometry});
    }
   }
   expect(errors).toEqual([]);expect(dialogs).toEqual([]);expect(logs.filter(s=>/Exception:|RuntimeError:|undefined symbol|NRO save sync failed/.test(s))).toEqual([]);
   fs.writeFileSync(path.join(output,'responsive-'+name+'-console.log'),logs.join('\n'));
   console.log('PASS actual WebGL '+name+': both platform flags, both accounts, resizing/rotation, no runtime errors');
   await context.close();
  }
  fs.writeFileSync(path.join(output,'responsive-unity.json'),JSON.stringify({engine:'Chromium with emulated device identity/touch/viewport',authenticated:false,results},null,2));
 }finally{await browser.close();await server.shutdown();}
})().catch(error=>{console.error(error);process.exitCode=1;});
