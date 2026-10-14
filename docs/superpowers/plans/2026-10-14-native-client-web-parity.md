# Native client to web parity implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bản web giữ hành vi và giao diện của app PRJ tương ứng với PC hoặc mobile, được nghiệm thu cả đăng nhập và gameplay Java trên thiết bị thật.

**Architecture:** Game1/Game2 và tài nguyên native là chuẩn; chỉ thay lớp browser adapter khi có lỗi được tái hiện. Giữ bố cục/zoom/input/protocol/storage cùng một hợp đồng; dùng app đã build, fixture và phiên Java test để chứng minh từng nhóm. Triển khai cuối cùng phục vụ một bộ WebGL nhất quán qua gateway hiện có.

**Tech Stack:** Unity2022.3.62f2, C#/IL2CPP/WebGL, HTML/JavaScript, Node/WebSocket gateway, Java server, Python/C# regression và Playwright Chromium/WebKit.

**Spec:** `docs/superpowers/specs/2026-10-14-native-client-web-parity.md`.

**Phạm vi lượt lập kế hoạch:** Chỉ viết tài liệu. Không sửa app/web, không build và không push tài liệu tự động.

## 1. Chốt app chuẩn và dữ liệu đối chiếu

**Files:** tạo `docs/parity/native-reference.json`, `docs/parity/acceptance.md`; bằng chứng đặt dưới `Tests/evidence/native-parity/` đang được loại khỏi Git.

- [ ] Xác định đúng EXE đang được người dùng dùng, APK tốt và source revision tương ứng. Không suy diễn từ tên thư mục; ghi SHA-256, build-result, scene, platform, màn hình và server. Tính hash bằng `Get-FileHash -Algorithm SHA256 -LiteralPath <đường dẫn app đã chốt>`; hash bộ dữ liệu đi kèm Windows.
- [ ] Chụp baseline app tại menu, đăng nhập, map Nhà Gôhan/Làng Mori, bảng NPC, hành trang, chat và giao dịch. Ghi kích thước ảnh/render, x2/x4, vị trí/mức sức mạnh và đồ của tài khoản test để lặp lại.
- [ ] Dùng cùng Java endpoint và dữ liệu khởi đầu cho hai phiên app/web test. Không sửa inventory/tài khoản thật để tạo fixture.
- [ ] Ghi bảng baseline: `PC1024x600 -> zoom2 -> logical512x300`, `mobile2400x1080 -> zoom4 -> logical600x270`. Với Safari ghi thêm CSS viewport, devicePixelRatio, visualViewport và safe-area.
- [ ] Đầu ra bước này: bộ baseline có định danh và điều kiện tái hiện; chưa có dòng nghiệm thu gameplay nào được tự đánh dấu đạt.

## 2. Kiểm kê khác biệt source giữa app và web

**Files:** đọc `Assets/Scripts/Assembly-CSharp/Game1/` và `Game2/` ở hai project; tạo `Tools/audit_native_parity.py`, `docs/parity/source-differences.json`.

- [ ] Công cụ so sánh từng file runtime cùng tên, báo file thiếu/thừa và từng method thay đổi. Bỏ khác biệt BOM/newline khỏi nội dung so sánh; không bỏ qua toàn bộ file chỉ vì chứa nhánh WebGL.
- [ ] Phân loại từng khác biệt bằng lý do cụ thể: `Main`/`ScaleGUI`/`MotherCanvas` cho màn hình; `Session_ME*`/`WebSessionCore` cho transport; `Rms`/`BrowserSaveStore` cho lưu; `TField`/`ipKeyboard` cho bàn phím; coroutine thay thread; sửa resource reader cần cho WebGL.
- [ ] Đối chiếu các file gameplay `GameScr`, `Char`, `Panel`, `Service`, `Controller`, `ModFunc`, `Mod/XMAP/PickMobController`. Bất kỳ khác biệt ngoài adapter hoặc bản sửa đã có bằng chứng phải được xem riêng, không giữ vì tiện port.
- [ ] So sánh Game1/Game2 đối xứng; kiểm tra hai scene NROL/NROL1 vẫn dùng đúng Main/namespace.
- [ ] Chạy `python Tools/audit_native_parity.py`; đầu ra phải liệt kê đầy đủ các khác biệt. Mỗi mục chỉ được duyệt khi có lý do, vị trí method và kiểm thử liên quan.

## 3. Chốt hợp đồng kích thước và bố cục

