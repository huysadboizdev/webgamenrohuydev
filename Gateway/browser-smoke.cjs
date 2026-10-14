const {chromium, devices} = require('@playwright/test');
const assert = require('node:assert/strict');
const {once} = require('node:events');
const path = require('node:path');
const fs = require('node:fs');
const {createGateway} = require('./server.cjs');

(async()=>{
  const output=path.join(__dirname,'../Tests/evidence');fs.mkdirSync(output,{recursive:true});
  const server=createGateway();server.listen(0,'127.0.0.1');await once(server,'listening');
  const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
  const results=[];
  try{
    for(const [name,options,mobile] of [
      ['pc',{viewport:{width:1280,height:800}},false],
      ['android',{...devices['Pixel 7'],viewport:{width:851,height:393}},true],
      ['ios',{...devices['iPhone 13'],viewport:{width:844,height:390}},true]
    ]){
      const context=await browser.newContext(options);const page=await context.newPage();
      const logs=[],errors=[];page.on('console',m=>logs.push(m.text()));page.on('pageerror',e=>errors.push(e.message));
      await page.goto('http://127.0.0.1:'+server.address().port,{waitUntil:'domcontentloaded'});
      await page.waitForFunction(()=>typeof gameInstance!=='undefined' && gameInstance!==null,{},{timeout:180000});
      await page.waitForFunction(()=>document.getElementById('status').style.display==='none',{},{timeout:180000});
      const expected='pc='+(!mobile?'True':'False')+' mobile='+(mobile?'True':'False');
      await require('@playwright/test').expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game1 '+expected)),{timeout:30000}).toBe(true);
      await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','1'));
      await require('@playwright/test').expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game2 '+expected)),{timeout:30000}).toBe(true);
      await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','0'));
      if(mobile){
        await page.evaluate(()=>NroInput.open('test',false,false));
        await page.locator('#text-input').fill('');
        await page.locator('#text-input').pressSequentially('mobile-keyboard');
        assert.equal(await page.locator('#text-input').inputValue(),'mobile-keyboard');
        await page.getByRole('button',{name:'Xong',exact:true}).click();
        assert.equal(await page.evaluate(()=>NroInput.state()),2);
        assert.equal(await page.evaluate(()=>NroInput.value()),'mobile-keyboard');
        await page.evaluate(()=>NroInput.close());
      }
      await page.locator('#unity-canvas').screenshot({path:path.join(output,name+'.png')});
      assert.deepEqual(errors,[],name+' JavaScript runtime errors');
      const runtimeErrors=logs.filter(s=>/Exception:|RuntimeError:|undefined symbol|NRO save sync failed/.test(s));
      assert.deepEqual(runtimeErrors,[],name+' Unity runtime errors');
      fs.writeFileSync(path.join(output,name+'-console.log'),logs.join('\n'));
      results.push({device:name,passed:true,twoTabs:true,mobileUI:mobile});
      console.log('PASS '+name+' actual Unity startup, two tabs'+(mobile?', mobile keyboard':''));
      await context.close();
    }
    fs.writeFileSync(path.join(output,'browser-results.json'),JSON.stringify(results,null,2));
  }finally{await browser.close();await server.shutdown();}
})().catch(error=>{console.error(error);process.exitCode=1;});
