'use strict';
const {chromium, devices, expect} = require('@playwright/test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const WebSocket = require('ws');
const output=path.resolve(__dirname,'../Tests/evidence/prj-release-20261014');
fs.mkdirSync(output,{recursive:true});
async function handshake() {
  return new Promise((resolve,reject)=>{
    const ws=new WebSocket('ws://127.0.0.1:8086/gateway',{origin:'http://127.0.0.1:8086'});
    const timer=setTimeout(()=>{ws.terminate();reject(Error('Local TCP key handshake timeout'));},5000);
    ws.on('open',()=>{ws.send(Buffer.from('127.0.0.1:14445'));ws.send(Buffer.from([229,0,0]));});
    let response=Buffer.alloc(0),finished=false;
    ws.on('message',bytes=>{
      if(finished)return;
      response=Buffer.concat([response,bytes]);
      if(response.length<3)return;
      if(response[0]!==229){clearTimeout(timer);ws.close();reject(Error('Unexpected key response'));return;}
      const length=response.readUInt16BE(1);
      if(response.length<3+length)return;
      finished=true;clearTimeout(timer);
      ws.close();resolve({target:'127.0.0.1:14445',keyResponseBytes:3+length});
    });
    ws.on('error',error=>{clearTimeout(timer);reject(error);});
    ws.on('close',(code,reason)=>{clearTimeout(timer);if(code!==1000)reject(Error('Gateway closed '+code+' '+reason));});
  });
}
(async()=>{
  const servedHtml = await (await fetch('http://127.0.0.1:8086/?v=prj-20261014')).text();
  assert(servedHtml.includes('WebGL-prj-staging-20261014.data'), 'Gateway must serve the new PRJ artifact');
  const network=await handshake();
  console.log('PASS active local gateway forwards TCP key handshake');
  const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
  const results=[];
  try {
    for(const [name,options,mobile] of [
      ['pc',{viewport:{width:1280,height:800}},false],
      ['android',{...devices['Pixel 7'],viewport:{width:851,height:393}},true],
      ['ios',{...devices['iPhone 13'],viewport:{width:844,height:390}},true]
    ]) {
      const context=await browser.newContext(options);const page=await context.newPage();const logs=[],errors=[];
      page.on('console',msg=>logs.push(msg.text()));page.on('pageerror',err=>errors.push(err.message));
      await page.goto('http://127.0.0.1:8086/?v=prj-20261014',{waitUntil:'domcontentloaded'});
      await page.waitForFunction(()=>window.gameInstance && document.getElementById('status').style.display==='none',null,{timeout:180000});
      const expected='pc='+(!mobile?'True':'False')+' mobile='+(mobile?'True':'False');
      await expect.poll(()=>logs.some(msg=>msg.includes('NRO_WEB_DEVICE Game1 '+expected)),{timeout:30000}).toBe(true);
      assert.equal(await page.evaluate(()=>NroDevice.classify(navigator)),name);
      await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','1'));
      await expect.poll(()=>logs.some(msg=>msg.includes('NRO_WEB_DEVICE Game2 '+expected)),{timeout:30000}).toBe(true);
      await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','0'));
      if(mobile) {
        await page.evaluate(()=>NroInput.open('',false,false));
        await page.locator('#text-input').fill('test-local');
        await page.locator('#keyboard button[type=submit]').click();
        assert.equal(await page.evaluate(()=>NroInput.value()),'test-local');
        assert.equal(await page.evaluate(()=>NroInput.state()),2);
        await page.evaluate(()=>NroInput.close());
      }
      assert.deepEqual(errors,[]);
      assert.deepEqual(logs.filter(msg=>/Exception:|RuntimeError:|undefined symbol|NRO save sync failed/.test(msg)),[]);
      await page.screenshot({path:path.join(output,name+'.png')});
      fs.writeFileSync(path.join(output,name+'.log'),logs.join('\n'));
      results.push({device:name,bothAccounts:true,mobileControls:mobile,passed:true});
      console.log('PASS '+name+' actual Unity device mode, both accounts'+(mobile?', browser keyboard':''));
      await context.close();
    }
    fs.writeFileSync(path.join(output,'results.json'),JSON.stringify({network,results,releaseBuild:true,gameplaySourceNotBuilt:false,source:"PRJ_2Tab_550K",scriptswebRole:"reference only"},null,2));
  } finally {await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
