# Chuẩn tương đương app cho client web

## Mục tiêu

Lấy app đã build mà người dùng xác nhận hoạt động tốt làm chuẩn. Bản web dùng gameplay, giao diện, tài nguyên và giao thức Java của PRJ_2Tab_550K; trên PC dùng hành vi PC, trên điện thoại dùng hành vi mobile. Nghiệm thu gồm màn hình vào game và chơi thật trong map, ở cả hai tài khoản.

Lượt này chỉ lập kế hoạch; không sửa gameplay, build, triển khai, hoặc tự xác nhận đã đạt toàn bộ.

## Chuẩn từ ảnh và các bản build đã tìm thấy

- Ảnh mobile: 2400x1080, zoom x4; có joystick bên trái, kỹ năng và cụm thao tác bên phải, hai tab.
- Ảnh PC: cửa sổ game 1024x600, zoom x2; chuột/bàn phím và menu theo bản PC.
- App Windows có sẵn: `C:/Users/Admin/Downloads/PRJ_2Tab_550K/Builds/Distribution/20261011-Icon70B/Windows/NRO.exe` cùng thư mục dữ liệu. Build-result ghi thành công, 0 lỗi. Chưa xác định đây có phải chính EXE người dùng chụp; phải chốt bằng đường dẫn/version/hash trong bước baseline.
- APK có sẵn: `C:/Users/Admin/Downloads/PRJ_2Tab_550K/Builds/Distribution/20261013-BuuUpgradeAura/Android/NRO.apk`; build thành công, 0 lỗi, ARMv7/ARM64. Chưa tìm thấy IPA làm chuẩn trong lượt rà soát này.
- Các bản build không dùng cùng cấu hình server: metadata Windows chỉ server ngoài, APK nêu localhost. Khi đối chiếu cần cùng server Java/test fixture, map, zone, tài khoản test và trạng thái bắt đầu để không nhầm khác dữ liệu thành lỗi web.

## Phương án

### Cập nhật yêu cầu màn hình theo thiết bị

Người dùng xác nhận ảnh app mobile làm chuẩn là APK. Ảnh Android mới có thanh Chrome và thanh điều hướng hệ thống, còn ảnh Safari iOS hiện tại đã được người dùng chấp nhận về bố cục. Không dùng cùng yêu cầu toàn màn hình cho hai nền tảng.

- **Android web:** khi vào game, hỏi một lần trong phiên: “Chơi toàn màn hình để có thêm diện tích?” với “Bật toàn màn hình” và “Để sau”. Chỉ đề nghị khi browser hỗ trợ. Nếu người chơi chọn bật, yêu cầu fullscreen trực tiếp từ lần chạm nút; chờ kích thước ổn định rồi mới khởi tạo Unity và chọn zoom/resource. Nếu bỏ qua hoặc browser từ chối, game vẫn vào bình thường.
- **Android trong phiên:** có thao tác bật/thoát toàn màn hình trong phần cấu hình. Đổi tab tài khoản không hỏi lại. Nút Back/thoát fullscreen, xoay ngang và mở/đóng bàn phím phải giữ phiên, hitbox và bố cục đúng. Diện tích game lớn hơn nhưng tỷ lệ hình và cách tính zoom giữ theo app.
- **iOS web:** giữ bố cục của ảnh Safari mới làm chuẩn đã chấp nhận; không hiện câu hỏi Android hoặc tự yêu cầu fullscreen. Logo, nút đổi tab, menu, joystick và nút kỹ năng luôn ở trong safe-area, không bị tai thỏ/Dynamic Island/thanh Home che. Kiểm tra cả hai hướng ngang.
- **PC web:** giữ nhánh PC và kích thước cửa sổ chuẩn; không hiện lời mời toàn màn hình Android.

Đây là yêu cầu cho web trên Android; lượt này không sửa hay build lại APK native. Fullscreen được browser điều khiển, không hứa mọi máy sẽ ẩn hoàn toàn mọi thanh hệ thống.