**Files:** `Assets/WebGLTemplates/NroWeb/index.html`, `device.js`, `Assets/Scripts/Assembly-CSharp/Web/BrowserPlatform.cs`; Game1/Game2 `Main.cs`, `ScaleGUI.cs`, `MotherCanvas.cs`, `ServerListScreen.cs`, `ServerScr.cs`, `LoginScr.cs`, `GameScr.cs`.

**Tests:** `Tests/mobile-native-viewport.test.cjs`, `render-resolution.test.cjs`, `InitialMobileMenuChecks.py`, `ResponsiveMenuChecks.py`, `Gateway/browser-responsive-unity.cjs`.

- [ ] Lặp lại ma trận PC1024x600/1920x1080; Android2400x1080; iPhone CSS844x280@3,667x260@2; portrait->landscape; thanh Safari hiện/ẩn; bàn phím hiện/ẩn. Mỗi trạng thái kiểm tra cả hai tab.
- [ ] So sánh app/web tại cùng render/zoom. Đối chiếu kích thước và tọa độ logo, 4/5 nút menu, joystick, nút đánh/kỹ năng, popup và ô đồ bằng tọa độ logical; mọi hitbox phải nằm trong vùng dùng được.
- [ ] Quy tắc kiểm tra pixel: PC backing buffer1024x600; mobile backing buffer bằng kích thước vùng game nhân DPR; Unity không nhân DPR lần thứ hai. Kiểm tra vùng root theo visualViewport và safe-area.
- [ ] Khi phát hiện sai lệch mới: thêm trường hợp FAIL vào test đã nêu, xác nhận FAIL đúng nguyên nhân, rồi chỉ sửa adapter/bố cục browser liên quan; chạy lại đúng trường hợp và ma trận. Không đổi native menu hoặc tự chọn zoom mới khi assets cũ chưa được xử lý nhất quán.
- [ ] Chạy `npm.cmd --prefix Gateway test`, `python Tests/InitialMobileMenuChecks.py`, `python Tests/ResponsiveMenuChecks.py`. Đạt khi không mất nút, không kéo giãn, hitbox trùng hình và native branch không bị đổi.

### 3A. Android hỏi fullscreen; iOS giữ bố cục đã chấp nhận

**Yêu cầu mới đã chốt:** ảnh app mobile là APK; Android web có thể hỏi bật toàn màn hình. iOS Safari hiện tại đã được người dùng chấp nhận và cần giữ vùng an toàn tai thỏ/Dynamic Island.

**Files:** tạo `Assets/WebGLTemplates/NroWeb/fullscreen.js`, `Tests/fullscreen-policy.test.cjs`, `Gateway/browser-fullscreen.cjs`; sửa template `index.html` cho lời mời và thứ tự khởi tạo; cập nhật `Tests/client-shell.test.cjs` để cho phép lời mời Android nhưng vẫn không thêm thanh nút tài khoản ngoài client. Chỉ đưa file mới vào `Builds/WebGL/` qua đợt build được yêu cầu.

- [ ] Viết test FAIL cho các trạng thái: Android+API hỗ trợ có lời mời; Android+API thiếu không chặn chơi; iOS/PC không có lời mời; “Để sau” khởi động bình thường; đồng ý gọi fullscreen đúng một lần; browser từ chối vẫn khởi động; đổi tab không hỏi lại.
- [ ] Thiết kế lời mời gọn trước khi khởi tạo Unity: “Chơi toàn màn hình để có thêm diện tích?” / “Bật toàn màn hình” / “Để sau”. Khả năng hỗ trợ dựa trên `document.fullscreenEnabled` và hàm `game.requestFullscreen`; không suy diễn chỉ từ tên browser.
- [ ] Trong callback chạm “Bật toàn màn hình”, gọi `game.requestFullscreen({navigationUI:'hide'})` ngay trong thao tác người chơi. Xử lý promise và `fullscreenchange`/`fullscreenerror`. Fullscreen chứa cả canvas và HTML keyboard để form vẫn dùng được.
- [ ] Chỉ bắt đầu `createUnityInstance` sau quyết định và sau khi root/canvas đã nhận kích thước ổn định. Đảm bảo chỉ khởi tạo một instance kể cả double tap, request thất bại hoặc resize đến cùng lúc; không nạp x3 rồi tự đổi x4 khi resource/font đang dùng.
- [ ] Gắn đường bật/thoát trong phần cấu hình cho Android qua browser adapter; giữ các thao tác của game gốc. Thoát fullscreen không reload trang, không đóng hai socket, không mất trạng thái hai tài khoản.
- [ ] Chạy browser test Android với thao tác tap thật, fullscreen vào/thoát, Back, xoay ngang và bàn phím. Đo game bounds, hitbox và trạng thái hai tab sau mỗi chuyển trạng thái. `fullscreenchange` phải là bằng chứng vào fullscreen; không dùng CSS che thanh browser làm bằng chứng.
- [ ] iOS: đối chiếu ảnh Safari người dùng vừa chấp nhận; test safe-area trái/phải trên máy tai thỏ và Dynamic Island, cả landscape-left/right. Không thêm prompt, không tự fullscreen, không thay zoom/bố cục iOS để giống Android fullscreen.
- [ ] PC: rerun pointer/menu checks và xác nhận không thấy prompt Android.
- [ ] Đầu ra nghiệm thu: Android có thêm diện tích game khi browser cho phép, bỏ qua vẫn chơi, mọi trạng thái đủ nút/bấm đúng; iOS giữ bố cục được chấp nhận và không có nút dưới vùng bị che. Chưa đạt nếu chỉ có test API mà chưa thử Android/iPhone thật.

