const {chromium,expect}=require('@playwright/test');
const {createGateway}=require('./server.cjs');
const {once}=require('node:events');
const fs=require('node:fs');
const path=require('node:path');
(async()=>{
 const server=createGateway();server.listen(0,'127.0.0.1');await once(server,'listening');
 const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:800}});
  await page.addInitScript(()=>{
   window.nroFrameDraws=[];window.nroLastDraws=0;window.nroDrawTotal=0;let draws=0;
   for(const Type of [window.WebGLRenderingContext,window.WebGL2RenderingContext]){
    if(!Type)continue;
    const originalClear=Type.prototype.clear;
    Type.prototype.clear=function(mask){if(mask&this.COLOR_BUFFER_BIT){window.nroFrameDraws.push(draws);draws=0;window.nroLastDraws=0;}return originalClear.call(this,mask);};
    for(const name of ['drawElements','drawArrays','drawElementsInstanced','drawArraysInstanced']){
     const original=Type.prototype[name];if(!original)continue;
     Type.prototype[name]=function(){draws++;window.nroLastDraws=draws;window.nroDrawTotal++;return original.apply(this,arguments);};
    }
   }
  });
  await page.goto('http://127.0.0.1:'+server.address().port);
  await page.waitForFunction(()=>window.gameInstance!==null&&document.getElementById('status').style.display==='none',{},{timeout:180000});
  await expect.poll(()=>page.evaluate(()=>nroFrameDraws.length),{timeout:20000}).toBeGreaterThan(100);
  const results=[];
  for(const tab of [1,2,1]){
   await page.locator('#tab'+tab).click();
   // Count submissions per animation frame, including offscreen GUI targets.
   const frames=await page.evaluate(()=>new Promise(resolve=>{let warmup=75,previous=nroDrawTotal;const frames=[];function sample(){const count=nroDrawTotal-previous;previous=nroDrawTotal;if(warmup-->0){requestAnimationFrame(sample);return;}frames.push(count);if(frames.length===60)resolve(frames);else requestAnimationFrame(sample);}requestAnimationFrame(sample);}));
   results.push({tab,frames});
   fs.writeFileSync(path.join(__dirname,'../Tests/evidence/render-frames.json'),JSON.stringify(results,null,2));
   console.log('Tab '+tab+' render draw counts:',frames.join(','));
   if(frames.some(count=>count===0))throw Error('Game render skipped on presented browser frames, tab '+tab);
  }
  console.log('PASS every presented browser frame contains game drawing, both tabs and return');
 }finally{await browser.close();await server.shutdown();}
})().catch(error=>{console.error(error);process.exitCode=1;});
