(function(global){
  'use strict';
  var DB_NAME='NroWebDataCache', DB_VERSION=1, FILES='files', CHUNKS='chunks', CHUNK_SIZE=4*1024*1024;
  var dbPromise;

  function openDatabase(){
    if(!global.indexedDB)return Promise.reject(new Error('IndexedDB unavailable'));
    if(!dbPromise)dbPromise=new Promise(function(resolve,reject){
      var request=global.indexedDB.open(DB_NAME,DB_VERSION);
      request.onupgradeneeded=function(){
        var db=request.result;
        if(!db.objectStoreNames.contains(FILES))db.createObjectStore(FILES,{keyPath:'key'});
        if(!db.objectStoreNames.contains(CHUNKS)){
          var store=db.createObjectStore(CHUNKS,{keyPath:'id'});
          store.createIndex('fileKey','fileKey',{unique:false});
        }
      };
      request.onsuccess=function(){resolve(request.result);};
      request.onerror=function(){reject(request.error||new Error('Could not open data cache'));};
      request.onblocked=function(){reject(new Error('Data cache database upgrade blocked'));};
    }).catch(function(error){dbPromise=null;throw error;});
    return dbPromise;
  }

  function requestValue(request){return new Promise(function(resolve,reject){request.onsuccess=function(){resolve(request.result);};request.onerror=function(){reject(request.error||new Error('IndexedDB request failed'));};});}
  function transactionDone(transaction){return new Promise(function(resolve,reject){transaction.oncomplete=resolve;transaction.onabort=transaction.onerror=function(){reject(transaction.error||new Error('IndexedDB transaction failed'));};});}

  async function readFile(db,key){
    var tx=db.transaction([FILES,CHUNKS],'readonly');
    var metaRequest=tx.objectStore(FILES).get(key);
    var chunksRequest=tx.objectStore(CHUNKS).index('fileKey').getAll(key);
    var values=await Promise.all([requestValue(metaRequest),requestValue(chunksRequest),transactionDone(tx)]);
    var meta=values[0],chunks=values[1];
    if(!meta||chunks.length!==meta.chunkCount)return null;
    chunks.sort(function(a,b){return a.index-b.index;});
    var bytes=new Uint8Array(meta.size),offset=0;
    for(var i=0;i<chunks.length;i++){
      var part=new Uint8Array(chunks[i].body);
      bytes.set(part,offset);offset+=part.byteLength;
    }
    return offset===meta.size?{meta:meta,bytes:bytes}:null;
  }

  async function removeFile(db,key){
    var tx=db.transaction([FILES,CHUNKS],'readwrite'),store=tx.objectStore(CHUNKS),keys=await requestValue(store.index('fileKey').getAllKeys(key));
    tx.objectStore(FILES).delete(key);
    for(var i=0;i<keys.length;i++)store.delete(keys[i]);
    await transactionDone(tx);
  }

  async function writeFile(db,key,version,url,response,bytes){
    await removeFile(db,key);
    var chunkCount=Math.ceil(bytes.byteLength/CHUNK_SIZE);
    for(var index=0;index<chunkCount;index++){
      var start=index*CHUNK_SIZE,end=Math.min(start+CHUNK_SIZE,bytes.byteLength);
      var tx=db.transaction(CHUNKS,'readwrite');
      tx.objectStore(CHUNKS).put({id:key+'#'+index,fileKey:key,index:index,body:bytes.buffer.slice(start,end)});
      await transactionDone(tx);
    }
    var meta={key:key,url:url,version:version,size:bytes.byteLength,chunkCount:chunkCount,etag:response.headers.get('ETag'),lastModified:response.headers.get('Last-Modified')};
    var metaTx=db.transaction(FILES,'readwrite');metaTx.objectStore(FILES).put(meta);await transactionDone(metaTx);
  }

  function report(options,loaded,total,type){
    if(options&&typeof options.onProgress==='function')options.onProgress({type:type||'progress',loaded:loaded,total:total,lengthComputable:total>0});
  }

  async function readResponse(response,options){
    if(!response.ok)throw new Error('Data request failed: '+response.status+' '+response.statusText);
    var total=Number(response.headers.get('Content-Length'))||0,reader=response.body&&response.body.getReader(),parts=[],length=0;
    if(reader){
      while(true){var result=await reader.read();if(result.done)break;parts.push(result.value);length+=result.value.byteLength;report(options,length,total,'progress');}
    }else{var body=await response.arrayBuffer();parts=[new Uint8Array(body)];length=body.byteLength;}
    var bytes=new Uint8Array(length),offset=0;
    for(var i=0;i<parts.length;i++){bytes.set(parts[i],offset);offset+=parts[i].byteLength;}
    parts.length=0;report(options,length,total||length,'load');
    return bytes;
  }

  function validatorsMatch(meta,response){
    var etag=response.headers.get('ETag'),lastModified=response.headers.get('Last-Modified');
    var compared=false;
    if(meta.etag&&etag){compared=true;if(meta.etag!==etag)return false;}
    if(meta.lastModified&&lastModified){compared=true;if(meta.lastModified!==lastModified)return false;}
    return compared;
  }

  async function cachedFetch(url,options){
    options=options||{};
    var absoluteUrl=new URL(typeof url==='string'?url:url.url,document.baseURI).href;
    var version=String(options.productVersion||'1'),key=absoluteUrl+'\n'+version,db=null;
    try{
      db=await openDatabase();
      var cached=await readFile(db,key);
      if(cached){
        try{
          var head=await fetch(absoluteUrl,{method:'HEAD',cache:'no-store',credentials:'same-origin'});
          if(head.ok&&validatorsMatch(cached.meta,head)){
            report(options,cached.bytes.byteLength,cached.bytes.byteLength,'load');
            return {parsedBody:cached.bytes};
          }
        }catch(_){
          report(options,cached.bytes.byteLength,cached.bytes.byteLength,'load');
          return {parsedBody:cached.bytes};
        }
      }
    }catch(error){
      if(global.console&&console.warn)console.warn('[NroDataCache] IndexedDB read failed; downloading data:',error);
    }

    var response=await fetch(absoluteUrl,{method:'GET',cache:'reload',credentials:'same-origin'});
    var bytes=await readResponse(response,options);
    if(db){
      try{await writeFile(db,key,version,absoluteUrl,response,bytes);}
      catch(error){
        try{await removeFile(db,key);}catch(_){}
        if(global.console&&console.warn)console.warn('[NroDataCache] Could not persist data:',error);
      }
    }
    return {parsedBody:bytes};
  }

  global.NroDataCache={cachedFetch:cachedFetch};
})(window);
