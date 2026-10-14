# iPhone Safari: native mobile density and visible viewport

The reported default-iPhone Safari screenshot showed oversized menu controls and a clipped last button. Public HTML/device.js on play.ngocrongbear.online matched the current tradefix release, so this was not an outdated release.

## Causes and changes

- Mobile canvas used CSS pixels and a 1024/768 cap. Native PRJ uses physical screen pixels, then MotherCanvas selects resource zoom. For an 844x390 CSS screen at density3, the web buffer844x390 selected zoom2; native2532x1170 selects zoom4. The old browser menu was about50% larger in CSS space.
- Mobile canvas now uses CSS size times devicePixelRatio. Unity auto density remains disabled so it cannot multiply the buffer a second time. PC still uses its native1024x600 buffer and fits smaller windows.
- The game root follows visualViewport dimensions/offset and its resize/scroll events, accounting for Safari chrome and the software keyboard. HTML input is positioned inside that visible root.
- Both account menus apply browser bounds during initCommand, including cold start and rebuilding the four/five-command menu. Native menu positions/actions are unchanged. Resource zoom is not changed on every resize, preserving the loaded resource/font set.
- Browser server selection centers the actual grid width without a trailing gap so narrow viewports retain valid button bounds.

## Verification

- New viewport tests failed before the fix (844 buffer pixels instead of2532 and no visible-viewport handling); afterward10/10 passed. Full Node suite46/46 passed.
- InitialMobileMenuChecks executes extracted production C# methods in memory. Before fix, a five-item menu at logicalheight174 had final hitboxbottom186. Both tabs now pass cold/reinitialized menus at174/188/210, saved/guest account actions and unchanged native layout.
- ResponsiveMenuChecks passes both tabs after adding the missing BrowserPlatform fixture stub.
- Actual Unity player tests: WebKit iOS emulation844x280/DPR3, Chromium iOS emulation667x260/DPR2, and PC mouse. Both tabs pass touch/mouse selection, input submission and isolated saved accounts.
- Playwright Windows WebKit reports its unsupported Web Audio alert; the fixture records this exact environment warning separately. Other dialogs and runtime errors remain test failures. This is WebKit emulation, not a physical iPhone test.
- WebGL iosfix build succeeded with0 errors/70 existing warnings; active local build was backed up and replaced. No server JAR, APK, EXE or database changes in this fix.

## VPS update

The user deploys through git pull. Pull main in the webgamenrohuydev checkout; the active release is tracked under Builds/WebGL. The gateway serves that directory without rebuilding Unity. Clear/purge stale cached HTML if the CDN still references tradefix filenames. Verify the domain HTML references WebGL-prj-iosfix-20261014 and open it with a new query version.

Local URL: http://localhost:8086/?v=WebGL-prj-iosfix-20261014

The agent has no configured SSH deployment session for this VPS. GitHub publication does not by itself verify the remote deployment.
