'use strict';
const http = require('node:http');
const net = require('node:net');
const fs = require('node:fs');
const path = require('node:path');
const {WebSocketServer, WebSocket} = require('ws');

function createGateway(options = {}) {
  const root = path.resolve(options.root || path.join(__dirname,'../Builds/WebGL'));
  const diagnosticsRoot = path.resolve(options.diagnosticsRoot || path.join(root,'..','Logs'));
  const allowed = new Set(options.targets || ['127.0.0.1:14445']);
  const maxBuffer = 8 * 1024 * 1024, maxDiagnosticBody = 64 * 1024;
  const mime = {'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.json':'application/json','.png':'image/png','.css':'text/css'};
  const server = http.createServer((req,res) => {
    let requestPath;
    try { requestPath = new URL(req.url,'http://local').pathname; } catch { res.writeHead(400);res.end();return; }
    if (req.method === 'POST' && requestPath === '/diagnostics') {
      const origin = req.headers.origin;
      const protocol = req.socket.encrypted ? 'https:' : 'http:';
      let sameOrigin = false;
      try { const parsed = new URL(origin); sameOrigin = parsed.origin === protocol + '//' + req.headers.host; } catch {}
      if (!sameOrigin && !(options.origins || []).includes(origin)) { res.writeHead(403);res.end('Origin rejected.');return; }
      if (!/^application\/json(?:\s*;|$)/i.test(req.headers['content-type'] || '')) { res.writeHead(415);res.end('JSON required.');return; }
      let body = '', tooLarge = false;
      req.on('data', chunk => {
        if (tooLarge) return;
        body += chunk.toString('utf8');
        if (Buffer.byteLength(body) > maxDiagnosticBody) { tooLarge = true;res.writeHead(413);res.end('Log batch too large.');req.destroy(); }
      });
      req.on('end', async () => {
        if (tooLarge || res.writableEnded) return;
        let payload;
        try {
          payload = JSON.parse(body);
        } catch {
          res.writeHead(400);res.end('Invalid log batch.');return;
        }
        if (!payload || typeof payload.sessionId !== 'string' || !/^[A-Za-z0-9_-]{8,64}$/.test(payload.sessionId)
            || !Array.isArray(payload.lines) || payload.lines.length < 1 || payload.lines.length > 100
            || payload.lines.some(line => typeof line !== 'string' || line.length > 512 || /[\r\n]/.test(line))) {
          res.writeHead(400);res.end('Invalid log batch.');return;
        }
        try {
          await fs.promises.mkdir(diagnosticsRoot,{recursive:true});
          const file = path.join(diagnosticsRoot,'combat-'+payload.sessionId+'.ndjson');
          const receivedAt = new Date().toISOString();
          const records = payload.lines.map(event => JSON.stringify({receivedAt,sessionId:payload.sessionId,event})).join('\n')+'\n';
          await fs.promises.appendFile(file,records,'utf8');
          res.writeHead(204);res.end();
        } catch (error) {
          console.error('Combat log write failed:',error.message);
          res.writeHead(500);res.end('Combat log could not be saved.');
        }
      });
      return;
    }
    if (req.method !== 'GET' && req.method !== 'HEAD') {res.writeHead(405);res.end();return;}
    let pathname;
    try {pathname = decodeURIComponent(new URL(req.url,'http://local').pathname);} catch {res.writeHead(400);res.end();return;}
    const file = path.resolve(root,'.'+(pathname==='/'?'/index.html':pathname));
    if (!file.startsWith(root+path.sep) || pathname.includes('\\') || pathname.includes('\0')) {res.writeHead(403);res.end();return;}
    fs.stat(file,(error,stat) => {
      if(error || !stat.isFile()){res.writeHead(404);res.end('Build WebGL first, then run Gateway/run-web.cmd.');return;}
      const lastModified=stat.mtime.toUTCString();
      const etag=`W/"${stat.size.toString(16)}-${Math.trunc(stat.mtimeMs*1000).toString(16)}"`;
      const versionedBuildAsset=pathname.startsWith('/Build/') && /^WebGL-[\w-]+-\d{8}\.(?:data|framework\.js|loader\.js|wasm)$/.test(path.basename(file));
      const headers={'Content-Type':mime[path.extname(file)]||'application/octet-stream','Content-Length':stat.size,'Cache-Control':versionedBuildAsset?'public, max-age=31536000, immutable':'no-cache','ETag':etag,'Last-Modified':lastModified,'X-Content-Type-Options':'nosniff'};
      const ifNoneMatch=req.headers['if-none-match'];
      const matchesEtag=ifNoneMatch && (ifNoneMatch.trim()==='*' || ifNoneMatch.split(',').some(value=>value.trim().replace(/^W\//,'')===etag.replace(/^W\//,'')));
      const ifModifiedSince=req.headers['if-modified-since'];
      const notModified=matchesEtag || (!ifNoneMatch && ifModifiedSince && !Number.isNaN(Date.parse(ifModifiedSince)) && Math.floor(stat.mtimeMs/1000)<=Math.floor(Date.parse(ifModifiedSince)/1000));
      if(notModified){delete headers['Content-Length'];res.writeHead(304,headers);res.end();return;}
      res.writeHead(200,headers);
      if(req.method==='HEAD'){res.end();return;}
      const stream=fs.createReadStream(file);stream.on('error',()=>res.destroy());stream.pipe(res);
    });
  });
  const wss = new WebSocketServer({noServer:true,maxPayload:maxBuffer,perMessageDeflate:false});
  server.on('upgrade',(request,socket,head)=>{
    if (new URL(request.url,'http://local').pathname !== '/gateway') {socket.destroy();return;}
    // Default accepts browser origin of this host. Override only for a trusted separate host.
    const origin=request.headers.origin;
    if(origin){let ok=false;try{ok=new URL(origin).host===request.headers.host || (options.origins||[]).includes(origin);}catch{}if(!ok){socket.destroy();return;}}
    wss.handleUpgrade(request,socket,head,ws=>wss.emit('connection',ws,request));
  });
  wss.on('connection', ws => {
    let tcp=null, routed=false, connected=false, pending=[], pendingBytes=0;
    const timer=setTimeout(()=>ws.close(1008,'Missing target'),5000);timer.unref();
    const fail=()=>{if(tcp)tcp.destroy();if(ws.readyState===WebSocket.OPEN)ws.close(1011,'TCP unavailable');};
    ws.on('error',()=>{if(tcp)tcp.destroy();});
    ws.on('close',()=>{clearTimeout(timer);if(tcp)tcp.destroy();pending=[];});
    ws.on('message',(data,binary)=>{
      if(!binary){ws.close(1003,'Binary messages required');return;}
      if(!routed){
        const target=data.toString('utf8');
        if(data.length>256 || !allowed.has(target)){ws.close(1008,'Target not allowed');return;}
        const split=target.lastIndexOf(':');const host=target.slice(0,split);const port=Number(target.slice(split+1));
        if(split<1 || !Number.isInteger(port) || port<1 || port>65535){ws.close(1008,'Invalid target');return;}
        routed=true;clearTimeout(timer);tcp=net.createConnection({host,port});tcp.setNoDelay(true);
        tcp.setTimeout(300000,()=>{tcp.destroy();ws.close(1001,'TCP idle');});
        tcp.on('connect',()=>{connected=true;for(const item of pending)tcp.write(item);pending=[];pendingBytes=0;});
        tcp.on('data',bytes=>{
          if(ws.readyState!==WebSocket.OPEN)return;
          if(ws.bufferedAmount>maxBuffer){fail();return;}
          tcp.pause();ws.send(bytes,{binary:true},error=>{if(error)fail();else if(tcp && !tcp.destroyed)tcp.resume();});
        });
        tcp.on('error',fail);tcp.on('close',()=>{if(ws.readyState===WebSocket.OPEN)ws.close(1000,'TCP closed');});
        return;
      }
      if(!connected){pendingBytes+=data.length;if(pendingBytes>maxBuffer){fail();return;}pending.push(data);}
      else if(!tcp.destroyed){if(tcp.writableLength>maxBuffer){fail();return;}tcp.write(data);}
    });
  });
  server.shutdown=async()=>{
    for(const client of wss.clients)client.terminate();
    await new Promise(resolve=>wss.close(resolve));
    if(server.listening){server.closeAllConnections();await new Promise(resolve=>server.close(resolve));}
  };
  return server;
}
module.exports={createGateway};
if(require.main===module){
  const config=JSON.parse(fs.readFileSync(path.join(__dirname,'config.json'),'utf8'));
  const server=createGateway({...config,root:path.resolve(__dirname,config.root)});
  server.listen(config.port,config.bind,()=>console.log(`Game: http://localhost:${config.port} | targets: ${config.targets.join(', ')}`));
  for(const signal of ['SIGINT','SIGTERM'])process.on(signal,async()=>{await server.shutdown();process.exit(0);});
}
