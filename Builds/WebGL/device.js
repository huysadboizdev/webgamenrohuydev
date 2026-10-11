(function (root) {
  'use strict';
  function classify(nav) {
    var ua = nav.userAgent || '';
    if (/Android/i.test(ua)) return 'android';
    if (/iPhone|iPad|iPod/i.test(ua) || (nav.platform === 'MacIntel' && nav.maxTouchPoints > 1)) return 'ios';
    return 'pc';
  }
  var api = { classify: classify };
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else root.NroDevice = api;
})(typeof window !== 'undefined' ? window : globalThis);
