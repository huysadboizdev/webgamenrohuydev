const {chromium,expect}=require('@playwright/test');
const {createGateway}=require('./server.cjs');
const {once}=require('node:events'),fs=require('node:fs'),path=require('node:path');
const windows='Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36';
const android='Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/128.0.0.0 Mobile Safari/537.36';
const iphone='Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Version/17.0 Mobile Safari/604.1';
const ipad='Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15) AppleWebKit/605.1.15 Version/17.0 Safari/605.1.15';
(async()=>{
 const server=createGateway();server.listen(0,'127.0.0.1');await once(server,'listening');
 const browser=await chromium.launch({headless:true});const results=[];
 try{
  for(const [name,ua,platform,touches,device,sizes] of [
   ['Windows laptop',windows,'Win32',0,'pc',[[1366,768],[1920,1080],[800,600],[320,568]]],
   ['Windows touch laptop',windows,'Win32',10,'pc',[[1024,768],[768,1024]]],
   ['Android',android,'Linux armv8l',5,'android',[[320,568],[360,800],[844,390],[412,915]]],
   ['iPhone',iphone,'iPhone',5,'ios',[[320,568],[375,812],[844,390]]],
   ['iPad desktop UA',ipad,'MacIntel',5,'ios',[[768,1024],[1024,768]]]
  ]){
   const context=await browser.newContext({viewport:{width:sizes[0][0],height:sizes[0][1]},userAgent:ua,isMobile:device!=='pc',hasTouch:touches>0,deviceScaleFactor:device==='pc'?1.25:3});
   await context.addInitScript(({platform,touches})=>{
    Object.defineProperty(navigator,'platform',{get:()=>platform});Object.defineProperty(navigator,'maxTouchPoints',{get:()=>touches});
   },{platform,touches});
   const page=await context.newPage();
   // Test shipped HTML/CSS/device JS with a small Unity-loader stub. Actual
   // WebGL platform flags/startup are covered separately by browser-smoke.cjs.
   await page.route('**/Build/*.loader.js',route=>route.fulfill({contentType:'application/javascript',body:'window.createUnityInstance=async function(canvas,config){window.testConfig=config;return {SendMessage:function(host,method,value){if(method==="SelectTab")NroTabs.changed(Number(value));}};};'}));
   await page.goto('http://127.0.0.1:'+server.address().port);
   await page.waitForFunction(()=>window.gameInstance!==null);
   for(const [width,height] of sizes){
    await page.setViewportSize({width,height});
    await expect.poll(()=>page.evaluate(()=>view.clientWidth)).toBe(width);
    await page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
    const layout=await page.evaluate(()=>{
     function rect(el){const r=el.getBoundingClientRect();return {x:r.x,y:r.y,w:r.width,h:r.height,right:r.right,bottom:r.bottom};}
     return {kind:device,label:document.getElementById('device').textContent,mobile:document.body.classList.contains('mobile'),canvas:rect(canvas),backing:{w:canvas.width,h:canvas.height},view:rect(view),nav:rect(document.querySelector('nav')),controls:['tab1','tab2','fullscreen','device'].map(id=>({id,...rect(document.getElementById(id))})),overflow:document.documentElement.scrollWidth>innerWidth,config:window.testConfig};
    });
    expect(layout.kind,name).toBe(device);expect(layout.mobile).toBe(device!=='pc');
    expect(layout.overflow,name+' horizontal overflow at '+width).toBe(false);
    for(const control of layout.controls){expect(control.x).toBeGreaterThanOrEqual(0);expect(control.right,name+' clipped '+control.id+' at '+width).toBeLessThanOrEqual(width+.5);}
    expect(layout.canvas.right).toBeLessThanOrEqual(width+.5);expect(layout.canvas.bottom).toBeLessThanOrEqual(height+.5);
    expect(layout.backing.w).toBeLessThanOrEqual(1024);expect(layout.backing.h).toBeLessThanOrEqual(device==='pc'?600:768);
    expect(Math.abs(layout.canvas.w/layout.canvas.h-layout.backing.w/layout.backing.h)).toBeLessThan(.01);
    expect(layout.config.devicePixelRatio).toBe(1);expect(layout.config.matchWebGLToCanvasSize).toBe(false);
    if(device!=='pc'&&height>width){await expect(page.locator('#rotate-device')).toBeVisible();expect(await page.locator('#view').evaluate(e=>e.inert)).toBe(true);}
    else {await expect(page.locator('#rotate-device')).toBeHidden();for(const tab of [2,1]){await page.locator('#tab'+tab).click();await expect(page.locator('#tab'+tab)).toHaveClass('active');}}
    results.push({name,width,height,...layout});
    console.log('PASS responsive '+name+' '+width+'x'+height);
   }
   await context.close();
  }
  fs.writeFileSync(path.join(__dirname,'../Tests/evidence/responsive-layout.json'),JSON.stringify(results,null,2));
 }finally{await browser.close();await server.shutdown();}
})().catch(error=>{console.error(error);process.exitCode=1;});
