const {chromium, expect} = require('@playwright/test');
const {createGateway} = require('./server.cjs');
const {once} = require('node:events');
const fs = require('node:fs');
const path = require('node:path');
const {spawnSync} = require('node:child_process');
const net=require('node:net');
const crypto=require('node:crypto');

(async()=>{
  const sockets=new Set();const tcp=net.createServer(socket=>{sockets.add(socket);socket.on('close',()=>sockets.delete(socket));let buffer=Buffer.alloc(0),ready=false;socket.on('data',b=>{if(ready)return;buffer=Buffer.concat([buffer,b]);if(buffer.length>=3){ready=true;socket.write(Buffer.from([229,0,4,3,7,14,4]));}});});tcp.listen(29999,'127.0.0.1');await once(tcp,'listening');
  const server=createGateway({targets:['127.0.0.1:29999']});server.listen(0,'127.0.0.1');await once(server,'listening');
  const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
  const evidence=path.join(__dirname,'../Tests/evidence');const errors=[],logs=[];
  try{
    const context=await browser.newContext({viewport:{width:800,height:360},deviceScaleFactor:3,isMobile:true,hasTouch:true,userAgent:'Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/128.0.0.0 Mobile Safari/537.36'});
    const page=await context.newPage();page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>logs.push(m.text()));
    await page.route('**/config.js',route=>route.fulfill({contentType:'application/javascript',body:"window.NRO_WEB_CONFIG={serverList:'Fixture:127.0.0.1:29999:0,0,0'};"}));
    await page.goto('http://127.0.0.1:'+server.address().port);
    if(await page.evaluate(()=>NroFullscreenController.isSupported))await page.getByRole('button',{name:'Để sau',exact:true}).tap();
    await page.waitForFunction(()=>window.gameInstance!==null,{},{timeout:180000});
    const versionKey=crypto.createHash('sha256').update('ResVersion').digest('hex').toUpperCase()+'.bin';
    await page.evaluate(key=>{for(const tab of [1,2]){gameInstance.Module.FS_createPath('/idbfs','nro-Tab'+tab,true,true);gameInstance.Module.FS_createDataFile('/idbfs/nro-Tab'+tab,key,new Uint8Array([0,1,49]),true,true);}},versionKey);
    for(const tab of [0,1,0]){
      if(tab!==0||logs.some(s=>s.includes('NRO_WEB_DEVICE Game2')))await page.evaluate(t=>gameInstance.SendMessage('BrowserHost','SelectTab',String(t)),tab);
      await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game'+(tab+1)))).toBe(true);
      if(!logs.some(s=>s.includes('background-fixture-tab'+tab))){const box=await page.locator('#unity-canvas').boundingBox();await page.touchscreen.tap(box.x+box.width/2,box.y+box.height/2);logs.push('background-fixture-tab'+tab);}
      await page.waitForFunction(()=>new Promise(resolve=>{let n=60;function tick(){if(--n<=0)resolve(true);else requestAnimationFrame(tick);}requestAnimationFrame(tick);}));
      const file=path.join(evidence,'background-clean-tab'+(tab+1)+'.png');await page.locator('#unity-canvas').screenshot({path:file});
      const script='from PIL import Image;import sys,json;im=Image.open(sys.argv[1]).convert("RGB");w,h=im.size;p=im.crop((0,h//5,w//4,h*3//4));pixels=list(p.getdata());colored=sum(max(c)-min(c)>35 and max(c)>80 for c in pixels)/len(pixels);print(json.dumps({"width":w,"height":h,"backgroundColoredFraction":colored}));sys.exit(0 if colored>.3 else 1)';
      const result=spawnSync('python',['-c',script,file],{encoding:'utf8'});
      console.log('tab'+(tab+1),result.stdout.trim());
      if(result.status!==0)throw new Error('Clean-cache menu background missing in tab'+(tab+1));
    }
    expect(errors).toEqual([]);console.log('PASS actual Unity clean-cache x4 backgrounds: tab1, tab2 and return to tab1');
  }finally{fs.writeFileSync(path.join(evidence,'background-clean-console.log'),logs.join('\n'));await browser.close();for(const socket of sockets)socket.destroy();await new Promise(r=>tcp.close(r));await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;});
