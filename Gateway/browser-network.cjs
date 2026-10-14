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
  const logs=[];
  try{
    const page=await browser.newPage({viewport:{width:1280,height:800}});page.on('console',m=>logs.push(m.text()));
    await page.route('**/config.js',route=>route.fulfill({contentType:'application/javascript',body:"window.NRO_WEB_CONFIG={serverList:'Fixture:"+target+":0,0,0'};"}));
    await page.goto('http://127.0.0.1:'+server.address().port);
    await page.waitForFunction(()=>typeof gameInstance!=='undefined'&&gameInstance!==null,{},{timeout:180000});
    await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game1')),{timeout:30000}).toBe(true);
    for(let tab=0;tab<2;tab++){
      if(tab===1){await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','1'));await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game2'))).toBe(true);}
      await page.locator('#unity-canvas').focus();
      // Enter selects the highlighted server; the next Enter selects play/download.
      await page.keyboard.press('Enter',{delay:150});
      await page.waitForFunction(()=>document.activeElement.id==='unity-canvas');
      await expect.poll(async()=>{
        await page.keyboard.press('Enter',{delay:150});return connections.filter(c=>c.handshake&&c.commands.length>0).length;
      },{timeout:30000,intervals:[500,1000,2000]}).toBeGreaterThanOrEqual(tab+1);
    }
    fs.writeFileSync(path.join(__dirname,'../Tests/evidence/browser-network.json'),JSON.stringify({fixture:true,connections},null,2));
    console.log('PASS actual WebGL → JS WebSocket → gateway → TCP: two independent key handshakes and encrypted client messages');
  }catch(error){fs.writeFileSync(path.join(__dirname,'../Tests/evidence/network-console.log'),logs.join('\n'));for(const page of browser.contexts().flatMap(c=>c.pages()))await page.screenshot({path:path.join(__dirname,'../Tests/evidence/network-failure.png')});throw error;}
  finally{await browser.close();await server.shutdown();for(const client of clients)client.destroy();await new Promise(resolve=>tcp.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