Tài liệu API đối chiếu: [requestFullscreen](https://developer.mozilla.org/en-US/docs/Web/API/Element/requestFullscreen), [fullscreenEnabled](https://developer.mozilla.org/en-US/docs/Web/API/Document/fullscreenEnabled).

1. **Đề xuất: dùng chung source native, chỉ port lớp nền tảng.** Giữ Game1/Game2, ảnh, UI, gameplay, packet format. Khác biệt được giới hạn và ghi rõ ở kích thước browser, input, bàn phím, WebSocket/TCP, lưu trữ và lifecycle. Đây là hướng phù hợp với yêu cầu lấy app làm chuẩn.
2. UI web vẽ riêng trên gameplay: cần duy trì hai bộ bố cục và rất dễ lệch app; không chọn làm kiến trúc của kế hoạch.
3. Stream app chạy từ máy chủ: có thêm máy chạy app và độ trễ stream cho mỗi phiên; không chọn cho client web hiện tại.

## Nghiệm thu

| Nhóm | Yêu cầu |
|---|---|
| Thiết bị | PC dùng chuột/phím; Windows cảm ứng vẫn dùng nhánh PC. Android/iPhone dùng nhánh mobile và đầy đủ nút cảm ứng. |
| Kích thước | PC1024x600/x2; mobile2400x1080/x4 tại khung chuẩn. Trên Safari vùng game vừa phần nhìn thấy sau thanh trình duyệt và vùng an toàn; không kéo giãn hoặc cắt nút. |
| Fullscreen Android | Hỏi một lần trước khởi tạo game, bật bằng thao tác chạm, bỏ qua vẫn chơi được; vào/thoát không mất phiên hoặc kẹt input. |
| iOS đã chấp nhận | Giữ bố cục ảnh Safari hiện tại, không thêm lời mời fullscreen Android; mọi vùng thao tác nằm ngoài tai thỏ/Dynamic Island/thanh Home. |
| Màn hình | Chọn server, 4/5 nút menu, form tài khoản, chat, bảng NPC, hành trang, shop, giao dịch và gameplay đều đối chiếu với app. |
| Thao tác | Di chuyển, nhảy/bay, đánh, chọn mục tiêu, dùng kỹ năng, mở menu, cuộn và kéo đồ phải thực hiện được. Kiểm tra giữ joystick đồng thời bấm kỹ năng. |
| Bàn phím | Gõ tiếng Việt, dấu/IME, xóa, dán, Enter/Xong, hủy, đổi tab; không gửi hai lần, không điều khiển nhân vật khi nhập. |
| Java | Cùng cấu trúc packet/capabilities, chỉ số, slot đồ và phản hồi server; xác nhận HP quái thay đổi khi đánh hợp lệ. |
| Hai tài khoản | Phiên/socket/RMS/input tách biệt; không để tab 1 thao tác nhân vật/tab 2 hoặc lưu đè tài khoản. |
| Lifecycle | Xoay màn hình, hiện/ẩn bàn phím, thu/phóng cửa sổ, chuyển nền rồi quay lại, rớt mạng rồi vào lại không kẹt phím hoặc kẹt giao diện. |
| Bản phát hành | Domain phục vụ đúng HTML và đủ bốn file cùng build; có backup và cách quay lại bản trước. |

## Trạng thái bằng chứng hiện tại

Đã có kiểm tra source/fixture cho nhận diện, độ phân giải, menu, input/lưu tài khoản, giao dịch và một số luồng Java. Đã chạy WebKit/Chromium mô phỏng. Chưa có bộ đối chiếu app/web đầy đủ, chưa kiểm thử gameplay đăng nhập toàn bộ trên iPhone thật, và chưa nghiệm thu trạng thái sau khi VPS pull bản sửa mới. Các khoảng trống này là công việc của kế hoạch, không được đánh dấu đạt bằng kết quả test menu.

## Quy tắc thay đổi

- App gốc đang tốt là baseline; mỗi thay đổi web cần có khác biệt tái hiện được và kiểm thử tương ứng.
- Không đổi zoom mỗi lần resize khi font/ảnh đã nạp; phải giữ nhất quán zoom, tài nguyên và tọa độ input.
- Không viết lại tính năng game để giải quyết lỗi ở browser adapter.
- Thực hiện theo từng nhóm; build mới chỉ khi người dùng yêu cầu rõ ràng cho đợt triển khai. Kế hoạch này không tự mở Unity hoặc đóng gói.
- WebKit mô phỏng hỗ trợ tìm lỗi; nghiệm thu iPhone phải có Safari trên thiết bị thật. Chạy nền của browser phải kiểm tra theo lifecycle thực tế, không mặc định giống app native.
