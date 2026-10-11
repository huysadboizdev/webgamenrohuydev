const {chromium,expect}=require('@playwright/test'),net=require('node:net'),{once}=require('node:events');
const {createGateway}=require('./server.cjs');
(async()=>{
 const sockets=new Set();let handshakes=0;
 const tcp=net.createServer(socket=>{sockets.add(socket);socket.on('close',()=>sockets.delete(socket));let pending=Buffer.alloc(0),ready=false;socket.on('data',data=>{if(ready)return;pending=Buffer.concat([pending,data]);if(pending.length<3)return;if(pending[0]!==229||pending[1]!==0||pending[2]!==0){socket.destroy();return;}ready=true;handshakes++;socket.write(Buffer.from([229,0,4,3,7,14,4]));});});
 for(let i=0;;i++){try{tcp.listen(20000+Math.floor(Math.random()*10000),'127.0.0.1');await once(tcp,'listening');break;}catch(e){if(e.code!=='EADDRINUSE'||i>20)throw e;}}
 const target='127.0.0.1:'+tcp.address().port,server=createGateway({targets:[target]});server.listen(0,'127.0.0.1');await once(server,'listening');
 const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  for(const mobile of [false,true]){
   const page=await browser.newPage({viewport:mobile?{width:844,height:390}:{width:1280,height:800},hasTouch:mobile,isMobile:mobile,userAgent:mobile?'Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/128.0.0.0 Mobile Safari/537.36':'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36'});
   const logs=[],errors=[];page.on('console',m=>logs.push(m.text()));page.on('pageerror',e=>errors.push(String(e)));
   await page.route('**/config.js',route=>route.fulfill({contentType:'application/javascript',body:"window.NRO_WEB_CONFIG={serverList:'Fixture:"+target+":0,0,0'};"}));
   await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>gameInstance!==null,{},{timeout:180000});
   await page.locator('#tab2').click();await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game2'))).toBe(true);
   await page.setViewportSize(mobile?{width:320,height:568}:{width:640,height:480});
   if(mobile){await expect(page.locator('#rotate-device')).toBeVisible();await page.setViewportSize({width:568,height:320});await expect(page.locator('#rotate-device')).toBeHidden();}
   for(const tab of [1,2]){
    await page.locator('#tab'+tab).click();const before=handshakes;
    await expect.poll(async()=>{const box=await page.locator('#unity-canvas').boundingBox();if(mobile)await page.touchscreen.tap(box.x+box.width/2,box.y+box.height/2);else await page.mouse.click(box.x+box.width/2,box.y+box.height/2,{delay:120});return handshakes;},{timeout:30000,intervals:[1000,2000]}).toBeGreaterThan(before);
   }
   expect(errors).toEqual([]);expect(logs.filter(s=>/Exception:|RuntimeError:/.test(s))).toEqual([]);
   console.log('PASS '+(mobile?'mobile rotation':'PC resize')+': both real Unity server buttons respond at their new centered position');await page.close();
  }
 }finally{await browser.close();await server.shutdown();for(const s of sockets)s.destroy();await new Promise(resolve=>tcp.close(resolve));}
})().catch(e=>{console.error(e);process.exitCode=1;});
