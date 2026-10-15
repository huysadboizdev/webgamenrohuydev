# Web: Cadíc và hiệu ứng chiến đấu — 15/10/2026

Yêu cầu: kiểm tra và sửa cùng lỗi quái/hiệu ứng đứng trên bản web. Phạm vi source `ClientWeb/PRJ_2Tab_Web`, không build WebGL/APK/EXE/JAR hoặc publish website.

## Kế hoạch và bằng chứng

- [x] Tái hiện với hàm GetFrame thực tế ở cả Game1/Game2: bảng animation thiếu gây NullReferenceException, có thể ngắt vòng cập nhật quái/chữ sát thương/hiệu ứng phía sau.
- [x] Đồng bộ guard của APK v3 vào cả hai Mob.cs: giữ sequence mặc định khi bảng thiếu/sai, xác thực đủ sáu sequence và index sprite, chỉ áp dụng bảng hợp lệ rồi kết thúc chờ. Regression `Tests/MobFrameRecoveryChecks.py` đã thấy thất bại trước sửa và qua sau sửa.
- [x] Kiểm tra/sửa decoder hiệu ứng: baseline `Tests/EffectDataChecks.py` thất bại với fixture hiệu ứng62 vì source chỉ đọc tọa độ byte trong khi dữ liệu dùng short. Cả hai EffectData.cs đã thử giải mã byte rồi short, xác thực sprite/index/sequence và chỉ công bố trạng thái khi đọc hoàn chỉnh. Gói bị cắt/hỏng giữ dữ liệu hợp lệ cũ; get(index)/paintFrame chặn index âm/vượt giới hạn.
- [x] Rà soát và chạy lại các kiểm thử trong phạm vi thay đổi; rà soát độc lập không phát hiện lỗi correctness trong phần sửa.

## Kết quả xác minh cuối

- `python Tests/MobFrameRecoveryChecks.py`: qua các subtest Game1/Game2, giữ sequence mặc định và cho phép phục hồi khi dữ liệu đến sau.
- `python Tests/EffectDataChecks.py`: qua 150 mẫu hiệu ứng mỗi tab; tọa độ short, marker animation, dữ liệu cắt ngắn/hỏng, giữ trạng thái cũ, giới hạn khi vẽ. Có thêm regression tự chứa cho mob ID0 chỉ có sprite frame và không có đuôi sequence, ở cả overload reader và byte array.
- Probe riêng với payload mob95 x1–x4 của source server giữ đúng số frame và bounds trước sửa: x1/x2/x4 15 frame, 24×54; x3 12 frame, 32×25. Không copy resource server vào web hoặc đổi resource trong lần sửa web này.
- Kiểm thử ảnh NPC giải mã/phục hồi, WebSocket, wrapper của bốn kết nối và gateway3/3 đều qua. Scoped `git diff --check` không có lỗi whitespace. Compiler vẫn có cảnh báo CS0675 có sẵn trong myReader.cs; không sửa file này.
- Hai EffectData.cs giống nhau sau khi chuẩn hóa namespace. Không sửa gateway/transport runtime hoặc các bản native/APK trong lượt này.

## Kiểm tra khác

Đã qua: Gateway/server.test.cjs (3/3), Tests/Transport/websocket-tests.cjs, Tests/Transport/wrapper-contract-tests.cjs, Tests/NpcImageDecodeChecks.py, Tests/NpcImageRecoveryChecks.py.

Giới hạn bộ kiểm thử có sẵn: `Tests/Transport/run-transport-tests.ps1` không biên dịch được vì thiếu ba file Assets/Editor/WebProtocolCases.cs, WebSessionCases.cs, WebTransportRegression.cs. Chưa sửa bộ test tổng hợp này; các bài kiểm tra JS kết nối vẫn chạy riêng và qua. Không khẳng định mọi test web đều xanh.

Source sửa chưa được đưa vào bản WebGL đã build; trang đang chạy chưa nhận code mới. Chưa có kiểm chứng gameplay bằng bản WebGL mới hoặc trên Android6 thật.
