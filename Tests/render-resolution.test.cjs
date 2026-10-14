const test=require('node:test'),assert=require('node:assert/strict'),vm=require('node:vm'),fs=require('node:fs'),path=require('node:path');
for(const file of ['Assets/WebGLTemplates/NroWeb/index.html','Builds/WebGL/index.html']) {
 const html=fs.readFileSync(path.join(__dirname,'..',file),'utf8');
 const resize=html.match(/function resize\(\)\{[^\n]+\}/)[0];
 for(const [width,height] of [[1920,926],[3840,2160],[1280,800],[800,480],[320,568]]){
  test(`${file}: PC ${width}x${height} keeps native 1024x600 without enlargement`,()=>{
   const context={device:'pc',view:{clientWidth:width,clientHeight:height},canvas:{style:{}},Math};vm.runInNewContext(resize+';resize();',context);
   assert.equal(context.canvas.width,1024);assert.equal(context.canvas.height,600);
   const scale=Math.min(1,width/1024,height/600);
   assert.equal(parseFloat(context.canvas.style.width),1024*scale);
   assert.equal(parseFloat(context.canvas.style.height),600*scale);
   assert(context.canvas.style.width.endsWith('px'));assert(context.canvas.style.height.endsWith('px'));
  });
 }
 for(const [device,width,height] of [['android',390,780],['android',844,390],['ios',844,390]]){
  test(`${file}: ${device} uses mobile viewport`,()=>{
   const c={device,view:{clientWidth:width,clientHeight:height},canvas:{style:{}},Math};vm.runInNewContext(resize+';resize();',c);
   assert(c.canvas.width<=1024&&c.canvas.height<=768);assert(c.canvas.width>0&&c.canvas.height>0);
   assert.equal(c.canvas.style.width,'100%');assert.equal(c.canvas.style.height,'100%');
   assert(Math.abs(c.canvas.width/c.canvas.height-width/height)<.01);
  });
 }
 test(`${file}: browser pixel ratio cannot magnify the canvas`,()=>assert(html.includes('matchWebGLToCanvasSize:false')&&html.includes('devicePixelRatio:1')));
}
