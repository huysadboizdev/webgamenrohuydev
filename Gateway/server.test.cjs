const test = require('node:test');
const assert = require('node:assert/strict');
const net = require('node:net');
const {once} = require('node:events');
const {WebSocket} = require('ws');
const fs = require('node:fs');
const api = fs.existsSync(__dirname + '/server.cjs') ? require('./server.cjs') : {};

test('gateway implementation available', () => assert.equal(typeof api.createGateway, 'function'));
test('two independent binary streams, handshake queue and rejected target', async (t) => {
  if (!api.createGateway) { assert.fail('gateway not implemented'); }
  const tcp = net.createServer(socket => socket.on('data', data => socket.write(data)));
  tcp.listen(0,'127.0.0.1'); await once(tcp,'listening');
  const target = '127.0.0.1:'+tcp.address().port;
  const gateway = api.createGateway({targets:[target],root:__dirname+'/../Builds/WebGL'});
  gateway.listen(0,'127.0.0.1'); await once(gateway,'listening');
  t.after(async () => { await gateway.shutdown(); await new Promise(resolve => tcp.close(resolve)); });
  const url = 'ws://127.0.0.1:'+gateway.address().port+'/gateway';
  const sockets = [new WebSocket(url),new WebSocket(url)];
  t.after(()=>sockets.forEach(s=>s.terminate()));
  await Promise.all(sockets.map(s=>once(s,'open')));
  const responses=sockets.map(s=>once(s,'message'));
  sockets[0].send(Buffer.from(target)); sockets[1].send(Buffer.from(target));
  const a=Buffer.from([0,255,128,27,0,2]); const b=Buffer.from([42,17,0,255]);
  sockets[0].send(a); sockets[1].send(b);
  const values=await Promise.all(responses);
  assert.deepEqual(values[0][0],a);assert.deepEqual(values[1][0],b);
  const bad=new WebSocket(url);await once(bad,'open');const closed=once(bad,'close');
  bad.send(Buffer.from('127.0.0.1:1'));assert.equal((await closed)[0],1008);
  sockets.forEach(s=>s.close());await Promise.all(sockets.map(s=>once(s,'close')));
});
test('static hosting exposes build only, traversals rejected', async(t)=>{
  if (!api.createGateway) assert.fail('gateway not implemented');
  const dir=fs.mkdtempSync(require('node:path').join(require('node:os').tmpdir(),'nro-test-'));
  fs.writeFileSync(dir+'/index.html','game');fs.writeFileSync(dir+'/client.wasm',Buffer.from([0,97,115,109]));
  const g=api.createGateway({targets:['127.0.0.1:14445'],root:dir});g.listen(0,'127.0.0.1');await once(g,'listening');
  t.after(async()=>{await g.shutdown();fs.rmSync(dir,{recursive:true,force:true});});
  const url='http://127.0.0.1:'+g.address().port;
  assert.equal(await (await fetch(url)).text(),'game');
  assert.equal((await fetch(url+'/client.wasm')).headers.get('content-type'),'application/wasm');
  assert.equal((await fetch(url+'/%2e%2e%2fserver.cjs')).status,403);
});

test('static files provide validators for Unity cache revalidation', async(t)=>{
  const dir=fs.mkdtempSync(require('node:path').join(require('node:os').tmpdir(),'nro-cache-test-'));
  fs.writeFileSync(dir+'/client.data',Buffer.from([1,2,3,4]));
  fs.mkdirSync(dir+'/Build');
  fs.writeFileSync(dir+'/Build/WebGL-release-20261015.data',Buffer.from([5,6,7,8]));
  fs.writeFileSync(dir+'/index.html','game');
  const g=api.createGateway({root:dir});g.listen(0,'127.0.0.1');await once(g,'listening');
  t.after(async()=>{await g.shutdown();fs.rmSync(dir,{recursive:true,force:true});});
  const url='http://127.0.0.1:'+g.address().port+'/client.data';
  const first=await fetch(url);
  const etag=first.headers.get('etag');
  const lastModified=first.headers.get('last-modified');
  assert.ok(etag,'GET should expose an ETag');
  assert.ok(lastModified,'GET should expose Last-Modified');
  assert.deepEqual(Buffer.from(await first.arrayBuffer()),Buffer.from([1,2,3,4]));

  const head=await fetch(url,{method:'HEAD'});
  assert.equal(head.status,200);
  assert.equal(head.headers.get('etag'),etag);
  assert.equal(head.headers.get('last-modified'),lastModified);
  assert.equal(head.headers.get('content-length'),'4');

  const unchanged=await fetch(url,{headers:{'If-None-Match':etag}});
  assert.equal(unchanged.status,304,'unchanged cached content should revalidate without downloading its body');
  assert.equal(unchanged.headers.get('etag'),etag);
  assert.equal(unchanged.headers.get('content-length'),null);

  const versionedAsset=await fetch(url.replace('/client.data','/Build/WebGL-release-20261015.data'));
  assert.equal(versionedAsset.headers.get('cache-control'),'public, max-age=31536000, immutable');
  const html=await fetch(url.replace('/client.data','/index.html'));
  assert.equal(html.headers.get('cache-control'),'no-cache','the shell must revalidate to discover each new build');
});