## 4. Điều khiển đúng PC và mobile

**Files:** Game1/Game2 `Main.cs`, `GameCanvas.cs`, `GamePad.cs`, `GameScr.cs`, `MyKeyMap.cs`; `Management.cs`, `Web/BrowserHost.cs`.

**Tests:** `Tests/VirtualControlsChecks.py`, `TabSwitchChecks.py`; tạo `Gateway/browser-native-controls.cjs` cho thao tác trong phiên test đăng nhập.

- [ ] PC: bấm menu/NPC, giữ và nhả di chuyển, phím kỹ năng, cuộn bảng, dán tài khoản; Windows cảm ứng vẫn mang cấu hình PC theo baseline.
- [ ] Mobile: kéo joystick, nhảy/bay, đổi mục tiêu, đánh, dùng kỹ năng, giữ di chuyển đồng thời chạm nút khác. Kiểm tra touchcancel, ngón tay ra ngoài canvas và đổi tab khi đang giữ.
- [ ] Đo phản hồi bằng thay đổi vị trí, mục tiêu/chiêu và phản hồi Java; không chỉ kiểm tra sự kiện touch đã được phát.
- [ ] Mỗi lỗi phải có sequence ngón/phím cụ thể, trạng thái trước/sau và test FAIL. Chỉ sửa luồng nhận/chuyển/release input trong browser adapter nếu app chuẩn xử lý đúng.
- [ ] Chạy `python Tests/VirtualControlsChecks.py`, `python Tests/TabSwitchChecks.py`, `node Gateway/browser-native-controls.cjs`. Đạt khi thao tác có tác dụng đúng trong map và không có phím/nút bị giữ sau hủy/đổi tab.

## 5. Bàn phím, hai tab và vòng đời phiên

**Files:** Game1/Game2 `TField.cs`, `ChatTextField.cs`, `ipKeyboard.cs`, `Rms.cs`; `Web/BrowserPlatform.cs`, `BrowserHost.cs`, `BrowserSaveStore.cs`; HTML keyboard.

**Tests:** `Tests/ChatKeyboardChecks.py`; `Gateway/browser-mobile-input.cjs`, `browser-pointer.cjs`, `browser-tab-switch.cjs`; bổ sung trường hợp tiếng Việt/IME và resume vào các runner hiện có.

- [ ] Form: tên/mật khẩu, xóa/dán, gửi Xong/Enter, hủy, đổi tab và reload vẫn lưu riêng mỗi tài khoản.
- [ ] Chat trong map: gõ tiếng Việt có dấu và IME, gửi đúng một lần, hủy không gửi, không di chuyển nhân vật lúc nhập, recipient và nội dung đúng. Kiểm tra các hộp hỏi đáp NPC liên tiếp.
- [ ] Tab1/Tab2: hai socket/nhân vật riêng, không lẫn input/save; kiểm tra đổi tab nhanh và giữ nút lúc đổi.
- [ ] Safari/Chrome: tắt màn hình/chuyển ứng dụng rồi quay lại, mất mạng và nối lại; xác minh UI phục hồi và không tự phát lệnh cũ. Ghi rõ hành vi browser khi bị tạm dừng thay vì hứa chạy nền như native.
- [ ] Chạy `python Tests/ChatKeyboardChecks.py`; chạy các browser runner trên PC Chromium, Android Chromium và iOS WebKit. Nghiệm thu phần bàn phím/cảm ứng cần lặp lại trên điện thoại thật.

