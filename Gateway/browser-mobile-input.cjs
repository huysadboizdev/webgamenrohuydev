const {chromium, expect} = require('@playwright/test');
const net = require('node:net');
const {once} = require('node:events');
const fs = require('node:fs');
const path = require('node:path');
const {createGateway} = require('./server.cjs');

(async()=>{
  const clients=new Set(), connections=[];
  const tcp=net.createServer(socket=>{
    clients.add(socket);socket.on('close',()=>clients.delete(socket));
    const record={handshake:false,commands:[]};connections.push(record);
    let buffer=Buffer.alloc(0),cursor=0;
    socket.on('data',data=>{
      buffer=Buffer.concat([buffer,data]);
      if(!record.handshake){
        if(buffer.length<3)return;
        if(buffer[0]!==229 || buffer[1]!==0 || buffer[2]!==0){socket.destroy();return;}
        buffer=buffer.subarray(3);record.handshake=true;
        socket.write(Buffer.from([229,0,4,3,7,14,4]));
      }
      const key=[7,9,13];
      while(buffer.length>=3){
        let local=cursor;
        function read(index){const value=buffer[index]^key[local];local=(local+1)%key.length;return value;}
        const command=read(0),high=read(1),low=read(2),length=(high<<8)|low;
        if(buffer.length<3+length)return;
        const bytes=[];for(let i=0;i<length;i++)bytes.push(read(3+i));
        cursor=local;buffer=buffer.subarray(3+length);
        record.commands.push({command:command>127?command-256:command,length});
      }
    });
  });
  // The legacy client stores its configured port as a signed short.
  for(let attempt=0;;attempt++){
    try{tcp.listen(20000+Math.floor(Math.random()*10000),'127.0.0.1');await once(tcp,'listening');break;}
    catch(error){if(error.code!=='EADDRINUSE'||attempt>=20)throw error;}
  }
  const target='127.0.0.1:'+tcp.address().port;
  const server=createGateway({targets:[target]});server.listen(0,'127.0.0.1');await once(server,'listening');
  const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
  const logs=[],errors=[],dialogs=[];
  try{
    const page=await browser.newPage({viewport:{width:844,height:390},isMobile:true,hasTouch:true,userAgent:process.env.MOBILE_KIND==='ios'?'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Version/17.0 Mobile/15E148 Safari/604.1':'Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/128.0.0.0 Mobile Safari/537.36'});page.on('console',m=>logs.push(m.text()));
    page.on('pageerror',error=>errors.push(String(error)));
    page.on('dialog',async dialog=>{dialogs.push(dialog.message());await dialog.accept();});
    await page.route('**/config.js',route=>route.fulfill({contentType:'application/javascript',body:"window.NRO_WEB_CONFIG={serverList:'Fixture:"+target+":0,0,0'};"}));
    await page.goto('http://127.0.0.1:'+server.address().port);
    await page.waitForFunction(()=>typeof gameInstance!=='undefined'&&gameInstance!==null,{},{timeout:180000});
    await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game1')),{timeout:30000}).toBe(true);
    for(let tab=0;tab<2;tab++){
      if(tab===1){await page.locator('#tab2').click();await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game2'))).toBe(true);}
      const baseline=connections.filter(c=>c.handshake&&c.commands.length>0).length;
      await page.locator('#unity-canvas').focus();
      // Wait for the actual server-selection UI, then click its command.
      await page.waitForFunction(()=>new Promise(resolve=>{let remaining=75;function frame(){if(--remaining<=0)resolve(true);else requestAnimationFrame(frame);}requestAnimationFrame(frame);}));
      await page.waitForFunction(()=>document.activeElement.id==='unity-canvas');
      await expect.poll(async()=>{
        const box=await page.locator('#unity-canvas').boundingBox();
        const dimensions=await page.locator('#unity-canvas').evaluate(c=>({w:c.width,h:c.height}));
        const zoom=dimensions.w*dimensions.h>153600?2:1;
        await page.touchscreen.tap(box.x+box.width*0.5,box.y+box.height*0.5);
        return connections.filter(c=>c.handshake&&c.commands.length>0).length;
      },{timeout:30000,intervals:[500,1000,2000]}).toBeGreaterThan(baseline);
      async function frames(count=15){await page.evaluate(count=>new Promise(resolve=>{function next(){if(--count<=0)resolve();else requestAnimationFrame(next);}requestAnimationFrame(next);}),count);}
      async function click(dx,dy){const box=await page.locator('#unity-canvas').boundingBox();const size=await page.locator('#unity-canvas').evaluate(c=>({w:c.width,h:c.height}));const zoom=size.w*size.h>153600?2:1;await page.touchscreen.tap(box.x+box.width*(0.5+dx*zoom/size.w),box.y+box.height*(0.5+dy*zoom/size.h));await frames();}
      await frames(30);
      await page.locator('#unity-canvas').screenshot({path:path.join(__dirname,'../Tests/evidence/mobile-input-menu-'+(tab+1)+'.png')});
      await click(0,10); // Change account, the second menu button.
      await click(-2,3); // Actual username field.
      const account='click-test'+(tab+1);
      await expect(page.locator('#keyboard')).toBeVisible();
      await page.locator('#open-keyboard').tap();
      await expect(page.locator('#text-input')).toBeFocused();
      await page.locator('#text-input').fill(account);
      await page.getByRole('button',{name:'Xong',exact:true}).tap();
      await expect(page.locator('#keyboard')).toBeHidden();
      await frames(20);
      await page.locator('#unity-canvas').screenshot({path:path.join(__dirname,'../Tests/evidence/mobile-input-login-'+(tab+1)+'.png')});
      const saveBox=await page.locator('#unity-canvas').boundingBox();
      await page.touchscreen.tap(saveBox.x+saveBox.width/2,saveBox.y+saveBox.height-18);
      await frames(20); // OK is the center footer command on this landscape mobile layout.
      await expect.poll(()=>page.evaluate(async({account,tab})=>{
        for(const {name} of await indexedDB.databases()){
          if(!name.startsWith('/idbfs'))continue;
          const db=await new Promise((resolve,reject)=>{const request=indexedDB.open(name);request.onsuccess=()=>resolve(request.result);request.onerror=()=>reject(request.error);});
          try{
            if(!db.objectStoreNames.contains('FILE_DATA'))continue;
            const records=await new Promise((resolve,reject)=>{const values=[];const request=db.transaction('FILE_DATA').objectStore('FILE_DATA').openCursor();request.onsuccess=()=>{const cursor=request.result;if(!cursor){resolve(values);return;}values.push({key:cursor.key,value:cursor.value});cursor.continue();};request.onerror=()=>reject(request.error);});
            if(records.some(r=>String(r.key).includes('/nro-Tab'+tab+'/')&&r.value.contents&&new TextDecoder().decode(r.value.contents).includes(account)))return true;
          }finally{db.close();}
        }
        return false;
      },{account,tab:tab+1}),{timeout:15000}).toBe(true);
    }
    expect(errors).toEqual([]);expect(dialogs).toEqual([]);
    expect(logs.filter(s=>/RuntimeError|NullReferenceException|ArgumentException|TypeInitializationException/.test(s))).toEqual([]);
    fs.writeFileSync(path.join(__dirname,'../Tests/evidence/browser-mobile-input-'+(process.env.MOBILE_KIND||'android')+'.json'),JSON.stringify({fixture:true,device:process.env.MOBILE_KIND||'android',engine:'Chromium emulation',errors,dialogs,connections},null,2));
    console.log('PASS mobile emulation: touch selection, HTML keyboard, submit and saved account from real Unity in both tabs; no errors/dialogs');
  }catch(error){fs.writeFileSync(path.join(__dirname,'../Tests/evidence/mobile-input-console.log'),logs.join('\n'));for(const page of browser.contexts().flatMap(c=>c.pages()))await page.screenshot({path:path.join(__dirname,'../Tests/evidence/mobile-input-failure.png')});throw error;}
  finally{await browser.close();await server.shutdown();for(const client of clients)client.destroy();await new Promise(resolve=>tcp.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});


