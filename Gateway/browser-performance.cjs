const {chromium,expect}=require('@playwright/test');
const {createGateway}=require('./server.cjs');
const {once}=require('node:events');const fs=require('node:fs');
(async()=>{
 const server=createGateway();server.listen(0,'127.0.0.1');await once(server,'listening');
 const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 const logs=[],errors=[],samples=[];
 try{
  const page=await browser.newPage({viewport:{width:1920,height:1080}});
  page.on('console',m=>logs.push(m.text()));page.on('pageerror',e=>errors.push(e.message));
  await page.addInitScript(()=>{window.draws=0;for(const Type of [WebGLRenderingContext,WebGL2RenderingContext])for(const name of ['drawElements','drawArrays']){const old=Type.prototype[name];Type.prototype[name]=function(){window.draws++;return old.apply(this,arguments);};}});
  await page.goto('http://127.0.0.1:'+server.address().port);
  await page.waitForFunction(()=>gameInstance!==null,{},{timeout:180000});
  await expect.poll(()=>logs.some(s=>s.includes('NRO_WEB_DEVICE Game1')),{timeout:30000}).toBe(true);
  const cdp=await page.context().newCDPSession(page);await cdp.send('Performance.enable');
  for(const [width,height,tab] of [[1920,1080,1],[1920,1080,2],[1280,800,2]]){
   await page.setViewportSize({width,height});await page.evaluate(index=>gameInstance.SendMessage('BrowserHost','SelectTab',String(index)),tab-1);
   await page.evaluate(()=>new Promise(r=>{let n=90;function next(){if(--n)requestAnimationFrame(next);else r();}requestAnimationFrame(next);}));
   const before=await cdp.send('Performance.getMetrics');
   const timing=await page.evaluate(()=>new Promise(resolve=>{const frames=[];let last=performance.now(),drawLast=window.draws;function next(now){frames.push({ms:now-last,draws:window.draws-drawLast});last=now;drawLast=window.draws;if(frames.length===180)resolve({frames,canvas:{width:document.querySelector('canvas').width,height:document.querySelector('canvas').height}});else requestAnimationFrame(next);}requestAnimationFrame(next);}));
   const after=await cdp.send('Performance.getMetrics');const metric=(r,n)=>r.metrics.find(x=>x.name===n)?.value||0;
   const durations=timing.frames.slice(1).map(f=>f.ms).sort((a,b)=>a-b);
   const item={viewport:{width,height},tab,canvas:timing.canvas,medianMs:durations[Math.floor(durations.length/2)],p95Ms:durations[Math.floor(durations.length*.95)],meanDraws:timing.frames.reduce((n,f)=>n+f.draws,0)/timing.frames.length,taskSeconds:metric(after,'TaskDuration')-metric(before,'TaskDuration')};samples.push(item);console.log(JSON.stringify(item));
  }
  expect(errors).toEqual([]);
 }finally{fs.writeFileSync('../Tests/evidence/performance-'+(process.env.PERF_LABEL||'latest')+'.json',JSON.stringify({samples,errors},null,2));await browser.close();await server.shutdown();}
})().catch(e=>{console.error(e);process.exitCode=1;});