## 6. Nghiệm thu gameplay với Java

**Files:** đọc Game1/Game2 `Controller.cs`, `Service.cs`, `Session_ME.cs`, `Session_ME2.cs`; `Web/WebSessionCore.cs`, `ProtocolCodec.cs`; `Gateway/server.cjs`. Tạo `docs/parity/gameplay-results.md` và runner `Gateway/browser-gameplay-parity.cjs`.

- [ ] Dùng tài khoản test/zone riêng cùng Java server và trạng thái baseline. Không ghi mật khẩu vào source hoặc log packet. Chỉ lưu command, độ dài, capability và payload đã loại dữ liệu xác thực khi cần đối chiếu.
- [ ] Chạy cùng chuỗi trên app và web: vào map, chọn NPC, mở đúng menu, dùng đồ/pet Cửu Vĩ, mặc/tháo đồ/hào quang, đổi map/zone, đánh quái, auto-combat, boss và hút Killis.
- [ ] Giao dịch hai tài khoản test: thêm đồ đơn, stack, slot>=128, gỡ, khóa, xác nhận, hủy. Đối chiếu túi và vàng trước/sau, không chỉ xem cửa sổ mở được.
- [ ] Xác nhận cùng capabilities/format `IOP32`, `INV200`, `BOSS1`, `BOSS2`; so sánh packet command/field giữa app và web. Nếu web gửi đúng nhưng server từ chối theo dữ liệu/quy tắc thì phân loại riêng, không sửa UI để che lỗi.
- [ ] Đánh hợp lệ phải có phản hồi damage/HP quái thay đổi; tình huống né/ngoài tầm đối chiếu app. Không dùng kết quả login fixture để đánh dấu combat đạt.
- [ ] Mỗi dòng kết quả ghi app, web, thiết bị, build commit, server, bước tái hiện và bằng chứng. Đạt khi toàn bộ chức năng trong bảng nghiệm thu có kết quả thực tế ở cả hai tab.

## 7. Chạy trên thiết bị thật và phát hành

**Files:** `docs/parity/acceptance.md`, active `Builds/WebGL/`, gateway config; metadata build/backup lưu ngoài Git theo quy tắc hiện tại.

- [ ] Thiết bị: PC Chrome/Edge, điện thoại Android Chrome, iPhone Safari mặc định. Nếu có app iOS chuẩn, bổ sung baseline IPA/ảnh/trạng thái; không ghi đã đối chiếu IPA khi chưa có bằng chứng.
- [ ] Android nghiệm thu cả “Để sau”, fullscreen, thoát bằng Back và hiện/ẩn bàn phím. iOS dùng ảnh Safari đã được người dùng chấp nhận, thử vùng an toàn tai thỏ/Dynamic Island và thanh Home ở cả hai hướng ngang; không yêu cầu iOS fullscreen như Android.
- [ ] Trên mỗi thiết bị chạy menu->login->map->điều khiển->chat->NPC/inventory->giao dịch->đổi tab->đổi kích thước/quay lại từ nền. Lưu lỗi còn lại vào acceptance, không tự đánh dấu cả nhóm đạt.
- [ ] Chỉ khi người dùng yêu cầu đợt build/deploy: chạy `build-web.cmd` tới thư mục staging, kiểm tra `Result=Succeeded`/`Errors=0`; rerun browser/unit tests cần thiết trên build cuối. Không build lại APK/EXE/JAR cho nhiệm vụ web.
- [ ] Backup active build và SHA-256; cài đủ HTML, loader, framework, data, wasm cùng phiên bản. Commit/push source và active build sau review theo phạm vi được yêu cầu.
- [ ] VPS cập nhật bằng `git pull --ff-only origin main`; kiểm tra domain trả đúng tên build mới, assets và WebSocket. Chỉ cập nhật HTML hoặc chỉ push source không phải nghiệm thu domain.
- [ ] Nếu domain còn cắt nút/điều khiển không được, giữ trạng thái chưa đạt và quay lại đúng bước tái hiện. Nếu build cuối lỗi, khôi phục active build đã backup; không phục hồi database người chơi để chữa lỗi UI.

## Điều kiện kết thúc

Có baseline app rõ ràng, khác biệt source đã được giải thích, bảng chức năng thực tế app/web đạt, Safari iPhone thật được kiểm tra và domain phục vụ đúng bản đã nghiệm thu. Câu “giống client” chỉ được dùng cho phần có bằng chứng; không cam kết tuyệt đối không có mọi loại lỗi từ một vài test menu.
