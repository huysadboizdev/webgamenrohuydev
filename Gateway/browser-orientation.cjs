const {chromium,expect}=require('@playwright/test');
const {createGateway}=require('./server.cjs');
const {once}=require('node:events');
(async()=>{
 const server=createGateway();server.listen(0,'127.0.0.1');await once(server,'listening');
 const browser=await chromium.launch({headless:true});
 try {
  for(const [kind,ua,platform,touches] of [
   ['pc','Mozilla/5.0 (Windows NT 10.0; Win64; x64)','Win32',10],
   ['android','Mozilla/5.0 (Linux; Android 14) Mobile','Linux armv8l',5],
   ['ios','Mozilla/5.0 (iPhone; CPU iPhone OS 17_0)','iPhone',5],
   ['ipad','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15)','MacIntel',5]
  ]) {
   const page=await browser.newPage({viewport:{width:390,height:844},userAgent:ua});
   await page.addInitScript(({platform,touches})=>{Object.defineProperty(navigator,'platform',{get:()=>platform});Object.defineProperty(navigator,'maxTouchPoints',{get:()=>touches});},{platform,touches});
   await page.route('**/Build/*.loader.js',r=>r.fulfill({contentType:'application/javascript',body:'window.starts=0;window.pastes=[];window.createUnityInstance=async()=>{starts++;return {SendMessage:(host,method,value)=>{if(method==="SelectTab")NroTabs.changed(Number(value));if(method==="Paste")pastes.push(value);}}};'}));
   await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>gameInstance!==null);
   const gate=page.locator('#rotate-device');
   if(kind==='pc') {await expect(gate).toBeHidden();await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','1'));}
   else {
    await expect(gate).toBeVisible();await expect(gate).toContainText('xoay ngang');
    expect(await page.locator('#view').evaluate(e=>e.inert)).toBe(true);
    await page.evaluate(()=>{const event=new Event('paste',{cancelable:true});Object.defineProperty(event,'clipboardData',{value:{getData:()=> 'portrait-paste'}});document.dispatchEvent(event);});
    expect(await page.evaluate(()=>pastes)).toEqual([]);
    await page.setViewportSize({width:844,height:390});await expect(gate).toBeHidden();
    await page.evaluate(()=>gameInstance.SendMessage('BrowserHost','SelectTab','1'));await expect.poll(()=>page.evaluate(()=>NroTabs.current)).toBe(1);
    await page.evaluate(()=>NroInput.open('giá»¯ ná»™i dung',false,false));
    await page.setViewportSize({width:390,height:844});await expect(gate).toBeVisible();
    expect(await page.locator('#text-input').evaluate(e=>document.activeElement===e)).toBe(false);
    await page.setViewportSize({width:844,height:390});await expect(gate).toBeHidden();
    expect(await page.evaluate(()=>({starts,tab:NroTabs.current,value:NroInput.value(),state:NroInput.state()}))).toEqual({starts:1,tab:1,value:'giá»¯ ná»™i dung',state:1});
    await expect(page.locator('#keyboard')).toBeVisible();
   }
   console.log('PASS orientation '+kind+': portrait gate, landscape access, session/input preserved');await page.close();
  }
 } finally {await browser.close();await server.shutdown();}
})().catch(e=>{console.error(e);process.exitCode=1;});
