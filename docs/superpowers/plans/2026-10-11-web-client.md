# Web Client Implementation Plan

> **For agentic workers:** Use subagent-driven-development for the session transport task and review; controller integrates browser platform, gateway and build. Steps use checkbox syntax.

**Goal:** Deliver separate playable WebGL client with two account tabs and automatic PC/Android/iOS UI.

**Architecture:** Preserve original Unity scenes and game protocol; add shared browser infrastructure and independent transport per session. A configurable gateway forwards WebSocket bytes to the existing TCP server.

**Tech Stack:** Unity 2022.3.62f2, C#, JavaScript jslib, Node.js, Python verification.

## Task 1: Standalone source snapshot
- [x] Copy Assets/Packages/ProjectSettings/Tests to ClientWeb/PRJ_2Tab_Web; omit Library/Temp/Builds/.git.
- [x] Record approved design; inspect Unity WebGL module and source protocol.

## Task 2: Browser transport
- [x] Create runtime WebSocket bridge and buffered XOR packet codec in Assets/Scripts/Assembly-CSharp/Web.
- [x] Test fragmented handshake, complete/partial encrypted packets, extended lengths and negotiation before integrating.
- [x] Add WebGL implementations to Game1/Game2 Session_ME and Session_ME2 while retaining native implementations.
- [x] Implement matching jslib callbacks with per-connection identifiers and stale callback protection.

## Task 3: Device and browser runtime
- [x] Test actual JS device classification for Windows, desktop touchscreen, Android, iPhone and iPadOS.
- [x] Add shared platform bridge, responsive template, keyboard and save integration.
- [x] Adapt both Main classes and thread-based auto features to coroutines without blocking render loop.

## Task 4: Gateway and delivery
- [x] Implement configurable allowlisted binary WS-to-TCP gateway and static build hosting.
- [x] Test against an actual TCP fixture for binary data, two independent connections, disconnect and rejected targets.
- [x] Add Unity batch build entry point and Windows build/run scripts.
- [x] Build WebGL; inspect compiler/build output, test browser startup and verify device presentation.
- [x] Review requirements first, then code quality; fix findings, record verification and README.

## Verification commands
Node: `node --test Gateway/*.test.cjs Tests/*.test.cjs`.
Unity: `Unity.exe -batchmode -nographics -quit -projectPath <project> -buildTarget WebGL -executeMethod WebClientBuild.Run -logFile <project>/Logs/web-build.log`.
Unity regression: `Unity.exe -batchmode -nographics -quit -projectPath <project> -executeMethod WebTransportRegression.Run -logFile <project>/Logs/web-regression.log`. `WebClientBuild.Run` also runs storage and viewport regressions before building.
Review project source against original for scoped changes. Record any unverified real-server gameplay explicitly.
