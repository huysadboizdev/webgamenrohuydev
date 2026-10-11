const {chromium,expect}=require('@playwright/test');
const fs=require('node:fs'),path=require('node:path');
// Exercise the actual template's input bridge without loading Unity.
const html=fs.readFileSync(path.join(__dirname,'../Assets/WebGLTemplates/NroWeb/index.html'),'utf8');
const markup=html.slice(html.indexOf('<div id="game">'),html.indexOf('<script src="device.js">'));
const bridge=html.slice(html.indexOf('  var keyboard ='),html.indexOf("  document.addEventListener('paste'"));
(async()=>{
 const browser=await chromium.launch({headless:true});
 try{
  for(const name of ['Android','iOS']){
   const page=await browser.newPage({viewport:{width:844,height:390},isMobile:true,hasTouch:true,userAgent:name==='iOS'?'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Version/17.0 Mobile Safari/604.1':'Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/128.0.0.0 Mobile Safari/537.36'});
   await page.setContent(markup+'<script>var canvas=document.getElementById("unity-canvas");'+bridge+'</script>');
   expect(await page.evaluate(()=>document.getElementById('game').contains(document.getElementById('keyboard'))),'Keyboard must belong to the fullscreen game element').toBe(true);
   await page.evaluate(()=>document.getElementById('fullscreen').onclick=()=>document.getElementById('game').requestFullscreen());
   await page.locator('#fullscreen').tap();
   await expect.poll(()=>page.evaluate(()=>document.fullscreenElement&&document.fullscreenElement.id)).toBe('game');
   // A deferred Unity callback has no trusted gesture. Reopen must provide one.
   await page.evaluate(()=>{window.NroInput.open('ban đầu',false,false);document.activeElement.blur();});
   await expect(page.getByRole('button',{name:'Mở bàn phím',exact:true})).toBeVisible();
   await page.getByRole('button',{name:'Mở bàn phím',exact:true}).tap();
   await expect(page.locator('#text-input')).toBeFocused();
   // Safari buttons can leave the input focused after the OS keyboard is dismissed.
   // Prevent Chromium's default button focus to exercise that same condition.
   await page.evaluate(()=>{
    window.refocusCount=0;
    document.getElementById('text-input').addEventListener('focus',()=>window.refocusCount++);
    document.getElementById('open-keyboard').addEventListener('pointerdown',event=>event.preventDefault());
   });
   await page.locator('#open-keyboard').tap();
   expect(await page.evaluate(()=>window.refocusCount),'Reopen must refocus an already focused input').toBe(1);
   await page.locator('#text-input').fill('Xin chào Đường Tăng');
   await page.getByRole('button',{name:'Xong',exact:true}).tap();
   expect(await page.evaluate(()=>NroInput.state())).toBe(2);
   expect(await page.evaluate(()=>NroInput.value())).toBe('Xin chào Đường Tăng');
   await page.evaluate(()=>NroInput.close());
   expect(await page.evaluate(()=>NroInput.state())).toBe(0);
   await page.evaluate(()=>NroInput.open('123',true,false));
   await expect(page.locator('#text-input')).toHaveAttribute('type','password');
   await page.evaluate(()=>NroInput.open('12',false,true));
   await expect(page.locator('#text-input')).toHaveAttribute('inputmode','numeric');
   await page.evaluate(()=>NroInput.close());
   await expect(page.locator('#keyboard')).toBeHidden();
   console.log('PASS '+name+' emulation in Chromium: fullscreen, trusted touch reopen, Unicode, submit, password/numeric and close');
   await page.close();
  }
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
