const fs = require('fs'), path = require('path'), assert = require('assert');
for (const game of ['Game1','Game2']) for (const session of ['Session_ME','Session_ME2']) {
  const file = path.join(__dirname,'../../Assets/Scripts/Assembly-CSharp',game,session+'.cs');
  const text = fs.readFileSync(file,'utf8').replace(/\r\n/g,'\n');
  assert(text.startsWith('#if !(UNITY_WEBGL && !UNITY_EDITOR) && !NRO_WEB_TRANSPORT_TEST'),game+'.'+session+' retains native branch');
  const browser = text.split('\n#else\n')[1]; assert(browser, 'browser branch exists');
  assert(!/TcpClient|Thread\.Sleep|new Thread|Thread\.Abort/.test(browser),'browser branch has no socket/thread APIs');
  assert(browser.includes('new WebSessionCore('+(session==='Session_ME'?'true':'false')), 'correct primary/secondary framing');
  assert(browser.includes('PacketItemOptionsInt32 = packet.ItemOptionsInt32') && browser.includes('PacketInventory200 = packet.Inventory200'), 'packet flags are snapped before dispatch');
}
console.log('PASS: native preservation and all four browser wrappers, primary/secondary protocol flags, no socket/thread APIs');
