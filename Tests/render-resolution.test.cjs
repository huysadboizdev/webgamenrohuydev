const test=require('node:test'),assert=require('node:assert/strict'),vm=require('node:vm'),fs=require('node:fs'),path=require('node:path');
const html=fs.readFileSync(path.join(__dirname,'../Assets/WebGLTemplates/NroWeb/index.html'),'utf8');
const resize=html.match(/function resize\(\)\{[^\n]+\}/)[0];
for(const [device,width,height] of [['pc',1920,1080],['pc',3840,2160],['pc',800,480],['android',390,780],['ios',844,390]]){
 test(`${device} ${width}x${height}: bounded rendering, full display and positive canvas`,()=>{
  const context={device,view:{clientWidth:width,clientHeight:height},canvas:{style:{}},Math};vm.runInNewContext(resize+';resize();',context);
  assert(context.canvas.width<=1024,'High-resolution screen multiplies render work');assert(context.canvas.width>0&&context.canvas.height>0);
  if(device==='pc'){assert(parseFloat(context.canvas.style.width)<=width);assert(parseFloat(context.canvas.style.height)<=height);assert(Math.abs(context.canvas.width/context.canvas.height-1024/600)<.01);if(width>=1920)assert(parseFloat(context.canvas.style.width)>1024,'Display must still fill available space');}
 });
}
test('Unity preserves explicitly bounded backing buffer',()=>assert(html.includes('matchWebGLToCanvasSize:false')));
