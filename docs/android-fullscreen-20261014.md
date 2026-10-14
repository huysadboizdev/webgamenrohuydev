# Android web fullscreen

User requested an optional fullscreen offer for Android and preservation of the accepted iOS Safari layout/safe areas. This release implements section3A of the native-client parity plan; it does not claim full authenticated gameplay parity or physical-device acceptance.

## Behavior

- Supported Android browsers offer “Bật toàn màn hình” / “Để sau” once before Unity initialization. Unsupported browsers, PC and iOS bypass the offer.
- The fullscreen request is made directly in the tap callback. The root game element includes both canvas and HTML keyboard.
- After accepting, skipping or rejection, the shell settles its viewport across two animation frames and boots Unity once. A refused request does not prevent starting the game.
- Android Configuration has a “Toàn màn hình” item in both account clients. It opens the same DOM offer with enter/exit as appropriate; input is cleared before the offer. Fullscreen changes clear pending keyboard and held game input.
- Entering/exiting does not reload Unity, alter account saves or close sockets. Back/external fullscreen exit uses the browser fullscreenchange event.
- iOS safe-area layout and PC1024x600 policy remain unchanged. No fullscreen offer or added configuration item appears on iOS/PC.

## Tests

- `Tests/fullscreen-policy.test.cjs`:14 cases cover Android capability checks, skip, tap request, rejection/throw/nochange, repeated startup, runtime enter/exit, external exit and error events. Red before implementation, green afterward.
- `Tests/FullscreenMenuChecks.py`: executes actual C# option-label/action methods for both accounts. The Android setting was absent before the fix; afterward it invokes the fullscreen adapter while existing analog and iOS/PC settings remain unchanged.
- `Gateway/browser-fullscreen.cjs`: real Chromium fullscreen API with a fixture Unity loader verifies enter, menu exit, external exit, one boot, fallback and unchanged iOS/PC shell. It does not substitute for a real Unity-player run.
- Regression coverage includes the node suite, initial/responsive menu bounds, virtual controls, chat keyboard and account-tab guards.
- Final build: `WebGL-prj-fullscreen-final-20261014`, succeeded with0 errors/70 existing warnings. Backed up the previous active local build and verified installed hashes.
- Actual Unity player fixture passed Android Chromium emulation/DPR3 with the initial fullscreen choice, touch/input/save in both tabs, runtime fullscreen exit/reentry retaining the same Unity instance and connection count. iOS WebKit emulation/DPR3 and PC mouse fixtures also passed both tabs. No physical phone or authenticated game-map acceptance was performed.

## Deployment and limits

Build only the web player. Active release is under `Builds/WebGL`; local builds/backup metadata stay ignored. VPS deploys with `git pull --ff-only origin main` in the webgamenrohuydev checkout. Verify HTML and all four build files belong to the same release; purge stale cached HTML where needed.

Real phone testing is still required for Android system Back/navigation bars and Safari notch/Dynamic Island/keyboard behavior. Fullscreen availability and system navigation are browser/OS controlled. No APK/EXE/server JAR or SQL changes are part of this release.
