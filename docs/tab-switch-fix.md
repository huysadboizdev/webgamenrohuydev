# Web tab switch repair ? 2026-10-11

User reports in-game ??i Tab2 does not work on web. The in-game button calls Management.ChangeTab from GameCanvas.paint; browser header uses BrowserHost.SelectTab. Previously only the header path cleared focused fields and updated buttons. Both Main.OnGUI instances read the same Unity input frame; after changing Management.tab the destination could receive the same release. Repeated canvas switching reproduced unstable selection; header remained on tab1 when game2 was open.

Changes: centralize browser tab notification in Management; reset pointer/key/focus state for either entry point; prevent the newly selected tab from processing input for the remainder of that frame; update the browser header from Unity's actual selection callback. Native input timing remains unchanged. Existing scene loading and two independent sessions are preserved.

RED: TabSwitchChecks.py compiling actual Management and first input guard of each Main failed on switch-frame input isolation. Browser header assertion after clicking the actual in-game switch button failed (tab2 button inactive). GREEN: controlled-frame checks pass. Browser integration and Unity build results recorded in Tests/evidence/verification.md after completion.
