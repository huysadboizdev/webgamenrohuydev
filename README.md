# webgamenrohuydev

Project Unity WebGL riêng, chuyển từ client **PRJ_2Tab_550K**, giữ hai tài khoản qua hai scene NROL/NROL1. Repository gồm source Unity, tài nguyên, gateway Node.js, kiểm thử và bản web đã build.

## Chạy bản web

1. Cài Node.js 22 trở lên.
2. Chạy server game TCP (mặc định `127.0.0.1:14445` trên máy gateway).
3. Chạy `run-web.cmd`; lần đầu script cài dependency bằng npm.
4. Mở `http://localhost:8086`.

Điện thoại cùng mạng LAN mở `http://<IP-LAN-của-PC>:8086`. Trên điện thoại, localhost là chính điện thoại đó. Cho phép cổng 8086 qua firewall nếu cần.

Windows/laptop, kể cả màn hình cảm ứng, dùng nhánh PC của client. Android/iPhone/iPad dùng nhánh mobile có sẵn trong source gốc. Mobile để dọc chỉ hiển thị yêu cầu xoay ngang; xoay ngang mới hiện game. Xoay lại dọc vẫn giữ phiên và tài khoản. Nút Cấu hình → Bàn phím ảo điều khiển joystick của client.

Hai tài khoản lưu dữ liệu riêng. Trình duyệt có thể giảm hoặc dừng cập nhật khi trang xuống nền.

## Địa chỉ server và triển khai

Cấu hình cùng địa chỉ TCP ở:

- `Gateway/config.json`: allowlist `targets`, ví dụ `["192.168.1.10:14445"]`.
- `Builds/WebGL/config.js`: `serverList`, ví dụ `NRO NGU:192.168.1.10:14445:0,0,0`.
- `Assets/WebGLTemplates/NroWeb/config.js`: giữ cấu hình cho lần build tiếp theo.

Nếu server có kết nối phụ, thêm địa chỉ đó vào allowlist. Khi dùng domain HTTPS, reverse proxy website và `/gateway` tới gateway, hỗ trợ WebSocket. Browser dùng WSS trên HTTPS. Có thể đặt `gatewayUrl` trong config.js và allowlist `origins` nếu gateway ở domain khác.

GitHub lưu source; push repository không tự chạy server game hoặc gateway. Website cần gateway Node.js và server TCP hoạt động.

## Build lại

Cài Unity **2022.3.62f2** cùng module **WebGL Build Support**. Mở folder repository như một Unity project, rồi chạy menu **NRO Web → Build browser client** hoặc `build-web.cmd`. Sửa đường dẫn Unity trong script nếu máy cài ở nơi khác.

Kết quả ở `Builds/WebGL`, báo cáo `Builds/WebGL/build-result.txt`, log `Logs/web-build.log`. Build hiện tại là release, giới hạn độ phân giải render để giảm tải GPU, giữ kiểm tra null và bounds. Hai scene đều được đưa vào build. Mẫu dữ liệu hiệu ứng phục vụ kiểm thử nằm trong `Tests/Fixtures/Effects`, không cần source server bên ngoài để build.

Source kết nối native được giữ; repository này dành cho bản web, chưa xác nhận build APK/IPA/EXE từ project port.

## Kiểm thử

Từ folder `Gateway`:

```powershell
npm.cmd ci
npm.cmd test
npx.cmd playwright install chromium
node browser-orientation.cjs
node browser-responsive.cjs
node browser-responsive-unity.cjs
node browser-resize-selection.cjs
node browser-pointer.cjs
node browser-mobile-input.cjs
```

Từ folder project, Windows cần .NET Framework C# compiler và Python 3:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tests/Transport/run-transport-tests.ps1
python Tests/EffectDataChecks.py
python Tests/MobFrameRecoveryChecks.py
python Tests/NpcImageRecoveryChecks.py
python Tests/NpcImageDecodeChecks.py
python Tests/TabSwitchChecks.py
python Tests/ResourceDownloadChecks.py
python Tests/ResponsiveMenuChecks.py
python Tests/VirtualControlsChecks.py
python Tests/ChatKeyboardChecks.py
```

Build Unity chạy thêm kiểm thử ảnh NPC, giao thức, lưu dữ liệu riêng từng tab, viewport và dữ liệu hiệu ứng. Bằng chứng browser được tạo ở `Tests/evidence`, không commit ảnh/log của phiên chơi.

Kiểm tra mobile tự động dùng Chromium giả lập Android/iPhone/iPad; không thay thế kiểm tra điện thoại thật hoặc Safari. Kiểm thử kết nối dùng TCP fixture; cần server và tài khoản thử để xác nhận các luồng đăng nhập/chiến đấu thực tế.

## Cấu trúc

- `Assets`, `Packages`, `ProjectSettings`: source và tài nguyên Unity.
- `Assets/Scripts/Assembly-CSharp/Web`: nền tảng, lưu dữ liệu và kết nối web.
- `Assets/Plugins/WebGL`, `Assets/WebGLTemplates/NroWeb`: cầu nối JavaScript và giao diện web.
- `Gateway`: WebSocket chuyển tiếp đến TCP server.
- `Builds/WebGL`: bản web chạy sẵn.
- `Tests`: kiểm thử và fixtures.
- `docs`: thiết kế và ghi chú.

Các script trong `Tools` phục vụ lần port ban đầu; không chạy lại trên source đã port. Cache Unity, node_modules, log và dữ liệu server không thuộc repository này.
